using System.IO;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Constants;
using UnityEngine.AddressableAssets;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Networking;
#endif

namespace Modules.AssetDeliveryModule.Models
{
    /// <summary>
    /// Reads FlowAssetPacks.json from where the catalog is: Addressables.RuntimePath - inside the
    /// APK on Android, so through UnityWebRequest there; a plain file everywhere else, including
    /// the Editor, where the path is the last build's folder under Library.
    /// </summary>
    public class AddressablesRuntimeManifestSource : IAssetPackManifestSource
    {
        public async Task<string> ReadAsync()
        {
            string path = Path.Combine(Addressables.RuntimePath, AssetDeliveryConstants.MANIFEST_FILE);

#if UNITY_ANDROID && !UNITY_EDITOR
            using UnityWebRequest request = UnityWebRequest.Get(path);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                FlowLogger.Log($"AddressablesRuntimeManifestSource - no manifest at {path}: {request.error}. No packs.");
                return null;
            }

            return request.downloadHandler.text;
#else
            await Task.CompletedTask;

            if (!File.Exists(path))
            {
                FlowLogger.Log($"AddressablesRuntimeManifestSource - no manifest at {path}. No packs.");
                return null;
            }

            return File.ReadAllText(path);
#endif
        }
    }
}
