#if UNITY_EDITOR && UNITY_IOS
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>
    /// After Unity writes the Xcode project: the downloader extension, the App Group and the
    /// Background Assets keys go in. Nothing calls it; Unity finds it by its interface. The step
    /// is idempotent, so an appended build that kept the project finds them there and changes
    /// nothing.
    /// </summary>
    internal class AssetDeliveryIosPostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;

            new AssetDeliveryXcodeProject().Apply(report.summary.outputPath, PlayerSettings.applicationIdentifier);
            FlowLogger.Log($"OnPostprocessBuild - the Background Assets downloader extension and keys are in {report.summary.outputPath}");
        }
    }
}
#endif
