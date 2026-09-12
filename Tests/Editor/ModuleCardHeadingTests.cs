using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A card's first line is the module's name in one of the two shapes the project has written
    /// it in - the folder name, or the name without Module the way the stub writes it today - and
    /// the rename changes exactly that line, in whichever shape it finds, and nothing under it.
    /// </summary>
    public class ModuleCardHeadingTests
    {
        private readonly ModuleCardHeading _heading = new ModuleCardHeading();

        [Test]
        public void The_folder_name_shape_becomes_the_new_folder_name()
        {
            string result = _heading.Renamed("# WalkModule\n\n## Purpose\nWalks.\n", "WalkModule", "StrollModule", "Walk", "Stroll", out bool changed);

            Assert.IsTrue(changed);
            Assert.AreEqual("# StrollModule\n\n## Purpose\nWalks.\n", result);
        }

        [Test]
        public void The_stem_shape_becomes_the_new_stem_and_keeps_its_line_ending()
        {
            string result = _heading.Renamed("# Walk\r\n\r\n## Purpose\r\n", "WalkModule", "StrollModule", "Walk", "Stroll", out bool changed);

            Assert.IsTrue(changed);
            Assert.AreEqual("# Stroll\r\n\r\n## Purpose\r\n", result);
        }

        [Test]
        public void A_heading_that_only_starts_with_the_name_is_somebody_elses_and_stays()
        {
            string text = "# Walker\n\n## Purpose\n";

            string result = _heading.Renamed(text, "WalkModule", "StrollModule", "Walk", "Stroll", out bool changed);

            Assert.IsFalse(changed);
            Assert.AreEqual(text, result);
        }

        [Test]
        public void A_card_without_the_heading_on_its_first_line_stays()
        {
            string text = "Some preamble\n# WalkModule\n";

            string result = _heading.Renamed(text, "WalkModule", "StrollModule", "Walk", "Stroll", out bool changed);

            Assert.IsFalse(changed);
            Assert.AreEqual(text, result);
        }
    }
}
