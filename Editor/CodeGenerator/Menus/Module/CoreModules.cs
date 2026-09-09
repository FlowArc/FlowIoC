#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The three modules a FlowIoC project is built on, and why each one stays.
    ///
    /// They arrive with the setup set and everything else in the project is written against them:
    /// the scene the game starts in, the one place modules are wired to each other, and the service
    /// every screen is opened through. Deleting one does not leave a smaller project, it leaves a
    /// project that does not run - and Delete Module is thorough enough that there is no half-way
    /// state to recover from afterwards.
    ///
    /// GameplayModule arrives with them and is not here. It is the worked example a game replaces
    /// with its own, which is exactly what deleting it is for.
    ///
    /// The match is by folder name, which is what a reader sees and what the setup set writes. A
    /// project that renames one of them loses the guard, and that is the right way round: renaming
    /// a module is a deliberate act, and the tool has no business insisting on a name the owner
    /// changed on purpose.
    /// </summary>
    internal class CoreModules
    {
        private static readonly Dictionary<string, string> Reasons = new Dictionary<string, string>
        {
            {
                "MainModule",
                "the scene the game starts in lives here"
            },
            {
                "ConnectorModule",
                "every crossing between modules is wired here"
            },
            {
                "ScreenModule",
                "every screen is opened through this service"
            }
        };

        /// <summary>
        /// Why this module cannot be deleted, or null when it can. A reader is owed the reason
        /// rather than a row whose button has silently gone.
        /// </summary>
        internal string WhyKept(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName)) return null;

            foreach (KeyValuePair<string, string> reason in Reasons)
            {
                if (string.Equals(reason.Key, moduleName, StringComparison.Ordinal)) return reason.Value;
            }

            return null;
        }
    }
}

#endif
