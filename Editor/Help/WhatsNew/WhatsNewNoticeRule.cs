#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>What the startup hook does about the version that is installed.</summary>
    internal enum WhatsNewDecision
    {
        /// <summary>Nothing to do: the reader is on the version they were on.</summary>
        Stop,

        /// <summary>Show nothing, but remember the version this reader is now on.</summary>
        RecordOnly,

        /// <summary>Open the window on What's New, then remember the version.</summary>
        Show
    }

    /// <summary>
    /// The rule the startup hook follows, with nothing of Unity in it so that it can be read and
    /// tested on its own.
    /// </summary>
    internal class WhatsNewNoticeRule
    {
        internal WhatsNewDecision For(string installedVersion, string lastSeenVersion)
        {
            // An unresolved package has no version to compare or to record, which is what an
            // embedded copy outside the Package Manager looks like.
            if (string.IsNullOrEmpty(installedVersion))
                return WhatsNewDecision.Stop;

            // Nothing recorded means this reader has never opened this project with FlowIoC in
            // it. What they want then is the introduction, so the version is recorded quietly
            // and the next update is the first one they are shown.
            if (string.IsNullOrEmpty(lastSeenVersion))
                return WhatsNewDecision.RecordOnly;

            return installedVersion == lastSeenVersion
                ? WhatsNewDecision.Stop
                : WhatsNewDecision.Show;
        }
    }
}

#endif