#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// One PrivateModulePage as the help window sees it. The page declares; this does the work -
    /// resolving the package the module ships in, checking whether it is already installed,
    /// deciding what the button says, adding packages that are missing, and copying the module
    /// in. Written once here rather than once per module, which is the whole reason a private
    /// package can get away with declaring so little.
    /// </summary>
    internal class PrivateModulePageAdapter : HelpPage
    {
        private readonly PrivateModulePage _page;
        private readonly PrivateModulePayload _payload;
        private readonly ModuleInstaller _installer;
        private readonly HelpAction _install;

        private bool _isInstalled;
        private double _checkedAt = double.NegativeInfinity;

        internal PrivateModulePageAdapter(PrivateModulePage page)
            : this(page, new ProjectRoot().Resolve(), new PrivateModulePayload(page.GetType().Assembly))
        {
        }

        internal PrivateModulePageAdapter(
            PrivateModulePage page, string projectRoot, PrivateModulePayload payload)
            : base(null)
        {
            _page = page;
            _payload = payload;

            _installer = payload.IsResolved
                ? new ModuleInstaller(projectRoot, payload.Source())
                : null;

            // The label and the enabled state are read every repaint rather than fixed here, so
            // the button turns itself off the moment the module lands in the project.
            _install = new HelpAction(() => State().Label, () => State().Enabled, Install);
        }

        public override string Title => _page.Title;

        public override string Subtitle => _page.Subtitle;

        public override string Icon => _page.Icon;

        public override HelpAction Action => _install;

        protected override string BodyTabTitle => _page.BodyTabTitle;

        protected override IReadOnlyList<HelpTab> MoreTabs => _page.MoreTabs;

        /// <summary>
        /// What the page says above its body and what its button reads, from the three readings
        /// that can change while the window is open.
        /// </summary>
        private PrivateModuleInstallState State() =>
            new PrivateModuleInstallState(_payload.IsResolved, IsInstalled(), AbsentAssemblies());

        private IReadOnlyList<string> AbsentAssemblies() =>
            new MissingAssemblies().In(new LoadedAssemblies().Names(), _page.RequiredAssemblies);

        /// <summary>
        /// Whether the module is in the project, from a cache that goes stale after a second. The
        /// check underneath walks every asmdef under Assets and the banner asks twice a repaint,
        /// which is the same bargain every module page in the package already makes.
        /// </summary>
        private bool IsInstalled()
        {
            if (_installer == null)
                return false;

            if (EditorApplication.timeSinceStartup - _checkedAt < 1d)
                return _isInstalled;

            _isInstalled = _installer.IsInstalled(_page.ModuleFolderName);
            _checkedAt = EditorApplication.timeSinceStartup;

            return _isInstalled;
        }

        /// <summary>
        /// Packages first. Copying a module whose asmdef references an assembly the project does
        /// not have stops the whole project compiling, so a missing package is asked about rather
        /// than discovered afterwards. A missing paid asset never gets this far: the button that
        /// would have started this is disabled.
        /// </summary>
        private void Install()
        {
            // Whatever happened, what the cache holds is now a guess about a project that has
            // changed underneath it.
            _checkedAt = double.NegativeInfinity;

            if (_installer == null)
                return;

            IReadOnlyList<string> missing =
                new MissingPackages().In(new InstalledPackages().Ids(), _page.RequiredPackages);

            if (missing.Count > 0)
            {
                bool add = EditorUtility.DisplayDialog(
                    _page.Title,
                    $"The module references {string.Join(" and ", missing)}, which this project "
                    + "does not have.\n\n"
                    + "Adding them writes to Packages/manifest.json and reimports the project. The "
                    + "module installs itself once that has finished.",
                    "Add and install",
                    "Cancel");

                if (add)
                {
                    new PendingModuleInstall().Begin(
                        _page.ModuleFolderName,
                        missing,
                        new PendingInstallPayload(_payload.PackageRoot, PrivateModulePayload.Folder));
                }

                return;
            }

            if (_installer.TryInstall(_page.ModuleFolderName, out string error))
            {
                EditorUtility.DisplayDialog(
                    $"{_page.Title} installed",
                    $"The module is now at {ModuleInstaller.TargetFolder}/{_page.ModuleFolderName}."
                    + "\n\nIt is yours to edit from here - the copy in the package is only the one "
                    + "installs are made from.",
                    "OK");

                return;
            }

            EditorUtility.DisplayDialog(_page.Title, error, "OK");
        }

        /// <summary>
        /// Whatever the page has to say about itself, under whatever this adapter has to say
        /// about whether it can be installed at all.
        /// </summary>
        protected override void DrawBody(HelpPainter painter)
        {
            string note = State().Note;

            if (!string.IsNullOrEmpty(note))
            {
                painter.Note(note);
                painter.Space();
            }

            _page.DrawBody(painter);
        }
    }
}

#endif
