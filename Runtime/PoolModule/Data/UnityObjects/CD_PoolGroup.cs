using System.Collections.Generic;
using FlowIoC.PoolModule.Data.ValueObjects;
using UnityEngine;

namespace FlowIoC.PoolModule.Data.UnityObjects
{
    [CreateAssetMenu(fileName = "PoolGroup", menuName = "FlowIoC/PoolModule/Data/CD_PoolGroup", order = 1)]
    public class CD_PoolGroup : ScriptableObject
    {
        [SerializeField]
        private List<PoolItemVO> _items = new();

        public List<PoolItemVO> Items => _items;
    }
}