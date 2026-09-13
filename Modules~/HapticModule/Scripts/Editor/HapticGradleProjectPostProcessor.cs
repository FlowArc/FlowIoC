#if UNITY_EDITOR
using System.IO;
using FlowIoC.ConsoleModule;
using UnityEditor.Android;

namespace Modules.HapticModule.Editor
{
    /// <summary>
    /// Runs after Unity has generated the Gradle project, on the unityLibrary manifest. The step is
    /// idempotent, so an incremental build that kept the previous manifest finds the permission
    /// there and changes nothing. Nothing calls it; Unity finds it by its interface.
    /// </summary>
    internal class HapticGradleProjectPostProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");

            if (!File.Exists(manifestPath))
            {
                FlowLogger.LogError(FlowModule.HapticModule,
                    $"OnPostGenerateGradleAndroidProject - no AndroidManifest.xml at {manifestPath}, so android.permission.VIBRATE was not added and nothing will vibrate.");
                return;
            }

            string before = File.ReadAllText(manifestPath);
            string after = new HapticAndroidManifest().AddVibratePermission(before);

            if (after == before)
            {
                FlowLogger.Log(FlowModule.HapticModule, "OnPostGenerateGradleAndroidProject - android.permission.VIBRATE was already in the unityLibrary manifest");
                return;
            }

            File.WriteAllText(manifestPath, after);
            FlowLogger.Log(FlowModule.HapticModule, "OnPostGenerateGradleAndroidProject - android.permission.VIBRATE added to the unityLibrary manifest");
        }
    }
}
#endif
