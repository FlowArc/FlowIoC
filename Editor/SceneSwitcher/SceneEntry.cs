#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER

using System;

namespace FlowIoC.Editor.SceneSwitcher
{
    /// <summary>
    /// One scene the switcher offers: its asset path, its name, the module it is listed under and
    /// what it is there for. The module is whatever folder sits directly under Modules, so a
    /// screen module's test scene is listed with the module whose feature the screen shows.
    /// </summary>
    public readonly struct SceneEntry
    {
        private const string MODULES_FOLDER_NAME = "Modules";
        private const string TEST_MODULES_FOLDER = "zTestModules";
        private const string SCREEN_MODULES_FOLDER = "zScreenModules";
        private const string CHECK_MODULE_SUFFIX = "CheckModule";

        public readonly string Path;
        public readonly string Name;
        public readonly string Module;
        public readonly SceneKind Kind;

        /// <summary>The module's folder, Assets/Modules/AudioModule - where its Root is read from.</summary>
        public readonly string ModuleFolder;

        private SceneEntry(string path, string name, string module, string moduleFolder, SceneKind kind)
        {
            Path = path;
            Name = name;
            Module = module;
            ModuleFolder = moduleFolder;
            Kind = kind;
        }

        public static SceneEntry From(string scenePath)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            string folderPath = System.IO.Path.GetDirectoryName(scenePath)?.Replace("\\", "/") ?? string.Empty;
            string[] folders = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            string module = string.Empty;
            string moduleFolder = string.Empty;
            bool inTestModule = false;
            bool inScreenModule = false;
            bool inCheckModule = false;

            int modulesIndex = Array.IndexOf(folders, MODULES_FOLDER_NAME);

            if (modulesIndex >= 0 && modulesIndex + 1 < folders.Length)
            {
                module = folders[modulesIndex + 1];
                moduleFolder = string.Join("/", folders, 0, modulesIndex + 2);

                for (int i = modulesIndex + 1; i < folders.Length; i++)
                {
                    if (folders[i] == TEST_MODULES_FOLDER) inTestModule = true;
                    else if (folders[i] == SCREEN_MODULES_FOLDER) inScreenModule = true;
                    else if (folders[i].EndsWith(CHECK_MODULE_SUFFIX, StringComparison.Ordinal)) inCheckModule = true;
                }
            }

            return new SceneEntry(scenePath, name, module, moduleFolder, KindOf(inTestModule, inScreenModule, inCheckModule));
        }

        public bool Matches(string search)
        {
            if (string.IsNullOrEmpty(search)) return true;

            return Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   Module.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// A test module wins over the folder it sits in: a check module's own test scene is a
        /// test, and so is a screen's.
        /// </summary>
        private static SceneKind KindOf(bool inTestModule, bool inScreenModule, bool inCheckModule)
        {
            if (inTestModule) return inScreenModule ? SceneKind.ScreenTest : SceneKind.Test;
            if (inCheckModule) return SceneKind.Check;

            return SceneKind.Game;
        }
    }
}

#endif
