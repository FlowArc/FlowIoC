#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.AdsTestModule.Models;
using Modules.AdsModule.Data.ValueObjects;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>
    /// The grant a game binds behind the rewarded step. It only prints: when the ad was
    /// dismissed the step stopped the sequence, and this line is the one that did not appear.
    /// </summary>
    public class ReportRewardGrantedCommand : Command<AdResultVO>
    {
        [Inject] private AdsTestModel _testModel { get; set; }

        public override void Execute(AdResultVO result)
        {
            _testModel.LastResult = result.ToString();
            FlowLogger.Log($"Reward granted - {result}");
        }
    }
}

#endif
