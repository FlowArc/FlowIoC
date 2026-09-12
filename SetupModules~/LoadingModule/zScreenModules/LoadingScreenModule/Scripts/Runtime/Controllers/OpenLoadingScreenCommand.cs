using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.LoadingModule.LoadingScreenModule.Models;
using Modules.LoadingModule.LoadingScreenModule.ViewsMediators;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingScreenModule.Controllers
{
    /// <summary>
    /// Opens the screen for a set that began, or refills it when it is already up (a retry, or a
    /// second fullscreen set). It fills from the model rather than from the signal that opened it:
    /// the set may have moved on, or closed, while the prefab was loading. Three ways out, each one
    /// resolving the retain.
    /// </summary>
    internal class OpenLoadingScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILoadingScreenModel _model { get; set; }

        [SignalParam] private LoadingSetStatusRVO _status { get; set; }

        public override async void Execute()
        {
            Retain();
            _model.Remember(_status);

            try
            {
                if (_screenService.Check.IsScreenActive<LoadingScreenView>()
                    && _screenService.TryGet.Screen(out LoadingScreenView open) && open != null)
                {
                    Fill(open);
                    Release();
                    return;
                }

                LoadingScreenView screen = await _screenService.Open<LoadingScreenView>().Show<LoadingScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError(FlowLogType.LoadingScreenModule, "OpenLoadingScreenCommand - the loading screen did not open.");
                    Stop();
                    return;
                }

                Fill(screen);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError(FlowLogType.LoadingScreenModule,
                    $"OpenLoadingScreenCommand threw while opening the loading screen: {exception}");
                Stop();
            }
        }

        private void Fill(LoadingScreenView screen)
        {
            string set = _status.Set;

            if (_model.TryGetLatest(set, out LoadingSetStatusRVO latest))
                screen.Apply(latest);

            string failedStep = _model.FailedStepOf(set);
            if (failedStep != null)
                screen.ShowFailed(failedStep);

            if (_model.IsClosed(set))
                screen.Hide();
        }
    }
}
