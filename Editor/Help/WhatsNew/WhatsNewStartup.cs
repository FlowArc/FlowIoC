#if UNITY_EDITOR
using FlowIoC.Editor.Help.Pages;
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
        private const string SESSION_KEY = "FlowIoC.WhatsNew.Checked";

        private readonly WhatsNewSource _source;
        private readonly LastSeenVersion _lastSeen;

        internal WhatsNewStartup() : this(new WhatsNewSource(), new LastSeenVersion())
        {
        }

        internal WhatsNewStartup(WhatsNewSource source, LastSeenVersion lastSeen)
        {
            _source = source;
            _lastSeen = lastSeen;
        }

        internal void Run()
        {
            // A batch run has nobody to read the notes and no window to draw them in.
            if (Application.isBatchMode)
                return;

            // A domain reload is not an update. Without this the window would come back every
            // time a script compiles, until the version is recorded.
            if (SessionState.GetBool(SESSION_KEY, false))
                return;

            SessionState.SetBool(SESSION_KEY, true);

            string installed = _source.Version;
            WhatsNewDecision decision = new WhatsNewNoticeRule().For(installed, _lastSeen.Read());

            if (decision == WhatsNewDecision.Stop)
                return;

            if (decision == WhatsNewDecision.Show)
                HelpWindow.OpenPage("Welcome", WelcomePage.WHATS_NEW_TAB);

            _lastSeen.Write(installed);
        }
    }
}

#endif
