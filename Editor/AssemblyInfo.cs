using System.Runtime.CompilerServices;
using FlowIoC.Editor.Help;
using UnityEngine.Scripting;

[assembly: Preserve]
[assembly: AlwaysLinkAssembly]
[assembly: InternalsVisibleTo("FlowIoC.Runtime")]
[assembly: InternalsVisibleTo("FlowIoC.Tests")]

// What the Help window calls the modules FlowIoC ships. Any other package's are listed under the
// package's own name unless it declares a title of its own the same way.
[assembly: ModuleGroup("FlowModules")]
