using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ScreenModule.Data;
using UnityEngine;

namespace FlowIoC.ScreenModule.ViewsMediators.Manager
{
    [RequireComponent(typeof(ViewInjector))]
    [CustomClassHeader("SCREEN MANAGER", 1.0f, 0.5f, 0.0f, 0.8f, 0.3f, 0.0f, 14, "⚡ ")]
    internal class ScreenManager : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }
        public ScreenManagerVO ManagerData = new ();

        public Action<List<ScreenConfig>> UnRegisterScreenConfig = delegate { };

        [SerializeField] private List<ScreenConfig> _screenConfigs = new List<ScreenConfig>();

        public List<ScreenConfig> GetScreenConfigs()
        {
            if (_screenConfigs == null || _screenConfigs.Count <= 0) return _screenConfigs;
            return _screenConfigs;
        }

        public void UnregisterScreenConfig() => UnRegisterScreenConfig.Invoke(_screenConfigs);
        
        public void AddScreenToConfig(ScreenConfig screenConfig)
        {
            _screenConfigs.Add(screenConfig);
        }
    }
}