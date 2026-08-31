#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Which package a deferred install is to copy from. Adding a package reloads the domain, so
    /// the intent has to survive as strings in SessionState and be read back afterwards - and a
    /// private module lives in a package that is not FlowIoC, so where is part of the intent
    /// rather than a constant.
    ///
    /// A payload that names neither half is what every intent written before private modules
    /// existed looks like, and it resumes against the modules FlowIoC ships, as it always did.
    /// </summary>
    internal class PendingInstallPayload
    {
        private readonly string _packageRoot;
        private readonly string _folder;

        internal PendingInstallPayload(string packageRoot, string folder)
        {
            _packageRoot = packageRoot;
            _folder = folder;
        }

        internal string PackageRoot => _packageRoot ?? string.Empty;

        internal string Folder => _folder ?? string.Empty;

        internal bool IsComplete =>
            !string.IsNullOrEmpty(_packageRoot) && !string.IsNullOrEmpty(_folder);

        internal ModulesSource Source() =>
            IsComplete ? new ModulesSource(_packageRoot, _folder) : new ModulesSource();
    }
}

#endif
