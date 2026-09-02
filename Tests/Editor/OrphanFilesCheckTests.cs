using System.Collections.Generic;
using FlowIoC.Editor.ModuleScan;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class OrphanFilesCheckTests
    {
        private const string ROOT = "C:/proj";

        private static ProjectTargetEVO Project() => new ProjectTargetEVO
        {
            ProjectRoot = ROOT,
            AllAssemblyNames = new[] {"Modules.Player", "Modules.Player.Shared", "FlowIoC.Editor"}
        };

        private static OrphanFilesCheck Check(List<string> deleted, params string[] rootFiles)
        {
            return new OrphanFilesCheck(
                (root, pattern) =>
                {
                    var matches = new List<string>();

                    foreach (string file in rootFiles)
                    {
                        bool isSettings = file.EndsWith(".csproj.DotSettings");

                        if (pattern.EndsWith(".csproj.DotSettings") && isSettings) matches.Add(file);
                        if (pattern.EndsWith(".csproj") && !isSettings && file.EndsWith(".csproj")) matches.Add(file);
                    }

                    return matches.ToArray();
                },
                path => deleted.Add(path));
        }

        [Test]
        public void Settings_files_backed_by_a_real_assembly_are_Ok()
        {
            var check = Check(new List<string>(),
                ROOT + "/Modules.Player.csproj.DotSettings",
                ROOT + "/Modules.Player.Shared.csproj.DotSettings",
                ROOT + "/FlowIoC.Editor.csproj.DotSettings");

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Project()).Status);
        }

        [Test]
        public void A_settings_file_with_no_assembly_behind_it_is_Fixable_and_named()
        {
            var check = Check(new List<string>(),
                ROOT + "/Modules.Player.csproj.DotSettings",
                ROOT + "/Modules.Deleted.csproj.DotSettings");

            FindingEVO finding = check.Inspect(Project());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Modules.Deleted.csproj.DotSettings", finding.Message);
        }

        /// <summary>
        /// Rider regenerates a .csproj per assembly. One left behind for an assembly that is gone
        /// keeps showing up in the solution, so it is swept with its settings file.
        /// </summary>
        [Test]
        public void An_orphaned_generated_csproj_counts_too()
        {
            var check = Check(new List<string>(), ROOT + "/Modules.Deleted.csproj");

            Assert.AreEqual(ModuleCheckStatus.Fixable, check.Inspect(Project()).Status);
        }

        [Test]
        public void Fix_deletes_only_the_orphans()
        {
            var deleted = new List<string>();
            var check = Check(deleted,
                ROOT + "/Modules.Player.csproj.DotSettings",
                ROOT + "/Modules.Deleted.csproj.DotSettings");

            check.Fix(Project());

            CollectionAssert.AreEqual(new[] {ROOT + "/Modules.Deleted.csproj.DotSettings"}, deleted);
        }

        /// <summary>
        /// A project whose assemblies could not be listed is not a project with no assemblies.
        /// Sweeping on the strength of an empty list would delete every settings file there is.
        /// </summary>
        [Test]
        public void An_empty_assembly_list_deletes_nothing()
        {
            var deleted = new List<string>();
            var check = Check(deleted, ROOT + "/Modules.Player.csproj.DotSettings");

            var project = new ProjectTargetEVO {ProjectRoot = ROOT, AllAssemblyNames = new string[0]};

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(project).Status);

            check.Fix(project);

            CollectionAssert.IsEmpty(deleted);
        }
    }
}
