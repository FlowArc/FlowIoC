#if UNITY_EDITOR

using FlowIoC.ScreenModule.Data;

namespace FlowIoC.Editor.CodeGenerator.Screens
{
    /// <summary>
    /// Where Create Module saves a screen's prefab, read off how the screen loads it. An
    /// addressable screen goes to the module's Prefabs folder under the module's name and is given
    /// an address. A Resource screen goes where Resources.Load will look for the path its context
    /// declares - the module's own Resources folder - and is given no address, because a prefab
    /// the screen never loads through Addressables has no business in a group.
    /// </summary>
    internal class ScreenPrefabPlacement
    {
        public string Folder { get; private set; }

        public string Name { get; private set; }

        public bool Addressable { get; private set; }

        /// <summary>
        /// An empty Resource path falls back to the module's name and is written back into the
        /// settings, so the context the template renders loads the prefab saved here.
        /// </summary>
        public ScreenPrefabPlacement For(ScreenModuleSettings settings, string moduleName, string modulePath, string prefabsFolder)
        {
            if (settings == null || settings.LoadType != ScreenLoadType.Resource)
            {
                return new ScreenPrefabPlacement {Folder = prefabsFolder, Name = moduleName, Addressable = true};
            }

            if (string.IsNullOrWhiteSpace(settings.ResourcePath))
                settings.ResourcePath = moduleName;

            string path = settings.ResourcePath.Trim().Replace('\\', '/').Trim('/');
            int slash = path.LastIndexOf('/');
            string folder = modulePath.Replace('\\', '/').TrimEnd('/') + "/Resources";

            return new ScreenPrefabPlacement
            {
                Folder = slash < 0 ? folder : folder + "/" + path.Substring(0, slash),
                Name = slash < 0 ? path : path.Substring(slash + 1),
                Addressable = false
            };
        }
    }
}

#endif
