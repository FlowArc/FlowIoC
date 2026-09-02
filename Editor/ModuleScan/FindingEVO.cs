#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleScan
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

        internal FindingEVO(string checkId, ModuleCheckStatus status, string message)
        {
            CheckId = checkId;
            Status = status;
            Message = message;
        }

        internal static FindingEVO Ok(string checkId, string message) =>
            new FindingEVO(checkId, ModuleCheckStatus.Ok, message);

        internal static FindingEVO Fixable(string checkId, string message) =>
            new FindingEVO(checkId, ModuleCheckStatus.Fixable, message);

        internal static FindingEVO Manual(string checkId, string message) =>
            new FindingEVO(checkId, ModuleCheckStatus.Manual, message);
    }
}

#endif
