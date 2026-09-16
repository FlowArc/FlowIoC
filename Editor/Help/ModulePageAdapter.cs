#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.Icons;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// One ModulePage as the help window sees it. The page declares; this does the work -
    /// resolving the package the module ships in, checking whether it is already installed and
    /// at which version, deciding what the button says, adding packages that are missing, copying
    /// the module in, and updating it when the package ships a newer one. Written once here
    /// rather than once per module, which is the whole reason a page can get away with declaring
    /// so little - FlowIoC's own pages included.
    /// </summary>
    internal class ModulePageAdapter : HelpPage
    {
        private readonly ModulePage _page;
        private readonly ModulePayload _payload;
        private readonly ModuleInstaller _installer;
        private readonly HelpAction _action;
        private readonly InstalledModuleRegistrar _registrar = new InstalledModuleRegistrar();
        private readonly ModuleLibraryArrivals _arrivals;

        private ReadingsEVO _readings;
        private double _readAt = double.NegativeInfinity;

        /// <summary>
        /// What the page reads off the project: taken at most once a second, because the check
        /// underneath walks every asmdef under Assets and reads two cards, and the banner and the
        /// sidebar ask several times a repaint.
        /// </summary>
        private class ReadingsEVO
        {
            internal bool Installed;
            internal string InstalledVersion;
            internal string ShippedVersion;
            internal bool IsNew;
        }

        internal ModulePageAdapter(ModulePage page)
            : this(page, new ProjectRoot().Resolve(), new ModulePayload(page.GetType().Assembly))
        {
        }

        internal ModulePageAdapter(ModulePage page, string projectRoot, ModulePayload payload)
            : base(null)
        {
            _page = page;
            _payload = payload;

            _installer = payload.IsResolved
                ? new ModuleInstaller(projectRoot, payload.Source())
                : null;

            _arrivals = new ModuleLibraryArrivals(payload.PackageName, payload.PackageVersion, projectRoot);

            // The label and the enabled state are read every repaint rather than fixed here, so
            // the button turns itself off the moment the module lands in the project.
            _action = new HelpAction(() => State().Label, () => State().Enabled, Act);
        }

        public override string Title => _page.Title;

        public override string Subtitle => _page.Subtitle;

        public override FlowIcon Icon => _page.Icon;

        public override HelpAction Action => _action;

        public override SidebarFlagEVO SidebarFlag
        {
            get
            {
                ModuleInstallState state = State();

                if (state.UpdateAvailable)
                    return SidebarFlagEVO.Update;

                return !state.Installed && Readings().IsNew ? SidebarFlagEVO.New : null;
            }
        }

        protected override string BodyTabTitle => _page.BodyTabTitle;

        protected override string BodyHeadline => _page.BodyHeadline;

        protected override string BodyTagline => _page.BodyTagline;

        protected override IReadOnlyList<HelpTab> MoreTabs => _page.MoreTabs;

        /// <summary>
        /// What the page says above its body and what its button reads, from the readings that
        /// can change while the window is open.
        /// </summary>
        private ModuleInstallState State()
        {
            ReadingsEVO readings = Readings();

            return new ModuleInstallState(_payload.IsResolved, readings.Installed, AbsentAssemblies(),
                readings.InstalledVersion, readings.ShippedVersion);
        }

        private IReadOnlyList<string> AbsentAssemblies() =>
            new MissingAssemblies().In(new LoadedAssemblies().Names(), _page.RequiredAssemblies);

        private ReadingsEVO Readings()
        {
            if (EditorApplication.timeSinceStartup - _readAt < 1d && _readings != null)
                return _readings;

            var readings = new ReadingsEVO();

            if (_installer != null)
            {
                readings.Installed = _installer.IsInstalled(_page.ModuleFolderName);
                readings.InstalledVersion = _installer.InstalledVersionOf(_page.ModuleFolderName);
                readings.ShippedVersion = _installer.ShippedVersionOf(_page.ModuleFolderName);
                readings.IsNew = !readings.Installed && IsNew();
            }

            _readings = readings;
            _readAt = EditorApplication.timeSinceStartup;

            return readings;
        }

        /// <summary>
        /// Whether this module arrived with the package version now in the project. A payload
        /// that names no package has no version to have arrived with, and keeps no record.
        /// </summary>
        private bool IsNew()
        {
            if (string.IsNullOrEmpty(_payload.PackageName))
                return false;

            if (!_payload.Source().TryList(out string[] folders, out _))
                return false;

            var names = new string[folders.Length];

            for (int index = 0; index < folders.Length; index++)
                names[index] = System.IO.Path.GetFileName(folders[index]);

            foreach (string name in _arrivals.NewFolders(names))
            {
                if (name == _page.ModuleFolderName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// What the dialog says once the module is in: where it landed, that the copy is the
        /// reader's now, and whatever pointer the page adds to that.
        /// </summary>
        internal string InstalledMessage()
        {
            string message =
                $"The module is now at {ModuleInstaller.TargetFolder}/{_page.ModuleFolderName}."
                + "\n\nIt is yours to edit from here - the copy in the package is only the one "
                + "installs are made from.";

            return string.IsNullOrEmpty(_page.InstalledHint)
                ? message
                : message + " " + _page.InstalledHint;
        }

        private void Act()
        {
            if (State().UpdateAvailable)
                Update();
            else
                Install();
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
            _readAt = double.NegativeInfinity;

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
                        new PendingInstallPayload(_payload.PackageRoot));
                }

                return;
            }

            if (_installer.TryInstall(_page.ModuleFolderName, out string error))
            {
                EditorUtility.DisplayDialog($"{_page.Title} installed", InstalledMessage(), "OK");

                return;
            }

            EditorUtility.DisplayDialog(_page.Title, error, "OK");
        }

        /// <summary>
        /// Plan, ask, apply. Nothing is written before the dialog, and the dialog describes
        /// exactly the pass that follows it. One policy for every conflict: a reader who wants
        /// finer control commits first and reads the diff.
        /// </summary>
        private void Update()
        {
            _readAt = double.NegativeInfinity;

            if (_installer == null)
                return;

            string installedAt = _installer.InstalledAt(_page.ModuleFolderName);
            string shipped = _installer.ShippedPathOf(_page.ModuleFolderName);

            if (installedAt == null || !System.IO.Directory.Exists(shipped))
                return;

            string from = _installer.InstalledVersionOf(_page.ModuleFolderName);
            string to = _installer.ShippedVersionOf(_page.ModuleFolderName);

            ModuleUpdatePlanEVO plan = new ModuleUpdatePlan().Build(installedAt, shipped);
            var summary = new ModuleUpdateSummary();
            string question = summary.Ask(_page.Title, from, to, plan);
            ConflictPolicy policy;

            if (plan.HasConflicts)
            {
                int choice = EditorUtility.DisplayDialogComplex("Update " + _page.Title, question,
                    "Keep mine on conflicts", "Cancel", "Take the package's on conflicts");

                if (choice == 1)
                    return;

                policy = choice == 0 ? ConflictPolicy.KeepMine : ConflictPolicy.TakeTheirs;
            }
            else
            {
                if (!EditorUtility.DisplayDialog("Update " + _page.Title, question, "Update", "Cancel"))
                    return;

                policy = ConflictPolicy.KeepMine;
            }

            if (new ModuleUpdater().TryApply(plan, installedAt, shipped, policy, out string error))
            {
                _registrar.Register(_installer.ProjectRelative(installedAt), "updated");
                EditorUtility.DisplayDialog(_page.Title + " updated",
                    summary.Done(_page.Title, to, plan, policy == ConflictPolicy.KeepMine), "OK");

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