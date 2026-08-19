using System;
using FlowIoC.PoolModule.Entities;

namespace FlowIoC.PoolModule.Components
{
    [Serializable]
    public class AssetReferenceSpawnableObject : ComponentReference<IPoolableItem>
    {
        public AssetReferenceSpawnableObject(string guid) : base(guid)
        {
        }
    }
}
