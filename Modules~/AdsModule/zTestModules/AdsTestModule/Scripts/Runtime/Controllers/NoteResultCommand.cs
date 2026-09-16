#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.AdsTestModule.Models;
using Modules.AdsModule.Data.ValueObjects;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>The interstitial step releases with its result on every outcome; this notes it for the label.</summary>
    public class NoteResultCommand : Command<AdResultVO>
    {
        [Inject] private AdsTestModel _testModel { get; set; }

        public override void Execute(AdResultVO result)
        {
            _testModel.LastResult = result.ToString();
            FlowLogger.Log($"Interstitial ended - {result}");
        }
    }
}

#endif
