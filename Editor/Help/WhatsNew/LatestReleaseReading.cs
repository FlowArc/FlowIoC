#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// The one thing read out of a registry's document for a package: the version its "latest"
    /// tag points at. The document is npm's shape, which is what OpenUPM and every scoped
    /// registry Unity can talk to serve, and everything else in it is somebody else's business.
    ///
    /// A reply that is not that shape reads as no version, so a registry that has changed its
    /// mind about the format is quietly wrong rather than an exception while drawing help.
    /// </summary>
    internal class LatestReleaseReading
    {
        internal string Of(string registryDocument)
        {
            if (string.IsNullOrWhiteSpace(registryDocument))
                return string.Empty;

            try
            {
                JToken tag = JObject.Parse(registryDocument)["dist-tags"]?["latest"];

                return tag == null ? string.Empty : tag.ToString().Trim();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}

#endif
