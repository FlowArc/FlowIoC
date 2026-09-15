#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LocalSaveModule.LocalSaveTestModule.Models;

namespace Modules.LocalSaveModule.LocalSaveTestModule.Controllers
{
    /// <summary>
    /// Changes the data. Writing it is the next step of the sequence - the module's own
    /// <c>ILocalSaveService.Commands.Save</c>, bound in the test context with the name the asset is
    /// filed under on LocalSaveRoot's adapter - so the flow reads from the context: increment, save,
    /// report.
    /// </summary>
    internal class IncrementProbeCommand : Command
    {
        [Inject] private ILocalSaveTestModel _model { get; set; }

        public override void Execute() => _model.Increment();
    }
}

#endif
