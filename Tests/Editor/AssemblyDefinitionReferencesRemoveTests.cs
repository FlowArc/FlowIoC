using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Taking a reference back out is what a module being deleted needs of every asmdef that named
    /// it. The file has to stay valid JSON afterwards and keep everything else it carried, because
    /// an asmdef holds references somebody added by hand.
    /// </summary>
    public class AssemblyDefinitionReferencesRemoveTests
    {
        private const string THREE =
            "{\n"
            + "    \"name\": \"Modules.Bar\",\n"
            + "    \"references\": [\n"
            + "        \"Modules.Foo.Shared\",\n"
            + "        \"Modules.Baz\",\n"
            + "        \"Modules.Qux\"\n"
            + "    ],\n"
            + "    \"autoReferenced\": true\n"
            + "}";

        private const string ONE =
            "{\n"
            + "    \"references\": [\n"
            + "        \"Modules.Foo.Shared\"\n"
            + "    ]\n"
            + "}";

        [Test]
        public void The_first_entry_leaves_no_blank_line_behind()
        {
            string result = new AssemblyDefinitionReferences().Remove(THREE, "Modules.Foo.Shared", out bool removed);

            Assert.IsTrue(removed);
            StringAssert.DoesNotContain("Modules.Foo.Shared", result);
            StringAssert.Contains("\"references\": [\n        \"Modules.Baz\",", result);
        }

        [Test]
        public void A_middle_entry_leaves_the_ones_around_it_joined()
        {
            string result = new AssemblyDefinitionReferences().Remove(THREE, "Modules.Baz", out bool removed);

            Assert.IsTrue(removed);
            StringAssert.DoesNotContain("Modules.Baz", result);
            StringAssert.Contains("\"Modules.Foo.Shared\",", result);
            StringAssert.Contains("\"Modules.Qux\"", result);
        }

        /// <summary>
        /// The comma that joined the last entry to the one before it goes with it, or the file is
        /// left with a trailing comma and Unity will not read it.
        /// </summary>
        [Test]
        public void The_last_entry_takes_its_comma_with_it()
        {
            string result = new AssemblyDefinitionReferences().Remove(THREE, "Modules.Qux", out bool removed);

            Assert.IsTrue(removed);
            StringAssert.DoesNotContain("Modules.Qux", result);
            StringAssert.DoesNotContain(",\n    ]", result);
            StringAssert.Contains("\"Modules.Baz\"\n", result);
        }

        [Test]
        public void The_only_entry_leaves_an_empty_list()
        {
            string result = new AssemblyDefinitionReferences().Remove(ONE, "Modules.Foo.Shared", out bool removed);

            Assert.IsTrue(removed);
            StringAssert.Contains("\"references\": []", result);
        }

        [Test]
        public void Everything_the_file_carried_besides_the_entry_is_left_alone()
        {
            string result = new AssemblyDefinitionReferences().Remove(THREE, "Modules.Baz", out _);

            StringAssert.Contains("\"name\": \"Modules.Bar\"", result);
            StringAssert.Contains("\"autoReferenced\": true", result);
        }

        [Test]
        public void A_reference_that_is_not_there_changes_nothing()
        {
            string result = new AssemblyDefinitionReferences().Remove(THREE, "Modules.Nowhere", out bool removed);

            Assert.IsFalse(removed);
            Assert.AreEqual(THREE, result);
        }

        [Test]
        public void A_file_with_no_reference_list_is_left_as_it_is()
        {
            const string none = "{\n    \"name\": \"Modules.Bar\"\n}";

            string result = new AssemblyDefinitionReferences().Remove(none, "Modules.Baz", out bool removed);

            Assert.IsFalse(removed);
            Assert.AreEqual(none, result);
        }

        /// <summary>
        /// Add and Remove are each other's undo, so a file that has been through both should be
        /// the file it started as.
        /// </summary>
        [Test]
        public void What_Add_wrote_Remove_takes_back_out()
        {
            var references = new AssemblyDefinitionReferences();

            string added = references.Add(THREE, "Modules.New.Signals", out _);
            string result = references.Remove(added, "Modules.New.Signals", out bool removed);

            Assert.IsTrue(removed);
            Assert.AreEqual(THREE, result);
        }
    }
}
