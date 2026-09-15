#if UNITY_EDITOR
using System.Text;
using Modules.AssetDeliveryModule.Data.ValueObjects;
using Modules.AssetDeliveryModule.Enums;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>
    /// The Manifest.json ba-package reads for one pack. Install Time is Apple's essential -
    /// downloaded as part of the install - and Fast Follow its prefetch, both on the first
    /// install and on every update; On Demand waits for Ensure.
    /// </summary>
    internal class IosAssetPackManifest
    {
        public string ToJson(AssetPackVO pack)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine($"  \"assetPackID\": \"{pack.Pack}\",");
            builder.AppendLine("  \"downloadPolicy\": " + Policy(pack.Policy) + ",");
            builder.AppendLine("  \"fileSelectors\": [");

            for (int index = 0; index < pack.Bundles.Count; index++)
                builder.AppendLine($"    {{ \"file\": \"{pack.Bundles[index]}\" }}" + (index < pack.Bundles.Count - 1 ? "," : string.Empty));

            builder.AppendLine("  ],");
            builder.AppendLine("  \"platforms\": [ \"iOS\", \"iPadOS\" ]");
            builder.Append("}");
            return builder.ToString();
        }

        private string Policy(AssetPackPolicy policy) => policy switch
        {
            AssetPackPolicy.InstallTime => "{ \"essential\": { \"installationEventTypes\": [ \"firstInstallation\", \"subsequentUpdate\" ] } }",
            AssetPackPolicy.FastFollow => "{ \"prefetch\": { \"installationEventTypes\": [ \"firstInstallation\", \"subsequentUpdate\" ] } }",
            _ => "{ \"onDemand\": {} }"
        };
    }
}
#endif
