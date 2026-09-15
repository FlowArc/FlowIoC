#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.RootsContexts;
using UnityEngine;

namespace Modules.MobileNotificationModule.MobileNotificationTestModule.Controllers
{
    /// <summary>The other half of SimulateLeaveCommand: the service Root's context, resumed.</summary>
    public class SimulateReturnCommand : Command
    {
        public override void Execute()
        {
            var root = Object.FindFirstObjectByType<MobileNotificationServiceRoot>();

            if (root == null || root.Context == null)
            {
                FlowLogger.LogError("No MobileNotificationServiceRoot in the scene to resume.");
                return;
            }

            root.Context.ResumeContext();
        }
    }
}

#endif
