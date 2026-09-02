using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.PoolModule.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.PoolModule.ViewsMediators.ConfigAdapter
{
    [RequireComponent(typeof(ViewInjector))]
    public class PoolConfigAdapterView : MonoBehaviour, IView
    {
        public bool UnregisterWhenViewDestroyed;
        public bool IsRegistered { get; set; }

        [SerializeField] private SerializedDictionary<string, PoolGroupCVO> _poolConfigs = new ();

        public SerializedDictionary<string, PoolGroupCVO> GetPoolConfigs() => _poolConfigs;

        public IEnumerable<string> GroupKeys => _poolConfigs.Keys;
    }
}