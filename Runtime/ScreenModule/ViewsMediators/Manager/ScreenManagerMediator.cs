using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Signals;

namespace FlowIoC.ScreenModule.ViewsMediators.Manager
{
    internal class ScreenManagerMediator : IMediator
    {
        [Inject] private ScreenManager _view { get; set; }
        [InjectSignal] private ScreenServiceInternalSignals _screenServiceInternalSignals { get; set; }

        public void OnRegister()
        {
            ScreenManagerVO manager = _view.ManagerData;

            FlowLogger.Log(SystemLogType.Screen, $"Registering screen manager with ID: {manager.ManagerID}");
            _screenServiceInternalSignals.RegisterManager.Dispatch(manager);
        }

        public void OnRemove()
        {
        }
    }
}