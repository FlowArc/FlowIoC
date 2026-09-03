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
    /// Opens the Help window once, on the reading the moment calls for, and remembers the version
    /// so that the next Editor launch is quiet again.
    ///
    /// A project meeting FlowIoC lands on Welcome's introduction: it has just been given four
    /// modules, an AGENTS.md and a set of skills, and nothing else in the Editor says where to
    /// start. A project that has had FlowIoC for a while and has just updated lands on What's New
    /// instead, because what it wants is the difference rather than the introduction it has
    /// already read.
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

            HelpWindow.OpenPage(WelcomePage.PAGE_TITLE, TabFor(decision));

            _lastSeen.Write(installed);
        }

        private string TabFor(WhatsNewDecision decision)
        {
            return decision == WhatsNewDecision.Show
                ? WelcomePage.WHATS_NEW_TAB
                : WelcomePage.INTRODUCTION_TAB;
        }
    }
}

#endif