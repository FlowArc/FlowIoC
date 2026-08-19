using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.Editor.CodeGenerator.TempScreens
{
    internal class TempScreenMediator : IMediator
    {
        [Inject] private TempScreenView _view { get; set; }
        
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
            
            //@Register
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            //here you can use 'RemoveListeners'
            //_sampleSignals.Incoming.sampleSignal.RemoveListener(_view.SampleTest);
            
            //@Remove
        }

        // if you want to use a signal dispatch for a button click
        // you have to check if screen is available. Example:
        //
        // if (_view.Data.State == ScreenState.AvailableToSendSignal)
        
        //@Methods
    }
}