using System.Runtime.CompilerServices;

// The module's Commands and its internal holder stay internal; the workspace's test assembly is
// the one reader allowed past that, the way the package's own tests read the package.
[assembly: InternalsVisibleTo("FlowIoC.Dev.Editor.Tests")]
