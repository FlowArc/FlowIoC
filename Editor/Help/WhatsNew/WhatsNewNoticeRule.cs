#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>What the startup hook does about the version that is installed.</summary>
    internal enum WhatsNewDecision
    {
        /// <summary>Nothing to do: the reader is on the version they were on.</summary>
        Stop,

        /// <summary>
        /// Open the window on Welcome's introduction, then remember the version. This reader is
        /// meeting FlowIoC, and what changed in a package they have not used yet means nothing to
        /// them.
        /// </summary>
        Introduce,

        /// <summary>Open the window on What's New, then remember the version.</summary>
        Show
    }

    /// <summary>
    /// The rule the startup hook follows, with nothing of Unity in it so that it can be read and
    /// tested on its own.
    /// </summary>
    internal class WhatsNewNoticeRule
    {
        /// <summary>
        /// <paramref name="setupVersion"/> is the version recorded in the project's setup marker,
        /// which only matters when the reader has seen nothing. It is what tells a project that has
        /// had FlowIoC in it for a while from one that has just met it.
        /// </summary>
        internal WhatsNewDecision For(string installedVersion, string lastSeenVersion, string setupVersion)
        {
            // An unresolved package has no version to compare or to record, which is what an
            // embedded copy outside the Package Manager looks like.
            if (string.IsNullOrEmpty(installedVersion))
                return WhatsNewDecision.Stop;

            if (string.IsNullOrEmpty(lastSeenVersion))
                return FirstTimeSeen(installedVersion, setupVersion);

            return installedVersion == lastSeenVersion
                ? WhatsNewDecision.Stop
                : WhatsNewDecision.Show;
        }

        /// <summary>
        /// Nothing recorded means one of two things, and the reader wants opposite answers to them.
        /// Somebody meeting FlowIoC wants the introduction, not a list of what changed in a package
        /// they have not used yet. Somebody whose project has been on FlowIoC for a while and has
        /// only now updated to a version that keeps this record wants exactly those notes - and
        /// without this, the release that introduces What's New could never announce itself to
        /// anyone, because every existing reader has nothing recorded on the day it lands.
        ///
        /// The project's setup marker separates them: it carries the version the setup modules were
        /// installed at, so a marker naming an older version is the project saying it has been here
        /// before. No marker, or one naming the version now installed, is a first meeting.
        /// </summary>
        private WhatsNewDecision FirstTimeSeen(string installedVersion, string setupVersion)
        {
            return !string.IsNullOrEmpty(setupVersion) && setupVersion != installedVersion
                ? WhatsNewDecision.Show
                : WhatsNewDecision.Introduce;
        }
    }
}

#endif