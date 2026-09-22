using System.Collections.Generic;
using Modules.BotBarModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.BotBarModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The bar as authored: the tabs, which one opens selected, and every switch. Filed in the
    /// Shared Scriptables of BotBarSystemRoot, because the screen module reads it too. The tab
    /// count is this list's count.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_BotBar", menuName = "FlowIoC/BotBarModule/Data/CD_BotBar")]
    public class CD_BotBar : ScriptableObject
    {
        public List<BotBarTabCVO> Tabs = new();

        [Tooltip("The key of the tab selected when the bar opens.")]
        public string StartTab;

        public BotBarOptionsCVO Options = new();
    }
}
