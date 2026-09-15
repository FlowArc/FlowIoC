using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.AssetDeliveryModule.Models
{
    /// <summary>The packs the build declared. Read once on the first question, then answered from memory.</summary>
    public class AssetPackManifestModel : IAssetPackManifestModel
    {
        [Inject] private IAssetPackManifestSource _source { get; set; }

        private IReadOnlyList<AssetPackVO> _packs;
        private Task<IReadOnlyList<AssetPackVO>> _reading;

        public Task<IReadOnlyList<AssetPackVO>> GetPacksAsync()
        {
            if (_packs != null) return Task.FromResult(_packs);
            return _reading ??= ReadAsync();
        }

        public async Task<AssetPackVO> FindByBundleAsync(string bundle)
        {
            IReadOnlyList<AssetPackVO> packs = await GetPacksAsync();

            foreach (AssetPackVO pack in packs)
                if (pack.Bundles.Contains(bundle))
                    return pack;

            return null;
        }

        private async Task<IReadOnlyList<AssetPackVO>> ReadAsync()
        {
            string text = await _source.ReadAsync();
            var packs = new List<AssetPackVO>();

            if (!string.IsNullOrEmpty(text))
            {
                AssetPackManifestVO manifest = JsonUtility.FromJson<AssetPackManifestVO>(text);

                if (manifest?.Packs == null)
                    FlowLogger.LogError("AssetPackManifestModel - FlowAssetPacks.json could not be read; the build that wrote it and this runtime disagree on its shape.");
                else
                    packs.AddRange(manifest.Packs);
            }

            _packs = packs;
            return _packs;
        }
    }
}
