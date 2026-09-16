#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>How the What's New tab draws the line about the latest release.</summary>
    internal enum UpdateNoticeKind
    {
        /// <summary>Nothing to say: no version to compare, or the latest release is not known yet.</summary>
        None = 0,

        /// <summary>A quiet line: this project is on the latest release, or ahead of it.</summary>
        UpToDate = 1,

        /// <summary>A note: a newer release is out, with the road to it for the way FlowIoC was installed.</summary>
        Behind = 2
    }

    /// <summary>The line about the latest release, as the What's New tab draws it.</summary>
    internal class UpdateNoticeEVO
    {
        internal UpdateNoticeEVO(UpdateNoticeKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }

        internal UpdateNoticeKind Kind { get; }

        internal string Text { get; }
    }
}

#endif
