#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;
using Modules.MobileNotificationModule.RootsContexts;
using UnityEngine;

namespace Modules.MobileNotificationModule.MobileNotificationTestModule.Controllers
{
    /// <summary>
    /// The Editor never pauses, so this calls what Unity's OnApplicationPause would reach: the
    /// service Root's context, paused.
    /// </summary>
    public class SimulateLeaveCommand : Command
    {
        public override void Execute()
        {
            var root = Object.FindFirstObjectByType<MobileNotificationServiceRoot>();

            if (root == null || root.Context == null)
            {
                FlowLogger.LogError("No MobileNotificationServiceRoot in the scene to pause.");
                return;
            }

            root.Context.PauseContext();
        }
    }
}

#endif
