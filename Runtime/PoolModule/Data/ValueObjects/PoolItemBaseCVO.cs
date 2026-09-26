using System;
using UnityEngine;

namespace FlowIoC.PoolModule.Data.ValueObjects
{
    [Serializable]
    public abstract class PoolItemBaseCVO
    {
        public string PoolKey;

        public int InitialCreateCount = 10;
        public bool IsExtendable = true;
        public bool LazyLoad = false;
        public bool IsAddressable = false;

        public abstract object Asset { get; }
    }
}