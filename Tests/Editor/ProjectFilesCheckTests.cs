using System.Collections.Generic;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.ProjectFiles;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A project file that points at a package folder the Package Manager has swept, or a
    /// solution that lists a project file that is gone, is what the IDE reads while Unity
    /// compiles from neither. The check names the file and its Fix regenerates them all.
    /// </summary>
    public class ProjectFilesCheckTests
    {
        private const string ROOT = "C:/proj";

        private class RecordingRegenerator : IProjectFilesRegenerator
        {
            public int Regenerated;
            public string EditorName => "RecordingEditor";
            public void Regenerate() => Regenerated++;
        }

        private readonly Dictionary<string, string> _files = new();
        private readonly HashSet<string> _folders = new();
        private RecordingRegenerator _regenerator;

        [SetUp]
        public void SetUp()
        {
            _files.Clear();
            _folders.Clear();
            _regenerator = new RecordingRegenerator();
        }

        private ProjectFilesCheck Check() => new ProjectFilesCheck(
            (root, pattern) =>
            {
                var matches = new List<string>();
                string suffix = pattern.TrimStart('*');
                foreach (string path in _files.Keys)
                    if (path.EndsWith(suffix)) matches.Add(path);
                return matches.ToArray();
            },
            path => _files[path],
            path => _folders.Contains(path.Replace('\\', '/')),
            path => _files.ContainsKey(path.Replace('\\', '/')),
            _regenerator);

        private static ProjectTargetEVO Project() => new ProjectTargetEVO {ProjectRoot = ROOT};

        private static string ProjectFileOn(string folder) =>
            "<Project>\n  <Compile Include=\"Library\\PackageCache\\" + folder + "\\Runtime\\A.cs\" />\n"
            + "  <Compile Include=\"Library\\PackageCache\\" + folder + "\\Runtime\\B.cs\" />\n</Project>";

        private static string SolutionListing(params string[] projects)
        {
            var text = "Microsoft Visual Studio Solution File\n";
            foreach (string project in projects)
                text += "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"" + project.Replace(".csproj", "")
                        + "\", \"" + project + "\", \"{655af913-a3f3-e822-fee6-c299f5a0a68e}\"\nEndProject\n";
            return text;
        }

        [Test]
        public void A_project_file_on_a_package_folder_that_exists_is_Ok()
        {
            _folders.Add(ROOT + "/Library/PackageCache/com.flowarc.flowioc.core@0253efc414f7");
            _files[ROOT + "/FlowIoC.csproj"] = ProjectFileOn("com.flowarc.flowioc.core@0253efc414f7");
            _files[ROOT + "/Game.sln"] = SolutionListing("FlowIoC.csproj");

            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Project()).Status);
        }

        [Test]
        public void A_project_file_on_a_package_folder_that_is_gone_is_Fixable_and_named()
        {
            _folders.Add(ROOT + "/Library/PackageCache/com.flowarc.flowioc.core@0253efc414f7");
            _files[ROOT + "/FlowIoC.csproj"] = ProjectFileOn("com.flowarc.flowioc.core@0a64e1922cc6");
            _files[ROOT + "/Game.sln"] = SolutionListing("FlowIoC.csproj");

            FindingEVO finding = Check().Inspect(Project());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("FlowIoC.csproj", finding.Message);
            StringAssert.DoesNotContain("Game.sln", finding.Message);
        }

        [Test]
        public void A_solution_listing_a_project_file_that_is_gone_is_Fixable_and_named()
        {
            _files[ROOT + "/Game.sln"] = SolutionListing("Modules.Player.csproj", "Modules.Deleted.csproj");
            _files[ROOT + "/Modules.Player.csproj"] = "<Project></Project>";

            FindingEVO finding = Check().Inspect(Project());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Game.sln", finding.Message);
        }

        [Test]
        public void A_project_file_with_no_package_source_is_Ok()
        {
            _files[ROOT + "/Modules.Player.csproj"] = "<Project>\n  <Compile Include=\"Assets\\Modules\\PlayerModule\\A.cs\" />\n</Project>";

            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Project()).Status);
        }

        [Test]
        public void No_project_files_at_all_is_Ok()
        {
            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Project()).Status);
        }

        [Test]
        public void Fix_regenerates_the_project_files_once()
        {
            _files[ROOT + "/FlowIoC.csproj"] = ProjectFileOn("com.flowarc.flowioc.core@0a64e1922cc6");

            Check().Fix(Project());

            Assert.AreEqual(1, _regenerator.Regenerated);
        }
    }
}
