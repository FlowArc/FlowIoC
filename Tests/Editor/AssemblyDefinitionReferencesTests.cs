using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AssemblyDefinitionReferencesTests
    {
        private AssemblyDefinitionReferences _references;

        [SetUp]
        public void SetUp() => _references = new AssemblyDefinitionReferences();

        [Test]
        public void A_reference_is_added_after_the_ones_already_there()
        {
            string asmdef = "{\n  \"name\": \"Modules.Player\",\n  \"references\": [\n    \"FlowIoC\"\n  ],\n  \"autoReferenced\": true\n}";

            string result = _references.Add(asmdef, "Modules.Player.Shared", out bool added);

            Assert.IsTrue(added);
            StringAssert.Contains("\"FlowIoC\",\n    \"Modules.Player.Shared\"", result);
        }

        /// <summary>
        /// The asmdef may carry references someone added by hand - a Service module, a Unity
        /// package - and this is why the file is edited rather than rewritten from the template.
        /// </summary>
        [Test]
        public void Everything_else_in_the_file_survives()
        {
            string asmdef = "{\n  \"name\": \"Modules.Player\",\n  \"references\": [\n    \"FlowIoC\",\n    \"Modules.Countdown\"\n  ],\n"
                            + "  \"allowUnsafeCode\": true,\n  \"defineConstraints\": [\n    \"UNITY_EDITOR\"\n  ]\n}";

            string result = _references.Add(asmdef, "Modules.Player.Shared", out _);

            StringAssert.Contains("\"Modules.Countdown\"", result);
            StringAssert.Contains("\"allowUnsafeCode\": true", result);
            StringAssert.Contains("\"UNITY_EDITOR\"", result);
            StringAssert.Contains("\"name\": \"Modules.Player\"", result);
        }

        [Test]
        public void A_reference_already_listed_is_left_alone()
        {
            string asmdef = "{\n  \"references\": [\n    \"FlowIoC\",\n    \"Modules.Player.Shared\"\n  ]\n}";

            string result = _references.Add(asmdef, "Modules.Player.Shared", out bool added);

            Assert.IsFalse(added);
            Assert.AreEqual(asmdef, result);
        }

        [Test]
        public void An_empty_reference_list_gets_its_first_entry()
        {
            string asmdef = "{\n  \"name\": \"Modules.Player\",\n  \"references\": [],\n  \"autoReferenced\": true\n}";

            string result = _references.Add(asmdef, "Modules.Player.Shared", out bool added);

            Assert.IsTrue(added);
            StringAssert.Contains("\"Modules.Player.Shared\"", result);
            StringAssert.Contains("\"autoReferenced\": true", result);
        }

        /// <summary>
        /// Unity writes asmdefs with four spaces and a hand-edited one may use tabs, so the new
        /// entry copies the indent of the one above it instead of imposing its own.
        /// </summary>
        [Test]
        public void The_new_entry_copies_the_indent_of_the_last_one()
        {
            string asmdef = "{\n\t\"references\": [\n\t\t\"FlowIoC\"\n\t]\n}";

            string result = _references.Add(asmdef, "Modules.Player.Shared", out _);

            StringAssert.Contains("\n\t\t\"Modules.Player.Shared\"", result);
        }

        [Test]
        public void A_file_with_no_reference_list_is_left_alone()
        {
            string asmdef = "{\n  \"name\": \"Modules.Player\"\n}";

            string result = _references.Add(asmdef, "Modules.Player.Shared", out bool added);

            Assert.IsFalse(added);
            Assert.AreEqual(asmdef, result);
        }

        [Test]
        public void Nothing_is_added_for_a_reference_that_was_never_found()
        {
            string asmdef = "{\n  \"references\": [\n    \"FlowIoC\"\n  ]\n}";

            Assert.AreEqual(asmdef, _references.Add(asmdef, null, out bool added));
            Assert.IsFalse(added);

            Assert.AreEqual(asmdef, _references.Add(asmdef, "", out added));
            Assert.IsFalse(added);
        }

        /// <summary>
        /// The result has to stay valid JSON, which the string assertions above cannot see on
        /// their own - a stray or missing comma would pass every one of them.
        /// </summary>
        [Test]
        public void The_result_is_still_parseable_as_an_assembly_definition()
        {
            string asmdef = "{\n  \"name\": \"Modules.Player\",\n  \"references\": [\n    \"FlowIoC\"\n  ],\n  \"autoReferenced\": true\n}";

            string result = _references.Add(asmdef, "Modules.Player.Shared", out _);
            var parsed = UnityEngine.JsonUtility.FromJson<AssemblyDefinitionShape>(result);

            Assert.AreEqual("Modules.Player", parsed.name);
            CollectionAssert.AreEqual(new[] {"FlowIoC", "Modules.Player.Shared"}, parsed.references);
            Assert.IsTrue(parsed.autoReferenced);
        }

        [System.Serializable]
        private class AssemblyDefinitionShape
        {
            public string name;
            public string[] references;
            public bool autoReferenced;
        }
    }
}
