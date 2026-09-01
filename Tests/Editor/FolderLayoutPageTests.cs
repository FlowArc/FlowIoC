using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Help;
using FlowIoC.Editor.Help.Pages;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FolderLayoutPageTests
    {
        private ED_MainModuleDirectoryStructure _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<ED_MainModuleDirectoryStructure>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        /// <summary>
        /// The page teaches the folder layout that Create Module actually writes. If the
        /// generator's folder list changes and this page does not, the window starts teaching a
        /// layout that no longer exists - which is worse than teaching nothing.
        /// </summary>
        [Test]
        public void Every_folder_the_page_shows_is_a_folder_the_generator_writes()
        {
            HashSet<string> generated = new HashSet<string>(Flatten(_config.RootFolders));
            FolderLayoutPage page = new FolderLayoutPage();

            List<string> shown = page.Root.Descendants()
                .Select(node => node.Name)
                .Where(name => name != page.Root.Name && !name.EndsWith(".asmdef"))
                .ToList();

            foreach (string name in shown)
            {
                Assert.IsTrue(generated.Contains(name),
                    $"The help window shows a '{name}' folder that Create Module does not write.");
            }
        }

        [Test]
        public void The_page_shows_the_folders_the_rules_call_out_by_name()
        {
            FolderLayoutPage page = new FolderLayoutPage();
            List<string> shown = page.Root.Descendants().Select(node => node.Name).ToList();

            foreach (string required in new[]
                     {
                         "Controllers", "Models", "Signals", "RootsContexts",
                         "ViewsMediators", "Services", "Systems", "Functions"
                     })
            {
                Assert.Contains(required, shown);
            }
        }

        [Test]
        public void Every_leaf_the_page_shows_says_what_belongs_in_it()
        {
            FolderLayoutPage page = new FolderLayoutPage();

            foreach (HelpTreeNode node in page.Root.Descendants())
            {
                if (node.Children.Count == 0)
                    Assert.IsFalse(string.IsNullOrWhiteSpace(node.Comment), $"'{node.Name}' has no comment.");
            }
        }

        private IEnumerable<string> Flatten(IEnumerable<FolderEVO> folders)
        {
            foreach (FolderEVO folder in folders)
            {
                yield return folder.FolderName;

                if (folder.SubFolders == null)
                    continue;

                foreach (string name in Flatten(folder.SubFolders))
                    yield return name;
            }
        }
    }
}
