using System;
using FlowIoC.PoolModule.Components;
using UnityEngine;

namespace FlowIoC.PoolModule.Data.ValueObjects
{
    [Serializable]
    public class PoolItemCVO : PoolItemBaseCVO
    {
        public GameObject Prefab;

        public AssetReferenceSpawnableObject AddressablePrefab;

        public override object Asset => IsAddressable ? (object)AddressablePrefab : Prefab;
    }
}