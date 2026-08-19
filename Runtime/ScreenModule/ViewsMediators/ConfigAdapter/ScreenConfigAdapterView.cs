using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ScreenModule.Data;
using UnityEngine;

namespace FlowIoC.ScreenModule.ViewsMediators.ConfigAdapter
{
    [RequireComponent(typeof(ViewInjector))]
    [CustomClassHeader("SCREEN CONFIG ADAPTER", 1.0f, 0.5f, 0.0f, 0.2f, 0.2f, 0.8f, 14)]
    internal class ScreenConfigAdapterView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }
        public int ManagerIdToRegister => _managerIdToRegister;
        public Action<int, List<ScreenConfig>> UnRegisterScreenConfig = delegate { };

        [SerializeField] private int _managerIdToRegister;
        [SerializeField] private List<ScreenConfig> _screenConfigs = new List<ScreenConfig>();
        
        public List<ScreenConfig> GetScreenConfigs()
        {
            if (_screenConfigs == null || _screenConfigs.Count <= 0) return _screenConfigs;
            return _screenConfigs;
        }

        public void UnregisterScreenConfig() => UnRegisterScreenConfig.Invoke(ManagerIdToRegister, _screenConfigs);
    }
}