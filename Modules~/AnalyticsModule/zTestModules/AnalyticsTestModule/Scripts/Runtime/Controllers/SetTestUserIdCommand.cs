#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.AnalyticsTestModule.Controllers
{
    /// <summary>A user id is never known at binding time, so it is a Command calling the interface.</summary>
    public class SetTestUserIdCommand : Command
    {
        [Inject] private IAnalyticsService _analytics { get; set; }

        public override void Execute() => _analytics.SetUserId("player-" + DateTime.Now.ToString("HHmmss"));
    }
}

#endif
