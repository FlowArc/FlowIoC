using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Service.Sub;

namespace FlowIoC.ScreenModule.Commands
{
    /// <summary>
    /// A screen context going away takes its screen with it. The instance is unloaded first,
    /// while the entry still knows how it was loaded, and the entry is dropped after.
    /// </summary>
    internal class UnRegisterScreenCommand : Command
    {
        [Inject] private IScreenRegistryModel _registry { get; set; }
        [Inject] private UnloadSubService _unload { get; set; }
        [SignalParam] private Type _viewType { get; set; }

        public override void Execute()
        {
            ScreenEntry entry = _registry.FindEntry(_viewType);
            if (entry == null)
                return;

            if (entry.Loaded != null)
                _unload.Unregistered(entry.Loaded);

            _registry.RemoveEntry(entry);
        }
    }
}
