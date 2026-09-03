#if UNITY_EDITOR
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.Help.Pages;
using FlowIoC.Editor.SetupModules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="WhatsNewStartup"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class WhatsNewStartupHook
    {
        static WhatsNewStartupHook()
        {
            EditorApplication.delayCall += () => new WhatsNewStartup().Run();
        }
    }

    /// <summary>
    /// Shows what changed, once, after the package has been updated.
    ///
    /// The Help window opens on the introduction, which is what somebody meeting FlowIoC wants
    /// and what nobody wants on the session after an update. So a reader who is on a version they
    /// have not seen the notes for is opened on Welcome's What's New instead, and the version is
    /// remembered so that the next Editor launch is quiet again.
    /// </summary>
    internal class WhatsNewStartup
    {
        private const string SESSION_KEY = "FlowIoC.WhatsNew.CheckedVersion";

        private readonly WhatsNewSource _source;
        private readonly LastSeenVersion _lastSeen;
        private readonly SetupState _setup;

        internal WhatsNewStartup() : this(
            new WhatsNewSource(),
            new LastSeenVersion(),
            new SetupState(new ProjectRoot().Resolve()))
        {
        }

        internal WhatsNewStartup(WhatsNewSource source, LastSeenVersion lastSeen, SetupState setup)
        {
            _source = source;
            _lastSeen = lastSeen;
            _setup = setup;
        }

        internal void Run()
        {
            // A batch run has nobody to read the notes and no window to draw them in.
            if (Application.isBatchMode)
                return;

            string installed = _source.Version;

            // A domain reload is not an update, and without a guard the window would come back
            // every time a script compiles. What the session has to remember is which version it
            // answered for, not that it answered: SessionState survives a domain reload and is
            // cleared only when the Editor restarts, and updating the package is a domain reload
            // inside the same session - so a bare flag would swallow the one event this exists for
            // and leave the notes until the reader next opened the Editor.
            if (SessionState.GetString(SESSION_KEY, string.Empty) == installed)
                return;

            SessionState.SetString(SESSION_KEY, installed);

            WhatsNewDecision decision = new WhatsNewNoticeRule()
                .For(installed, _lastSeen.Read(), _setup.InstalledVersion());

            if (decision == WhatsNewDecision.Stop)
                return;

            if (decision == WhatsNewDecision.Show)
                HelpWindow.OpenPage("Welcome", WelcomePage.WHATS_NEW_TAB);

            _lastSeen.Write(installed);
        }
    }
}

#endif