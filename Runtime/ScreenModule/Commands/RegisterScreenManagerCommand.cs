using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Registry;

namespace FlowIoC.ScreenModule.Commands
{
    internal class RegisterScreenManagerCommand : Command
    {
        [Inject] private IScreenRegistryModel _registry { get; set; }
        [SignalParam] private ScreenManagerVO _manager { get; set; }

        public override void Execute()
        {
            _registry.RegisterScreenManager(_manager);
        }
    }
}