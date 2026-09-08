using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Builder
{
    /// <summary>
    /// Starts an open and hands back a builder for it. Everything the open then decides lives on
    /// that builder: this sub service is one object for the whole run, and it used to hold the
    /// screen being opened in its own fields - so two commands opening a screen in the same frame
    /// wrote over each other, and the second Show opened whatever the first had asked for.
    ///
    /// The pool is not consulted here. It used to be, and an open that never reached Show kept
    /// the instance it had taken out of the pool for good.
    /// </summary>
    public class ScreenBuilderSubService : IScreenBuilderSubService
    {
        [Inject] private IScreenRegistryModel _registry { get; set; }
        [Inject] private IScreenRuntimeModel _screenRuntimeModel { get; set; }
        [Inject] private ShowSubService _show { get; set; }
        [Inject] private HideSubService _hide { get; set; }

        IScreenBuilder IScreenBuilderSubService.Open<T>(int managerId)
        {
            // The quiet lookup: not finding one is what this method is here to report, and asking
            // with GetEntry meant the same miss was written twice, once in each voice.
            if (!_registry.TryGetEntry(managerId, typeof(T), out ScreenEntry entry))
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService.Open] {typeof(T).Name} aborted: not registered at manager {managerId}. Is the Root of the module that owns it in the scene?");

                // Handed back all the same, so the chain the caller wrote still reads. Show answers
                // with nothing rather than throwing halfway through somebody's fluent line.
                return new ScreenBuilder(null, _screenRuntimeModel, _show, _hide);
            }

            ScreenVO screenData = new ScreenVO {ScreenType = typeof(T), ManagerId = managerId};
            _registry.CopyDataFromConfig(screenData, entry.Screen);

            FlowLogger.LogAbout(SystemLogType.Screen, screenData.ScreenType, "[ScreenService.Open] ", screenData.ScreenType.Name);
            return new ScreenBuilder(screenData, _screenRuntimeModel, _show, _hide);
        }
    }
}
