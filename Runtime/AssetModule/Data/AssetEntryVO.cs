using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Gateway;

namespace FlowIoC.AssetModule.Data
{
    internal sealed class AssetEntryVO
    {
        public IAssetHandle Handle;
        public object Result;
        public Type AssetType;

        /// <summary>The async load in flight for this entry; a second caller waits on it rather than loading again.</summary>
        public Task<object> Loading;

        public readonly HashSet<string> Owners = new();
        public int UnscopedClaims;

        public bool HasOwners => Owners.Count > 0 || UnscopedClaims > 0;
    }
}