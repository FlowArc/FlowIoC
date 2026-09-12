using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.LoadingModule.LoadingOverlayScreenModule.Models;
using Modules.LoadingModule.LoadingOverlayScreenModule.ViewsMediators;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.Controllers
{
    /// <summary>
    /// Opens the overlay for a set that began, or refills it when it is already up. It fills from
    /// the model rather than from the signal that opened it: the set may have moved on, or ended,
    /// while the prefab was loading. Three ways out, each one resolving the retain.
    /// </summary>
    internal class OpenLoadingOverlayScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILoadingOverlayScreenModel _model { get; set; }

        [SignalParam] private LoadingSetStatusRVO _status { get; set; }

        public override async void Execute()
        {
            Retain();
            _model.Remember(_status);

            try
            {
                if (_screenService.Check.IsScreenActive<LoadingOverlayScreenView>()
                    && _screenService.TryGet.Screen(out LoadingOverlayScreenView open) && open != null)
                {
                    Fill(open);
                    Release();
                    return;
                }

                LoadingOverlayScreenView screen = await _screenService.Open<LoadingOverlayScreenView>().Show<LoadingOverlayScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError(FlowLogType.LoadingOverlayScreenModule, "OpenLoadingOverlayScreenCommand - the overlay did not open.");
                    Stop();
                    return;
                }

                Fill(screen);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError(FlowLogType.LoadingOverlayScreenModule,
                    $"OpenLoadingOverlayScreenCommand threw while opening the overlay: {exception}");
                Stop();
            }
        }

        private void Fill(LoadingOverlayScreenView screen)
        {
            string set = _status.Set;

            if (_model.TryGetLatest(set, out LoadingSetStatusRVO latest))
                screen.Apply(latest);

            if (_model.IsClosed(set))
                screen.Hide();
        }
    }
}
