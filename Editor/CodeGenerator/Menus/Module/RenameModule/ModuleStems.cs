#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// The two stems a module name has, and the prefix rule that carries a rename from a module to
    /// what was named after it.
    ///
    /// The full stem is the folder name without "Module" - Counter, CounterTest, GameplayScreen -
    /// and is what a class, an asset or a nested module has to start with to follow the module.
    /// The typed stem is the folder name without its kind suffix - Counter for CounterTestModule -
    /// and is what the panel's field asks for, the way Create Module asks for a name without the
    /// suffix it appends.
    /// </summary>
    internal class ModuleStems
    {
        private const string MODULE = "Module";
        private const string TEST = "TestModule";
        private const string SCREEN = "ScreenModule";

        private static readonly Regex Identifier = new Regex("^[A-Za-z_][A-Za-z0-9_]*$");

        internal string FullStem(string moduleName) => WithoutSuffix(moduleName, MODULE);

        internal string KindSuffix(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Test: return TEST;
                case ModuleKind.Screen: return SCREEN;
                default: return MODULE;
            }
        }

        /// <summary>
        /// A test module named without Test in it - which Create Module never writes, but a folder
        /// made by hand may be - keeps its full stem as its typed one rather than losing a suffix it
        /// does not carry.
        /// </summary>
        internal string TypedStem(string moduleName, ModuleKind kind)
        {
            string suffix = KindSuffix(kind);

            return EndsWithSuffix(moduleName, suffix) ? WithoutSuffix(moduleName, suffix) : FullStem(moduleName);
        }

        internal string NameFor(string typedStem, ModuleKind kind) => typedStem + KindSuffix(kind);

        /// <summary>Whether the whole stem stands at the front of the name.</summary>
        internal bool Carries(string name, string stem) =>
            !string.IsNullOrEmpty(name)
            && !string.IsNullOrEmpty(stem)
            && name.StartsWith(stem, StringComparison.Ordinal);

        internal string Carried(string name, string oldStem, string newStem) => newStem + name.Substring(oldStem.Length);

        /// <summary>Why the typed stem cannot be used, or null when it can.</summary>
        internal string WhyInvalid(string typedStem)
        {
            if (string.IsNullOrWhiteSpace(typedStem)) return "Type the new name.";

            if (!Identifier.IsMatch(typedStem))
                return "A name is letters, digits and underscores, and does not start with a digit.";

            if (typedStem.EndsWith(MODULE, StringComparison.Ordinal))
                return "Leave the Module suffix off - the panel adds it.";

            return null;
        }

        private static bool EndsWithSuffix(string name, string suffix) =>
            !string.IsNullOrEmpty(name)
            && name.Length > suffix.Length
            && name.EndsWith(suffix, StringComparison.Ordinal);

        private static string WithoutSuffix(string name, string suffix) =>
            EndsWithSuffix(name, suffix) ? name.Substring(0, name.Length - suffix.Length) : name;
    }
}
#endif
