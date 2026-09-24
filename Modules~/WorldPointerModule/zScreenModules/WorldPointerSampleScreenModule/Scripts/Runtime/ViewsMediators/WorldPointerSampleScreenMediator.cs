#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.WorldPointerModule.WorldPointerSampleScreenModule.Signals;

namespace Modules.WorldPointerModule.WorldPointerSampleScreenModule.ViewsMediators
{
    /// <summary>
    /// Says when the screen came up and went down; nothing is decided here. The screen is pooled,
    /// so the pair runs on every opening, and the service sees the displays come and go with it.
    /// </summary>
    public class WorldPointerSampleScreenMediator : IMediator
    {
        [Inject] private WorldPointerSampleScreenView _view { get; set; }
        [InjectSignal] private WorldPointerSampleScreenInternalSignals _internalSignals { get; set; }

        public void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
        }

        public void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
        }

        private void OnScreenShown(IScreenBody screen) => _internalSignals.DisplaysShown.Dispatch(_view);

        private void OnScreenHidden(IScreenBody screen) => _internalSignals.DisplaysHidden.Dispatch(_view);
    }
}
#endif