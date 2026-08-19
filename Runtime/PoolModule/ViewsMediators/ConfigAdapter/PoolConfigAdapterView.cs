using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.PoolModule.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.PoolModule.ViewsMediators.ConfigAdapter
{
    [RequireComponent(typeof(ViewInjector))]
    [CustomClassHeader("POOL CONFIG ADAPTER", 1.0f, 0.5f, 0.0f, 0.2f, 0.2f, 0.8f, 14)]
    public class PoolConfigAdapterView : MonoBehaviour, IView
    {
        public bool UnregisterWhenViewDestroyed;
        public bool IsRegistered { get; set; }

        [SerializeField] private SerializedDictionary<string, PoolGroupCVO> _poolConfigs = new ();

        public SerializedDictionary<string, PoolGroupCVO> GetPoolConfigs() => _poolConfigs;

        public IEnumerable<string> GroupKeys => _poolConfigs.Keys;
    }
}