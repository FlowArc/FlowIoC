using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Models;

namespace Modules.AdsModule.Controllers
{
    /// <summary>The reward was earned. The show is not over yet - Closed ends it - so only the fact is kept.</summary>
    internal class RecordRewardCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }

        [SignalParam] private AdRewardVO _reward { get; set; }

        public override void Execute()
        {
            ShowVO current = _model.Current;

            if (current == null)
            {
                FlowLogger.Log($"Reward earned - {_reward} with no show in progress; ignored.");
                return;
            }

            current.Rewarded = true;
            current.Reward = _reward;
            FlowLogger.Log($"Reward earned - {_reward} ({current})");
        }
    }
}
