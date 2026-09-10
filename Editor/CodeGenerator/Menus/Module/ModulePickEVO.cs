#if UNITY_EDITOR
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// One module a generator offers as the parent of what it is about to write: the module's
    /// descriptor, where it lives, and the module it sits in.
    /// </summary>
    internal class ModulePickEVO : IModuleTreeItem
    {
        public string Name { get; set; }
        public ModuleKind Kind { get; set; }
        public string ParentName { get; set; }

        /// <summary>
        /// The module folder, absolute, in the shape the generators compare against: what they
        /// build with Path.Combine off Application.dataPath - forward slashes through the data
        /// folder, the platform's separator after it. ModuleGenerator tells a top level module
        /// from a nested one by plain string equality on this, so the shape is not free to drift.
        /// </summary>
        public string Path { get; set; }

        public ModuleDescriptorEVO Descriptor { get; set; }
    }
}

#endif
