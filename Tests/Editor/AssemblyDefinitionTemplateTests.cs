using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AssemblyDefinitionTemplateTests
    {
        private AssemblyDefinitionTemplate _template;

        [SetUp]
        public void SetUp() => _template = new AssemblyDefinitionTemplate();

        [Test]
        public void The_assembly_name_lands_in_the_name_field()
        {
            string asmdef = _template.Build("Modules.Player", null);

            StringAssert.Contains("\"name\": \"Modules.Player\"", asmdef);
        }

        [Test]
        public void FlowIoC_is_referenced_even_when_nothing_else_is()
        {
            string asmdef = _template.Build("Modules.Player", null);

            StringAssert.Contains("\"FlowIoC\"", asmdef);
        }

        [Test]
        public void Every_reference_given_is_listed()
        {
            string asmdef = _template.Build("Modules.Player.Screen", new List<string> {"Modules.Player.Shared"});

            StringAssert.Contains("\"FlowIoC\"", asmdef);
            StringAssert.Contains("\"Modules.Player.Shared\"", asmdef);
        }

        /// <summary>
        /// Callers pass the result of a lookup that legitimately finds nothing - a module with no
        /// Shared folder, a parent module that publishes none - so a null has to be dropped here
        /// rather than guarded against at each call site.
        /// </summary>
        [Test]
        public void A_reference_that_was_never_found_is_dropped()
        {
            string asmdef = _template.Build("Modules.Player", new List<string> {null, "", "Modules.Player.Shared"});

            StringAssert.Contains("\"Modules.Player.Shared\"", asmdef);
            Assert.AreEqual(2, CountReferenceLines(asmdef), "Only FlowIoC and the Shared assembly should be listed.");
        }

        [Test]
        public void A_reference_is_never_listed_twice()
        {
            string asmdef = _template.Build("Modules.Player", new List<string> {"FlowIoC", "Modules.Player.Shared", "Modules.Player.Shared"});

            Assert.AreEqual(2, CountReferenceLines(asmdef));
        }

        private int CountReferenceLines(string asmdef)
        {
            const string opening = "\"references\": [";

            int start = asmdef.IndexOf(opening, System.StringComparison.Ordinal) + opening.Length;
            int end = asmdef.IndexOf("]", start, System.StringComparison.Ordinal);
            string block = asmdef.Substring(start, end - start);

            int count = 0;
            foreach (string line in block.Split('\n'))
            {
                if (line.Trim().StartsWith("\"")) count++;
            }

            return count;
        }
    }
}
