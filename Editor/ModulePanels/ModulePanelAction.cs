#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// One button in a row of them: what it says, what it does, whether it can be pressed now,
    /// and what kind of thing it does - a destructive one is tinted so a reader's hand slows
    /// before the file is gone, and one that adds something stands apart on the right, larger
    /// and green, so the way to make more is found without reading the row.
    /// </summary>
    public readonly struct ModulePanelAction
    {
        public ModulePanelAction(string label, Action onClick, bool enabled = true, bool destructive = false)
            : this(label, onClick, destructive ? ModulePanelActionKind.Remove : ModulePanelActionKind.Plain, enabled)
        {
        }

        public ModulePanelAction(string label, Action onClick, ModulePanelActionKind kind, bool enabled = true)
        {
            Label = label;
            OnClick = onClick;
            Kind = kind;
            Enabled = enabled;
        }

        /// <summary>The button that adds something: drawn on the right of its row, larger and green.</summary>
        public static ModulePanelAction Add(string label, Action onClick, bool enabled = true) =>
            new ModulePanelAction(label, onClick, ModulePanelActionKind.Add, enabled);

        public string Label { get; }

        public Action OnClick { get; }

        public ModulePanelActionKind Kind { get; }

        public bool Enabled { get; }

        public bool Destructive => Kind == ModulePanelActionKind.Remove;
    }

    /// <summary>
    /// What a button does, which decides its colour and its place in the row. Add and Remove stand
    /// apart at the right edge; Confirm and Caution stay in line with the plain ones and only
    /// wear a colour - green for the button that switches something on, amber for one that
    /// changes the shape or the state of what it sits on.
    /// </summary>
    public enum ModulePanelActionKind
    {
        Plain = 0,
        Add = 1,
        Remove = 2,
        Confirm = 3,
        Caution = 4
    }
}

#endif