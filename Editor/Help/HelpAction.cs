#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// The one thing a page can offer to do, drawn as a button on the right of its banner. A page
    /// that only explains something has none; a page about a module that can be installed has the
    /// button that installs it.
    ///
    /// The label and the enabled state are asked for rather than stored, because both change while
    /// the window is open - a module installs, and the button that offered it has nothing left to
    /// offer. What colour it wears is the theme's business, not the page's.
    /// </summary>
    internal class HelpAction
    {
        private readonly Func<string> _label;
        private readonly Func<bool> _enabled;
        private readonly Action _perform;

        public HelpAction(Func<string> label, Func<bool> enabled, Action perform)
        {
            _label = label;
            _enabled = enabled;
            _perform = perform;
        }

        public string Label => _label();

        public bool Enabled => _enabled();

        public void Perform() => _perform();
    }
}

#endif