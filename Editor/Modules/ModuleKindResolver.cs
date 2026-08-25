#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// A module's kind is the name of the folder it sits in, and nothing else. Three copies of
    /// this rule used to exist — in ModuleAutoDetector, in DeleteModuleMenu against hardcoded
    /// "z" prefixes, and in NamespaceProvider against a parsed string — and they disagreed.
    /// The container folder names are configurable, so they are passed in rather than assumed.
    /// </summary>
    internal class ModuleKindResolver
    {
        private readonly string _subModulesFolder;
        private readonly string _screenModulesFolder;
        private readonly string _testModulesFolder;

        public ModuleKindResolver(string subModulesFolder, string screenModulesFolder, string testModulesFolder)
        {
            _subModulesFolder = subModulesFolder;
            _screenModulesFolder = screenModulesFolder;
            _testModulesFolder = testModulesFolder;
        }

        public ModuleKind Resolve(string parentFolderName)
        {
            if (string.IsNullOrEmpty(parentFolderName)) return ModuleKind.Main;

            if (Matches(parentFolderName, _subModulesFolder)) return ModuleKind.Sub;
            if (Matches(parentFolderName, _screenModulesFolder)) return ModuleKind.Screen;
            if (Matches(parentFolderName, _testModulesFolder)) return ModuleKind.Test;

            return ModuleKind.Main;
        }

        private bool Matches(string parentFolderName, string configuredFolderName)
        {
            return !string.IsNullOrEmpty(configuredFolderName)
                   && string.Equals(parentFolderName, configuredFolderName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

#endif
