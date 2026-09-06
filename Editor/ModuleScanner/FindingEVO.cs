#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// One check's answer about one target. The id is the check's own, which is how the repair
    /// finds its way back from a finding to the check that made it without the report having to
    /// carry the check itself.
    /// </summary>
    internal class FindingEVO
    {
        internal string CheckId { get; }
        internal ModuleCheckStatus Status { get; }
        internal string Message { get; }

        /// <summary>
        /// The asset this finding is about, relative to the project, or null when there is
        /// nothing to point at. The panel turns a finding that carries one into a row that pings
        /// the file - which is also what makes the row light up under the pointer, because a row
        /// only does that when clicking it does something.
        /// </summary>
        internal string AssetPath { get; }

        internal FindingEVO(string checkId, ModuleCheckStatus status, string message, string assetPath = null)
        {
            CheckId = checkId;
            Status = status;
            Message = message;
            AssetPath = assetPath;
        }

        internal static FindingEVO Ok(string checkId, string message, string assetPath = null) =>
            new FindingEVO(checkId, ModuleCheckStatus.Ok, message, assetPath);

        internal static FindingEVO Fixable(string checkId, string message, string assetPath = null) =>
            new FindingEVO(checkId, ModuleCheckStatus.Fixable, message, assetPath);

        internal static FindingEVO Manual(string checkId, string message, string assetPath = null) =>
            new FindingEVO(checkId, ModuleCheckStatus.Manual, message, assetPath);
    }
}

#endif