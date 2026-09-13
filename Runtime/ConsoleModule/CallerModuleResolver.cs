using System;
using System.Collections.Generic;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// The module a source file belongs to, read off its path: the innermost folder named
    /// <c>*Module</c> above the file. <c>Modules/MainModule/zScreenModules/MainScreenModule/Scripts/
    /// Runtime/Controllers/OpenMainScreenCommand.cs</c> is <c>MainScreenModule</c>, and a file outside
    /// any module - a probe under <c>Assets/DeviceTests</c>, a script at the project root - is
    /// <see cref="FlowModule.Default"/>.
    ///
    /// This is what lets a log name no channel: the compiler writes the caller's file path into
    /// the call through <c>[CallerFilePath]</c>, and the module is in the path. A test module is
    /// named <c>*TestModule</c> and has no channel of its own, so its files log on the module they
    /// test - the next <c>*Module</c> folder out.
    ///
    /// Answered once per path and remembered: the path is a compile-time constant, so a call site
    /// asks with the same string every time, and a log must not walk a path on every line.
    /// </summary>
    public class CallerModuleResolver
    {
        private const string ModuleSuffix = "Module";
        private const string TestModuleSuffix = "TestModule";

        private readonly Dictionary<string, string> _byPath = new Dictionary<string, string>(StringComparer.Ordinal);

        public string ChannelOf(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return FlowModule.Default;

            if (_byPath.TryGetValue(filePath, out string cached)) return cached;

            string channel = Resolve(filePath);
            _byPath[filePath] = channel;

            return channel;
        }

        private static string Resolve(string filePath)
        {
            string[] segments = filePath.Replace('\\', '/').Split('/');

            // The last segment is the file; the folders are what say where it lives.
            for (int index = segments.Length - 2; index >= 0; index--)
            {
                string folder = segments[index];

                if (!folder.EndsWith(ModuleSuffix, StringComparison.Ordinal)) continue;
                if (folder.Length == ModuleSuffix.Length) continue;
                if (folder.EndsWith(TestModuleSuffix, StringComparison.Ordinal)) continue;

                return folder;
            }

            return FlowModule.Default;
        }
    }
}
