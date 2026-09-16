#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.AdsTestModule.Models;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>The last Outgoing the test context heard, kept for the label.</summary>
    public class NoteOutgoingCommand : Command
    {
        [Inject] private AdsTestModel _testModel { get; set; }

        [SignalParam] private string _text { get; set; }

        public override void Execute() => _testModel.LastOutgoing = _text;
    }
}

#endif
