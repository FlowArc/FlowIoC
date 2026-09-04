#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// What the panel draws and what Fix All is allowed to touch, in one enum.
    ///
    /// Fixable is the only status the repair acts on. Manual is a problem whose repair would
    /// cascade past what a scan should decide on its own - renaming an assembly moves every
    /// asmdef that references it by name and the root .csproj.DotSettings named after it - so it
    /// is reported and left to a person.
    ///
    /// The order matters: a row takes the worst status among its findings, and that is computed
    /// by comparing these values.
    /// </summary>
    internal enum ModuleCheckStatus
    {
        Ok,
        Fixable,
        Manual
    }
}

#endif
