using System.Collections.Generic;
using FlowIoC.Editor.ModuleCards;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleDirectoryBuilderTests
    {
        private static ModuleCardEntryEVO Entry(
            string name, ModuleKind kind, string group, string path, int depth, string purpose, string concepts)
        {
            return new ModuleCardEntryEVO
            {
                Name = name,
                Kind = kind,
                Group = group,
                RelativePath = path,
                Depth = depth,
                Purpose = purpose,
                Concepts = concepts,
            };
        }

        private static List<ModuleCardEntryEVO> Entries()
        {
            return new List<ModuleCardEntryEVO>
            {
                Entry("PlayerModule", ModuleKind.Main, "Assets/Modules", "Assets/Modules/PlayerModule", 0,
                    "Owns the money.", "currency, wallet"),
                Entry("PlayerScreenModule", ModuleKind.Screen, "Assets/Modules",
                    "Assets/Modules/PlayerModule/zScreenModules/PlayerScreenModule", 1,
                    "Shows the wallet.", "wallet screen"),
                Entry("ScreenModule", ModuleKind.Main, "Packages/FlowIoC/Runtime",
                    "Packages/FlowIoC/Runtime/ScreenModule", 0, "Opens and pools screens.", "screen, popup"),
            };
        }

        [Test]
        public void The_warning_line_comes_first()
        {
            StringAssert.StartsWith(
                "This file is generated and gitignored. Do not commit it.",
                new ModuleDirectoryBuilder().Build(Entries()));
        }

        [Test]
        public void Each_group_gets_a_heading()
        {
            string body = new ModuleDirectoryBuilder().Build(Entries());

            StringAssert.Contains("### Assets/Modules", body);
            StringAssert.Contains("### Packages/FlowIoC/Runtime", body);
        }

        [Test]
        public void A_module_shows_its_name_kind_and_purpose()
        {
            StringAssert.Contains(
                "- **PlayerModule** (Main) — Owns the money.",
                new ModuleDirectoryBuilder().Build(Entries()));
        }

        [Test]
        public void A_module_shows_its_path_and_concepts_on_the_next_line()
        {
            StringAssert.Contains(
                "  `Assets/Modules/PlayerModule` · currency, wallet",
                new ModuleDirectoryBuilder().Build(Entries()));
        }

        [Test]
        public void A_nested_module_is_indented_under_its_parent()
        {
            StringAssert.Contains(
                "  - **PlayerScreenModule** (Screen) — Shows the wallet.",
                new ModuleDirectoryBuilder().Build(Entries()));
        }

        [Test]
        public void A_module_with_no_purpose_still_appears_and_says_so()
        {
            var entries = new List<ModuleCardEntryEVO>
            {
                Entry("MapModule", ModuleKind.Main, "Assets/Modules", "Assets/Modules/MapModule", 0, null, null),
            };

            string body = new ModuleDirectoryBuilder().Build(entries);

            StringAssert.Contains("**MapModule** (Main)", body);
            StringAssert.Contains("no purpose written yet", body);
        }

        [Test]
        public void The_same_entries_always_render_the_same_text()
        {
            Assert.AreEqual(
                new ModuleDirectoryBuilder().Build(Entries()),
                new ModuleDirectoryBuilder().Build(Entries()));
        }
    }
}
