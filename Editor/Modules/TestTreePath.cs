#if UNITY_EDITOR
using System;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// Whether a path lies inside a test module - the test module itself, or anything nested in
    /// it, a screen made for it included. Where a thing sits is what makes it test code: deleting
    /// the test module deletes it, so nothing the game runs may depend on it. The scanner, Create
    /// Module, the Root inspector and Add Sub Context all ask here, so none of them keeps its own
    /// copy of the folder name.
    /// </summary>
    internal class TestTreePath
    {
        private const string DEFAULT_FOLDER_NAME = "zTestModules";

        private readonly string _folderName;

        internal TestTreePath() : this(ConfiguredFolderName())
        {
        }

        internal TestTreePath(string testModulesFolderName)
        {
            _folderName = string.IsNullOrEmpty(testModulesFolderName) ? DEFAULT_FOLDER_NAME : testModulesFolderName;
        }

        /// <summary>
        /// True when one whole segment of the path is the test modules folder. Either separator,
        /// any casing - Windows answers both - and never a substring, so a module whose name
        /// merely contains the folder's name is not taken for one.
        /// </summary>
        internal bool Contains(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            foreach (string segment in path.Replace('\\', '/').Split('/'))
            {
                if (string.Equals(segment, _folderName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string ConfiguredFolderName()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);

            return settings == null
                ? DEFAULT_FOLDER_NAME
                : settings.FolderNameFor(FolderEVO.FolderType.TestModules, DEFAULT_FOLDER_NAME);
        }
    }
}
#endif
