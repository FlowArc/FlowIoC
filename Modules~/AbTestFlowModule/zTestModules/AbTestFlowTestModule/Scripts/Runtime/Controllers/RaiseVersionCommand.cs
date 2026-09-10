#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Models;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Controllers
{
    public class RaiseVersionCommand : Command
    {
        [Inject] private IAbTestFlowTestModel _model { get; set; }

        public override void Execute() => _model.RaiseVersion();
    }
}

#endif
