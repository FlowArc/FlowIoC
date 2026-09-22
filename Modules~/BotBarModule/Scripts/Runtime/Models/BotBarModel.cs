using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.BotBarModule.Shared.Data.UnityObjects;
using Modules.BotBarModule.Shared.Data.ValueObjects;
using Modules.BotBarModule.Shared.Enums;

namespace Modules.BotBarModule.Models
{
    /// <summary>
    /// Owns RD_BotBar and resets it from CD_BotBar when the module constructs. Both assets come
    /// through ISharedDataModel because the screen module reads them the same way; a miss is
    /// reported there, once, so nothing is logged again here.
    /// </summary>
    internal class BotBarModel : IBotBarModel, IConstructable
    {
        [Inject] private ISharedDataModel _sharedData { get; set; }

        private CD_BotBar _config;
        private RD_BotBar _runtime;

        public bool IsPostConstructed { get; set; }

        public bool IsDeconstructed { get; set; }

        public CD_BotBar Config => _config;

        public IReadOnlyList<BotBarTabRVO> Tabs => _runtime.Tabs;

        public string Selected => _runtime.Selected;

        public string StartTab { get; private set; }

        public bool IsShown => _runtime.IsShown;

        public void PostConstruct()
        {
            CD_BotBar config = _sharedData.GetScriptable<CD_BotBar>();
            RD_BotBar runtime = _sharedData.GetScriptable<RD_BotBar>();

            if (config == null || runtime == null)
                return;

            Load(config, runtime);
        }

        /// <summary>Resets the runtime asset from the config. Internal so a test loads without a Root.</summary>
        internal void Load(CD_BotBar config, RD_BotBar runtime)
        {
            _config = config;
            _runtime = runtime;

            runtime.Tabs.Clear();
            var seen = new HashSet<string>();

            foreach (BotBarTabCVO tab in config.Tabs)
            {
                if (string.IsNullOrWhiteSpace(tab.Key))
                {
                    FlowLogger.LogError($"{config.name} - a tab has an empty key; it is skipped.");
                    continue;
                }

                if (!seen.Add(tab.Key))
                {
                    FlowLogger.LogError($"{config.name} - the key '{tab.Key}' is on two tabs; the second is skipped.");
                    continue;
                }

                runtime.Tabs.Add(new BotBarTabRVO {Key = tab.Key, State = BotBarTabState.Open, Badge = 0});
            }

            runtime.IsShown = true;

            if (runtime.Tabs.Count == 0)
            {
                FlowLogger.LogError($"{config.name} lists no tab.");
                StartTab = null;
                runtime.Selected = null;
                return;
            }

            if (HasTab(config.StartTab))
            {
                StartTab = config.StartTab;
            }
            else
            {
                StartTab = runtime.Tabs[0].Key;
                FlowLogger.LogError($"{config.name} - StartTab '{config.StartTab}' is on no tab; '{StartTab}' is selected instead.");
            }

            runtime.Selected = StartTab;
        }

        public bool HasTab(string key) => GetTab(key) != null;

        public BotBarTabRVO GetTab(string key)
        {
            if (key == null)
                return null;

            foreach (BotBarTabRVO tab in _runtime.Tabs)
            {
                if (tab.Key == key)
                    return tab;
            }

            return null;
        }

        public string Select(string key)
        {
            string previous = _runtime.Selected;

            if (HasTab(key))
                _runtime.Selected = key;

            return previous;
        }

        public bool MarkSeen(string key)
        {
            BotBarTabRVO tab = GetTab(key);

            if (tab == null || tab.State != BotBarTabState.NewlyUnlocked)
                return false;

            tab.State = BotBarTabState.Open;
            return true;
        }

        public bool SetLocked(string key, bool locked, out BotBarTabState state)
        {
            BotBarTabRVO tab = GetTab(key);
            state = tab?.State ?? BotBarTabState.Open;

            if (tab == null)
                return false;

            BotBarTabState next = tab.State;

            if (locked)
                next = BotBarTabState.Locked;
            else if (tab.State == BotBarTabState.Locked)
                next = _config.Options.NewMarkOnUnlock ? BotBarTabState.NewlyUnlocked : BotBarTabState.Open;

            if (next == tab.State)
                return false;

            tab.State = next;
            state = next;
            return true;
        }

        public bool SetBadge(string key, int count)
        {
            BotBarTabRVO tab = GetTab(key);

            if (tab == null)
                return false;

            int next = Math.Max(0, count);

            if (next == tab.Badge)
                return false;

            tab.Badge = next;
            return true;
        }

        public bool SetShown(bool shown)
        {
            if (_runtime.IsShown == shown)
                return false;

            _runtime.IsShown = shown;
            return true;
        }
    }
}
