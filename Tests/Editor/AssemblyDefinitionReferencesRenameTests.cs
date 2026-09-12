using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Renaming a module renames its assemblies, and every asmdef that named one has to say the
    /// new name in the same place it said the old one - the entry alone, the rest of the file as
    /// it was, since an asmdef carries what somebody added by hand.
    /// </summary>
    public class AssemblyDefinitionReferencesRenameTests
    {
        private const string THREE =
            "{\n"
            + "    \"name\": \"Modules.Bar\",\n"
            + "    \"references\": [\n"
            + "        \"Modules.Counter\",\n"
            + "        \"Modules.Counter.Signals\",\n"
            + "        \"GUID:0123456789abcdef0123456789abcdef\"\n"
            + "    ],\n"
            + "    \"autoReferenced\": true\n"
            + "}";

        [Test]
        public void The_entry_is_renamed_in_place_and_nothing_else_moves()
        {
            string result = new AssemblyDefinitionReferences().Rename(THREE, "Modules.Counter", "Modules.Timer", out bool renamed);

            Assert.IsTrue(renamed);
            Assert.AreEqual(THREE.Replace("\"Modules.Counter\"", "\"Modules.Timer\""), result);
        }

        [Test]
        public void A_name_that_is_the_front_of_a_longer_entry_does_not_touch_it()
        {
            string result = new AssemblyDefinitionReferences().Rename(THREE, "Modules.Counter", "Modules.Timer", out _);

            StringAssert.Contains("\"Modules.Counter.Signals\"", result);
        }

        [Test]
        public void The_longer_entry_is_renamed_on_its_own()
        {
            string result = new AssemblyDefinitionReferences().Rename(THREE, "Modules.Counter.Signals", "Modules.Timer.Signals", out bool renamed);

            Assert.IsTrue(renamed);
            StringAssert.Contains("\"Modules.Timer.Signals\"", result);
            StringAssert.Contains("\"Modules.Counter\",", result);
        }

        [Test]
        public void An_unlisted_name_and_a_guid_reference_leave_the_file_as_it_was()
        {
            string result = new AssemblyDefinitionReferences().Rename(THREE, "Modules.Nope", "Modules.Yes", out bool renamed);

            Assert.IsFalse(renamed);
            Assert.AreEqual(THREE, result);
        }
    }
}
