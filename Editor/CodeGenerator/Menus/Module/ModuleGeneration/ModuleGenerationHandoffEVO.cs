#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// What the first half of a Create Module run hands the second: the run's own answers,
    /// written whole once the files are on disk and read back after the domain reload those
    /// files trigger. One record per run rather than a key per value, so nothing an earlier
    /// run wrote can be read as this run's answer - the run that asks for no scene records no
    /// scene path, and that is the whole of what the reader checks.
    /// </summary>
    [Serializable]
    internal class ModuleGenerationHandoffEVO
    {
        public ModuleType ModuleType;
        public string ModuleName;

        /// <summary>
        /// The Root class the generator wrote - PlayerSystemRoot for a System, CounterServiceRoot
        /// for a Service - and empty when the run wrote none.
        /// </summary>
        public string RootName;

        /// <summary>The namespace the Root and Context were written in, which is how the Root type is found again.</summary>
        public string ContextNamespace;

        public string ViewNamespace;
        public string ScreenContextFullName;
        public string ScreenPrefabPath;

        /// <summary>The prefab's file name: the module's name, or the last segment of a Resource path.</summary>
        public string ScreenPrefabName;

        /// <summary>False for a screen loaded from Resources, whose prefab is given no address.</summary>
        public bool ScreenIsAddressable;

        /// <summary>
        /// The asset path of the scene the first half made and saved, and empty when the run
        /// asked for none. After the reload the Root goes into the scene at this path and into
        /// no other.
        /// </summary>
        public string ScenePath;
    }
}
#endif