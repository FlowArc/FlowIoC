using System;
using FlowIoC.PoolModule.Entities;

namespace FlowIoC.PoolModule.Addressable.Components
{
    [Serializable]
    public class AssetReferenceSpawnableObject : ComponentReference<IPoolableItem>
    {
        public AssetReferenceSpawnableObject(string guid) : base(guid)
        {
        }
    }
}
