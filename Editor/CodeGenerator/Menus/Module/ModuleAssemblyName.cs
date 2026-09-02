#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The assembly name a module folder declares: "PlayerModule" is "Modules.Player", and a
    /// screen or test module named after the module it belongs to says so in its own name -
    /// "MatchBoardScreenModule" is "Modules.MatchBoard.Screen".
    ///
    /// The rules used to be hand-rolled in four places, and each copy read the suffix off the end
    /// without asking what was left in front of it. A module called exactly "ScreenModule" matched
    /// the screen rule as a whole string, so the parent it named was empty and the result came out
    /// as "Modules." + "" + ".Screen" - the double dot that ScreenModule shipped with. A rule whose
    /// parent turns out to be empty is the wrong rule, so it gives way to the next one down.
    ///
    /// There is no Unity API in here, which is what makes it worth having on its own rather than
    /// only inside the tool that calls it.
    /// </summary>
    internal class ModuleAssemblyName
    {
        private const string ROOT = "Modules.";

        /// <summary>
        /// Recognised in order, longest suffix first: "MatchBoardScreenTestModule" has to be read
        /// as a screen's test module rather than as a test module of "MatchBoardScreen".
        /// </summary>
        private static readonly (string Suffix, string Tail)[] Roles =
        {
            ("ScreenTestModule", ".Screen.Test"),
            ("ScreenModule", ".Screen"),
            ("TestModule", ".Test")
        };

        /// <summary>
        /// <paramref name="moduleFolderName"/> is the folder as it sits on disk. A folder that
        /// names no module at all is worth no assembly name, and says so with an empty string
        /// rather than a bare "Modules.".
        /// </summary>
        public string From(string moduleFolderName)
        {
            string name = moduleFolderName?.Trim();

            if (string.IsNullOrEmpty(name))
                return string.Empty;

            foreach ((string suffix, string tail) in Roles)
            {
                string parent = ParentBefore(name, suffix);

                if (parent.Length > 0)
                    return ROOT + parent + tail;
            }

            string core = ParentBefore(name, "Module");

            return core.Length > 0 ? ROOT + core : ROOT + name;
        }

        /// <summary>
        /// The same answer for a module named without its "Module" suffix - "MatchBoardScreen" or
        /// "MatchBoardScreenTest", which is the shape the generator carries in EditorPrefs and
        /// hands to the assembly lookup after a reload. The folder such a name stands for is that
        /// name plus "Module", so that is what the rules above are asked about: reading the
        /// suffixes off the shorter form instead is what produced "Modules.MatchBoardScreen.Test"
        /// for a module whose assembly is "Modules.MatchBoard.Screen.Test".
        /// </summary>
        public string FromModuleName(string moduleName)
        {
            string name = moduleName?.Trim();

            if (string.IsNullOrEmpty(name))
                return string.Empty;

            return From(name.EndsWith("Module", StringComparison.OrdinalIgnoreCase) ? name : name + "Module");
        }

        /// <summary>
        /// What stands in front of <paramref name="suffix"/>, or nothing when the name does not
        /// carry that suffix or is made of nothing else.
        /// </summary>
        private static string ParentBefore(string name, string suffix)
        {
            if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return name.Substring(0, name.Length - suffix.Length).Trim();
        }
    }
}
#endif