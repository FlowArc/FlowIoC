#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// One button in a row of them: what it says, what it does, whether it can be pressed now,
    /// and whether it destroys something - a destructive one is tinted so a reader's hand slows
    /// before the file is gone.
    /// </summary>
    public readonly struct ModulePanelAction
    {
        public ModulePanelAction(string label, Action onClick, bool enabled = true, bool destructive = false)
        {
            Label = label;
            OnClick = onClick;
            Enabled = enabled;
            Destructive = destructive;
        }

        public string Label { get; }

        public Action OnClick { get; }

        public bool Enabled { get; }

        public bool Destructive { get; }
    }
}

#endif
