using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Which assemblies a module folder declares, which is what Delete Module unwires from other
    /// asmdefs and whose project files it takes off the solution root.
    ///
    /// This used to be worked out from the module's name - the module's own assembly, plus Shared
    /// and Signals - and a module that holds sub-modules declares more than three. A screen module
    /// brings a test module with it, so deleting one left Modules.MatchBoard.Screen.Test named in
    /// asmdefs that referenced it and its two project files at the root, pointing at an assembly
    /// that had gone.
    /// </summary>
    public class ModuleAssembliesTests
    {
        private const string MODULE = "C:/Project/Assets/Modules/MatchBoardModule";

        private static ModuleAssemblies Reading(Dictionary<string, string> asmdefs) =>
            new ModuleAssemblies(
                path => Paths(asmdefs, path),
                asmdef => asmdefs.TryGetValue(asmdef, out string json) ? json : throw new FileNotFoundException(asmdef));

        private static IEnumerable<string> Paths(Dictionary<string, string> asmdefs, string under)
        {
            foreach (KeyValuePair<string, string> pair in asmdefs)
            {
                if (pair.Key.StartsWith(under, StringComparison.OrdinalIgnoreCase)) yield return pair.Key;
            }
        }

        private static string Declaring(string assemblyName) => "{\"name\": \"" + assemblyName + "\"}";

        [Test]
        public void The_assemblies_the_asmdefs_declare_are_answered()
        {
            IReadOnlyList<string> assemblies = Reading(new Dictionary<string, string>
            {
                {MODULE + "/Modules.MatchBoard.asmdef", Declaring("Modules.MatchBoard")},
                {MODULE + "/Scripts/Signals/Modules.MatchBoard.Signals.asmdef", Declaring("Modules.MatchBoard.Signals")}
            }).Of(MODULE, "MatchBoardModule");

            CollectionAssert.Contains(assemblies, "Modules.MatchBoard");
            CollectionAssert.Contains(assemblies, "Modules.MatchBoard.Signals");
        }

        /// <summary>
        /// The regression this exists for. A screen module's test module is inside its folder and
        /// goes with it, so its assembly has to go with it too.
        /// </summary>
        [Test]
        public void A_sub_modules_assembly_is_among_them()
        {
            IReadOnlyList<string> assemblies = Reading(new Dictionary<string, string>
            {
                {MODULE + "/Modules.MatchBoard.Screen.asmdef", Declaring("Modules.MatchBoard.Screen")},
                {
                    MODULE + "/zTestModules/MatchBoardScreenTestModule/Modules.MatchBoard.Screen.Test.asmdef",
                    Declaring("Modules.MatchBoard.Screen.Test")
                }
            }).Of(MODULE, "MatchBoardScreenModule");

            CollectionAssert.Contains(assemblies, "Modules.MatchBoard.Screen.Test");
        }

        /// <summary>
        /// The three the name implies are kept alongside what was found, so a module whose folder
        /// has already been half removed still has its own assemblies unwired. Naming one that never
        /// existed costs nothing: references are only removed where they are, and project files only
        /// deleted where they exist.
        /// </summary>
        [Test]
        public void The_three_the_name_implies_are_there_even_when_no_asmdef_is()
        {
            IReadOnlyList<string> assemblies =
                Reading(new Dictionary<string, string>()).Of(MODULE, "MatchBoardModule");

            CollectionAssert.AreEquivalent(
                new[] {"Modules.MatchBoard", "Modules.MatchBoard.Shared", "Modules.MatchBoard.Signals"},
                assemblies);
        }

        [Test]
        public void An_assembly_both_declared_and_derived_is_answered_once()
        {
            IReadOnlyList<string> assemblies = Reading(new Dictionary<string, string>
            {
                {MODULE + "/Modules.MatchBoard.asmdef", Declaring("Modules.MatchBoard")}
            }).Of(MODULE, "MatchBoardModule");

            Assert.AreEqual(1, Occurrences(assemblies, "Modules.MatchBoard"));
        }

        /// <summary>
        /// This runs on the way to a deletion, so one asmdef nobody can read is not a reason to
        /// leave the rest of the module wired into the project.
        /// </summary>
        [Test]
        public void An_asmdef_that_cannot_be_read_is_skipped_rather_than_thrown()
        {
            IReadOnlyList<string> assemblies = new ModuleAssemblies(
                    _ => new[] {MODULE + "/Broken.asmdef", MODULE + "/Modules.MatchBoard.asmdef"},
                    asmdef => asmdef.EndsWith("Broken.asmdef", StringComparison.Ordinal)
                        ? throw new IOException("locked")
                        : Declaring("Modules.MatchBoard"))
                .Of(MODULE, "MatchBoardModule");

            CollectionAssert.Contains(assemblies, "Modules.MatchBoard");
        }

        [Test]
        public void An_asmdef_that_declares_no_name_is_worth_no_assembly()
        {
            IReadOnlyList<string> assemblies = Reading(new Dictionary<string, string>
            {
                {MODULE + "/Nameless.asmdef", "{}"}
            }).Of(MODULE, "MatchBoardModule");

            CollectionAssert.DoesNotContain(assemblies, string.Empty);
            CollectionAssert.DoesNotContain(assemblies, null);
        }

        [Test]
        public void A_folder_that_names_no_module_and_holds_no_asmdef_is_worth_nothing()
        {
            Assert.IsEmpty(Reading(new Dictionary<string, string>()).Of(null, null));
        }

        private static int Occurrences(IEnumerable<string> assemblies, string name)
        {
            var count = 0;

            foreach (string assembly in assemblies)
            {
                if (assembly == name) count++;
            }

            return count;
        }
    }
}
