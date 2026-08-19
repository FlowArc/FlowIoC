using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FlowIoC.AssetModule.Data
{
    internal sealed class AssetEntryVO
    {
        public AsyncOperationHandle Handle;
        public object Result;
        public Type AssetType;

        public readonly HashSet<string> Owners = new();
        public int UnscopedClaims;

        public bool HasOwners => Owners.Count > 0 || UnscopedClaims > 0;
    }
}
