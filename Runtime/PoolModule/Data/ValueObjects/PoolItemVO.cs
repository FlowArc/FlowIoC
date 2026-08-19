using System;
using UnityEngine;
using FlowIoC.PoolModule.Addressable.Components;

namespace FlowIoC.PoolModule.Data.ValueObjects
{
    [Serializable]
    public class PoolItemVO : PoolItemBaseCVO
    {
        [Header("Asset Config - Direct Prefab")]
        public GameObject Prefab;

        [Header("Asset Config - Addressable Prefab")]
        public AssetReferenceSpawnableObject AddressablePrefab;

        public override object Asset => IsAddressable ? (object)AddressablePrefab : Prefab;
    }
}