#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Models;
using Modules.AbTestFlowModule.Shared.Constants;
using UnityEngine;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Controllers
{
    /// <summary>
    /// Forgets this player's assignment. The key is the one the module writes, built from the
    /// prefix it publishes, so the next roll - Reroll here, or the next launch - is a fresh one.
    /// </summary>
    public class ClearStoredAbTestCommand : Command
    {
        [Inject] private IAbTestFlowTestModel _model { get; set; }

        public override void Execute()
        {
            PlayerPrefs.DeleteKey(AbTestConstants.PrefsPrefix + _model.AbTestId);
            PlayerPrefs.Save();
        }
    }
}

#endif
