#if UNITY_EDITOR

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Which package a deferred install is to copy from. Adding a package reloads the domain, so
    /// the intent has to survive as a string in SessionState and be read back afterwards - and a
    /// module may live in a package that is not FlowIoC, so where is part of the intent rather
    /// than a constant. Every package ships its modules under the same Modules~, so the package
    /// root is the whole of it.
    ///
    /// A payload that names no package is what every intent written before other packages could
    /// ship modules looks like, and it resumes against the modules FlowIoC ships, as it always did.
    /// </summary>
    internal class PendingInstallPayload
    {
        private readonly string _packageRoot;

        internal PendingInstallPayload(string packageRoot)
        {
            _packageRoot = packageRoot;
        }

        internal string PackageRoot => _packageRoot ?? string.Empty;

        internal bool IsComplete => !string.IsNullOrEmpty(_packageRoot);

        internal ModulesSource Source() =>
            IsComplete ? new ModulesSource(_packageRoot) : new ModulesSource();
    }
}

#endif