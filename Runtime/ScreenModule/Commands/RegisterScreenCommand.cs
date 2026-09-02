using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Model.Registry;

namespace FlowIoC.ScreenModule.Commands
{
    internal class RegisterScreenCommand : Command
    {
        [Inject] private IScreenRegistryModel _registry { get; set; }
        [SignalParam] private ScreenEntry _entry { get; set; }

        public override void Execute()
        {
            _registry.RegisterScreen(_entry);
        }
    }
}
