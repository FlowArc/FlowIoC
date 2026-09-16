using System.Runtime.CompilerServices;
using FlowIoC.Editor.Help;
using UnityEngine.Scripting;

[assembly: Preserve]
[assembly: AlwaysLinkAssembly]
[assembly: InternalsVisibleTo("FlowIoC.Runtime")]
[assembly: InternalsVisibleTo("FlowIoC.Dev.Editor.CoreTests")]

// The host's publish tool writes a shipped module's record and raises its version with the same
// classes the installer reads them with, so the two can never drift.
[assembly: InternalsVisibleTo("FlowIoC.Dev.Editor")]

// What the Help window calls the modules FlowIoC ships. Any other package's are listed under the
// package's own name unless it declares a title of its own the same way.
[assembly: ModuleGroup("FlowModules")]
