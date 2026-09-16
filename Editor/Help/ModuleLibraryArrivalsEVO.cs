#if UNITY_EDITOR

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// What one package shipped when this reader last looked, and which of those modules were
    /// not there at the package version before it.
    /// </summary>
    internal class ModuleLibraryArrivalsEVO
    {
        internal string Version { get; set; }
        internal string[] Shipped { get; set; } = new string[0];
        internal string[] New { get; set; } = new string[0];
    }
}

#endif
