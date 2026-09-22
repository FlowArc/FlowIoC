using System.Collections.Generic;
using Modules.BotBarModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.BotBarModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The bar's state during play, reset from CD_BotBar when the module constructs. Filed beside
    /// the config in the Root's Shared Scriptables so the screen's opening Command reads it.
    /// </summary>
    [CreateAssetMenu(fileName = "RD_BotBar", menuName = "FlowIoC/BotBarModule/Data/RD_BotBar")]
    public class RD_BotBar : ScriptableObject
    {
        public List<BotBarTabRVO> Tabs = new();
        public string Selected;
        public bool IsShown;
    }
}
