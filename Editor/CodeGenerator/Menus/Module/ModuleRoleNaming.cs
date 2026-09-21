#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The names Create Module writes for a module's Root and Context. Only these two carry the
    /// role: the module folder, its assembly and its namespaces are named for what the module
    /// does, and a Service module is <c>CounterModule</c> holding <c>CounterServiceRoot</c> and
    /// <c>CounterServiceContext</c>.
    /// </summary>
    internal class ModuleRoleNaming
    {
        private const string ROOT = "Root";
        private const string CONTEXT = "Context";

        public string Suffix(ModuleRole role)
        {
            switch (role)
            {
                case ModuleRole.System: return "System";
                case ModuleRole.Service: return "Service";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// The module name with the role spelled after it. A name that ends in the role already
        /// keeps what it has, so <c>CounterService</c> does not become <c>CounterServiceService</c>.
        /// </summary>
        public string Apply(string moduleName, ModuleRole role)
        {
            string suffix = Suffix(role);

            if (string.IsNullOrEmpty(suffix) || string.IsNullOrEmpty(moduleName))
                return moduleName;

            return moduleName.EndsWith(suffix, StringComparison.Ordinal) ? moduleName : moduleName + suffix;
        }

        public string RootName(string moduleName, ModuleRole role) => Apply(moduleName, role) + ROOT;

        public string ContextName(string moduleName, ModuleRole role) => Apply(moduleName, role) + CONTEXT;

        /// <summary>
        /// The role a module of this type is actually written with. Only a main module that gets a
        /// Root of its own is one of the three: a test module's Root is a test Root, a screen
        /// module has no Root, and a sub module's context hangs off its parent's. Every one of
        /// those is written plain whatever role the caller carried - the window collapses it before
        /// asking, and a script that calls the generator directly gets the same answer there.
        /// </summary>
        public ModuleRole Effective(ModuleRole role, ModuleType type, bool createRoot) =>
            type == ModuleType.Main && createRoot ? role : ModuleRole.Core;
    }
}
#endif