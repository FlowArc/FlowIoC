#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Data.ValueObjects;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Models;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Signals;
using Modules.AbTestFlowModule.Services;
using Modules.AbTestFlowModule.Shared.Data.UnityObjects;
using Modules.AbTestFlowModule.Shared.Data.ValueObjects;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Controllers
{
    /// <summary>
    /// Reads the group both ways a game module can - through the injected service, and off
    /// RD_AbTestStatus through ISharedDataModel, the asset the service Root files as shared - and
    /// pairs them with what the probe holds right now. The two should agree; the scene shows both
    /// so that a disagreement is seen.
    /// </summary>
    public class ReportStateCommand : Command
    {
        [Inject] private IAbTestFlowService _abTests { get; set; }
        [Inject] private IAbTestFlowTestModel _model { get; set; }
        [Inject] private ISharedDataModel _sharedData { get; set; }

        [InjectSignal] private AbTestFlowTestInternalSignals _signals { get; set; }

        public override void Execute()
        {
            _signals.StateChanged.Dispatch(new AbTestFlowTestStateVO
            {
                AbTestId = _model.AbTestId,
                Version = _model.Version,
                Group = _abTests.GetGroup(_model.AbTestId),
                SharedGroup = SharedGroup(),
                Lives = _model.Probe.Lives,
                Speed = _model.Probe.Speed
            });
        }

        private string SharedGroup()
        {
            RD_AbTestStatus status = _sharedData.GetScriptable<RD_AbTestStatus>();
            AbTestStatusRVO standing = status != null ? status.Get(_model.AbTestId) : null;

            return standing != null && standing.IsInTest ? standing.Group : null;
        }
    }
}

#endif