#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// The title the Help window gives the modules an assembly's pages install, declared once on
    /// the assembly: <c>[assembly: ModuleGroup("FlowModules")]</c>. Without it a package's group
    /// is called after the package - "&lt;displayName&gt; Modules" - which reads right for most and
    /// costs nothing to declare, so the attribute is for a package that wants a spelling of its own.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ModuleGroupAttribute : Attribute
    {
        public ModuleGroupAttribute(string title)
        {
            Title = title;
        }

        public string Title { get; }
    }
}

#endif
