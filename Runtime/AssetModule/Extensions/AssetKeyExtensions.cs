using UnityEngine.AddressableAssets;

namespace FlowIoC.AssetModule.Extensions
{
    internal static class AssetKeyExtensions
    {
        public static bool TryNormalize(object key, out string registryKey, out object runtimeKey)
        {
            registryKey = null;
            runtimeKey = null;

            switch (key)
            {
                case null:
                    return false;

                case string s:
                    if (string.IsNullOrEmpty(s)) return false;
                    registryKey = s;
                    runtimeKey = s;
                    return true;

                case AssetReference assetReference:
                    if (!assetReference.RuntimeKeyIsValid()) return false;
                    runtimeKey = assetReference.RuntimeKey;
                    registryKey = assetReference.RuntimeKey.ToString();
                    return true;

                default:
                    registryKey = key.ToString();
                    runtimeKey = key;
                    return true;
            }
        }
    }
}
