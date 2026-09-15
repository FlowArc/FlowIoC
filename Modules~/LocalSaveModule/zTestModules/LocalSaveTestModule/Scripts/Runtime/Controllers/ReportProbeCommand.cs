#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LocalSaveModule.LocalSaveTestModule.Models;
using Modules.LocalSaveModule.LocalSaveTestModule.Signals;

namespace Modules.LocalSaveModule.LocalSaveTestModule.Controllers
{
    internal class ReportProbeCommand : Command
    {
        [Inject] private ILocalSaveTestModel _model { get; set; }

        [InjectSignal] private LocalSaveTestSignals _signals { get; set; }

        public override void Execute() => _signals.Outgoing.CounterChanged.Dispatch(_model.Counter);
    }
}

#endif
