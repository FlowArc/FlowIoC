#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.AssetDeliveryModule.Editor
{
    internal class IosPackagingReport
    {
        public readonly List<string> PackFolders = new();
        public string Script;
        public bool Ran;
    }

    /// <summary>
    /// After an iOS Addressables build: each delivery group's bundles leave the build folder -
    /// so they never enter the app - for a folder of their own with a Manifest.json, and a
    /// script beside them runs ba-package per pack. On macOS the script runs here and now; on
    /// Windows it waits for the Mac, and the Help page says so. The catalog stays where it was
    /// and keeps the bundles' local ids, which the runtime transform rewrites.
    /// </summary>
    internal class IosAssetPackPackager
    {
        private const string SCRIPT = "package-asset-packs.sh";

        private readonly bool _runXcrun;

        public IosAssetPackPackager(bool runXcrun) => _runXcrun = runXcrun;

        public IosPackagingReport Package(string buildPath, IReadOnlyList<AssetPackVO> packs, string outputRoot)
        {
            var report = new IosPackagingReport();
            var manifest = new IosAssetPackManifest();
            var script = new StringBuilder();
            script.Append("#!/bin/sh\n");
            script.Append("# Written by FlowIoC's asset delivery module. Run on a Mac with Xcode 26: packs one .aar per asset pack.\n");
            script.Append("set -e\n");
            script.Append("cd \"$(dirname \"$0\")\"\n");
            Directory.CreateDirectory(outputRoot);

            foreach (AssetPackVO pack in packs)
            {
                string folder = Path.Combine(outputRoot, pack.Pack);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                Directory.CreateDirectory(folder);

                foreach (string bundle in pack.Bundles)
                {
                    string source = Path.Combine(buildPath, bundle);

                    if (!File.Exists(source))
                    {
                        FlowLogger.LogError($"IosAssetPackPackager - '{bundle}' of pack '{pack.Pack}' is not in the build folder {buildPath}; build Addressables for iOS first.");
                        continue;
                    }

                    File.Move(source, Path.Combine(folder, bundle));
                }

                File.WriteAllText(Path.Combine(folder, "Manifest.json"), manifest.ToJson(pack));
                script.Append($"(cd \"{pack.Pack}\" && xcrun ba-package Manifest.json -o \"../{pack.Pack}.aar\")\n");
                report.PackFolders.Add(folder);
            }

            report.Script = Path.Combine(outputRoot, SCRIPT);
            File.WriteAllText(report.Script, script.ToString());

            if (_runXcrun && Application.platform == RuntimePlatform.OSXEditor)
                report.Ran = Run(report.Script);

            return report;
        }

        private bool Run(string script)
        {
            var process = Process.Start(new ProcessStartInfo("/bin/sh", $"\"{script}\"")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });

            if (process == null)
            {
                FlowLogger.LogError("IosAssetPackPackager - /bin/sh could not be started, so the packs were not archived.");
                return false;
            }

            process.WaitForExit();

            if (process.ExitCode == 0) return true;

            FlowLogger.LogError($"IosAssetPackPackager - ba-package failed ({process.ExitCode}): {process.StandardError.ReadToEnd()}");
            return false;
        }
    }
}
#endif
