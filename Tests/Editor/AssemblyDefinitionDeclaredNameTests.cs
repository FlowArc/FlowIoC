using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>The asmdef's own "name" field, renamed in place; the references block is not it.</summary>
    public class AssemblyDefinitionDeclaredNameTests
    {
        private const string ASMDEF =
            "{\n    \"name\": \"Modules.Counter\",\n    \"references\": [\n        \"Modules.Counter.Signals\"\n    ]\n}";

        [Test]
        public void The_declared_name_is_renamed_and_the_references_are_not()
        {
            string result = new AssemblyDefinitionDeclaredName().Rename(ASMDEF, "Modules.Counter", "Modules.Timer", out bool renamed);

            Assert.IsTrue(renamed);
            StringAssert.Contains("\"name\": \"Modules.Timer\"", result);
            StringAssert.Contains("\"Modules.Counter.Signals\"", result);
        }

        [Test]
        public void Any_spacing_around_the_colon_is_kept()
        {
            string result = new AssemblyDefinitionDeclaredName().Rename("{\"name\":\"Modules.Counter\"}", "Modules.Counter", "Modules.Timer", out bool renamed);

            Assert.IsTrue(renamed);
            Assert.AreEqual("{\"name\":\"Modules.Timer\"}", result);
        }

        [Test]
        public void A_different_declared_name_leaves_the_file_as_it_was()
        {
            string result = new AssemblyDefinitionDeclaredName().Rename(ASMDEF, "Modules.Other", "Modules.Timer", out bool renamed);

            Assert.IsFalse(renamed);
            Assert.AreEqual(ASMDEF, result);
        }
    }
}
