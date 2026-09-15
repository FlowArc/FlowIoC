using System;
using System.Collections.Generic;
using System.IO;
using Modules.AssetDeliveryModule.Data.ValueObjects;

namespace Modules.AssetDeliveryModule.Initialization
{
    /// <summary>
    /// Bundle file name to the path the platform holds it at. Only a bundle the manifest lists
    /// is rewritten; the catalog, settings and every local bundle pass through untouched. The
    /// platform is asked once per bundle, because Apple says not to keep its URL beyond the
    /// process and not to fetch it on every load either.
    /// </summary>
    public class AssetPackPathMap
    {
        private readonly HashSet<string> _bundles = new();
        private readonly Dictionary<string, string> _resolved = new();
        private readonly Func<string, string> _pathOf;

        public AssetPackPathMap(IEnumerable<AssetPackVO> packs, Func<string, string> pathOf)
        {
            _pathOf = pathOf;

            foreach (AssetPackVO pack in packs)
            foreach (string bundle in pack.Bundles)
                _bundles.Add(bundle);
        }

        public string Transform(string internalId)
        {
            string file = Path.GetFileName(internalId.Replace('\\', '/'));
            if (!_bundles.Contains(file)) return internalId;

            if (!_resolved.TryGetValue(file, out string path))
            {
                path = _pathOf(file);
                _resolved[file] = string.IsNullOrEmpty(path) ? internalId : path;
            }

            return _resolved[file];
        }
    }
}