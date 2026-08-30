using Modules.GameplayModule.GameplayScreenModule.Shared.Signals;
using Modules.GameplayModule.GameplayScreenModule.ViewsMediators;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    public class GameplayScreenMediator : IMediator
    {
		[Inject] private GameplayScreenView _view { get; set; }
		[InjectSignal] private GameplayScreenSignals _signals { get; set; }
        
        public virtual void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
        }

        public virtual void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
        }

        private void OnScreenShown(IScreenBody screen)
        {
            //here you can use 'AddListeners'
            //_sampleSignals.Incoming.sampleSignal.AddListener(_view.SampleTest);
            
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            //here you can use 'RemoveListeners'
            //_sampleSignals.Incoming.sampleSignal.RemoveListener(_view.SampleTest);
            
        }

        // if you want to use a signal dispatch for a button click
        // you have to check if screen is available. Example:
        //
        // if (_view.Data.State == ScreenState.AvailableToSendSignal)
    }
}
