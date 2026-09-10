#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Data.ValueObjects;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Models;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Signals;
using Modules.AbTestFlowModule.Services;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Controllers
{
    /// <summary>
    /// Reads the group the way any game module would - through the injected service - and pairs
    /// it with what the probe holds right now.
    /// </summary>
    public class ReportStateCommand : Command
    {
        [Inject] private IAbTestFlowService   _abTests { get; set; }
        [Inject] private IAbTestFlowTestModel _model   { get; set; }

        [InjectSignal] private AbTestFlowTestInternalSignals _signals { get; set; }

        public override void Execute()
        {
            _signals.StateChanged.Dispatch(new AbTestFlowTestStateVO
            {
                AbTestId = _model.AbTestId,
                Version = _model.Version,
                Group = _abTests.GetGroup(_model.AbTestId),
                Lives = _model.Probe.Lives,
                Speed = _model.Probe.Speed
            });
        }
    }
}

#endif
