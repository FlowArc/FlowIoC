using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using Modules.BotBarModule.BotBarScreenModule.ViewsMediators;
using Modules.BotBarModule.Shared.Data.UnityObjects;
using Modules.BotBarModule.Shared.Data.ValueObjects;

namespace Modules.BotBarModule.BotBarScreenModule.Controllers
{
    /// <summary>
    /// Opens the bar and fills it from CD_BotBar and RD_BotBar, read through ISharedDataModel -
    /// the whole state at Open is one event, so a bar already up is fetched and refilled. Three
    /// ways out, each one resolving the retain.
    /// </summary>
    internal class OpenBotBarScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ISharedDataModel _sharedData { get; set; }

        public override async void Execute()
        {
            Retain();

            try
            {
                CD_BotBar config = _sharedData.GetScriptable<CD_BotBar>();
                RD_BotBar runtime = _sharedData.GetScriptable<RD_BotBar>();

                if (config == null || runtime == null)
                {
                    // The shared data model reported the missing filing.
                    Stop();
                    return;
                }

                if (_screenService.Check.IsScreenActive<BotBarScreenView>()
                    && _screenService.TryGet.Screen(out BotBarScreenView open) && open != null)
                {
                    Fill(open, config, runtime);
                    Release();
                    return;
                }

                IScreenBuilder builder = _screenService.Open<BotBarScreenView>();

                // Hidden before it ever opened: no slide in, and the bar is parked off screen below.
                if (!runtime.IsShown)
                    builder = builder.SkipShowAnimation();

                BotBarScreenView screen = await builder.Show<BotBarScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError("OpenBotBarScreenCommand - the bar did not open.");
                    Stop();
                    return;
                }

                Fill(screen, config, runtime);
                FlowLogger.Log($"Execute - OpenBotBarScreenCommand | {runtime.Tabs.Count} tabs, selected {runtime.Selected}");
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenBotBarScreenCommand threw while opening the bar: {exception}");
                Stop();
            }
        }

        private static void Fill(BotBarScreenView screen, CD_BotBar config, RD_BotBar runtime)
        {
            screen.Build(runtime.Tabs, config);

            foreach (BotBarTabRVO tab in runtime.Tabs)
            {
                screen.ShowTabState(tab.Key, tab.State);
                screen.ShowBadge(tab.Key, tab.Badge);
            }

            screen.ShowSelected(runtime.Selected);

            if (!runtime.IsShown)
                screen.ShowBar(false, animate: false);
        }
    }
}
