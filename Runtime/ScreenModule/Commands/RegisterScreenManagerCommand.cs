using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Config;

namespace FlowIoC.ScreenModule.Commands
{
    internal class RegisterScreenManagerCommand : Command
    {
        [Inject] private IScreenConfigModel _screenConfigModel { get; set; }
        [SignalParam] private ScreenManagerVO _manager { get; set; }
        [SignalParam] private IContext _registeredContext { get; set; }

        public override void Execute()
        {
            _screenConfigModel.RegisterScreenManager(_manager, _registeredContext);
        }
    }
}