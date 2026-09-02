using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.ScreenModule.Model.Runtime
{
    internal class ScreenRuntimeModel : IScreenRuntimeModel, IConstructable
    {
        [ShowInModelViewer] private readonly Dictionary<(int managerId, Type viewType), List<IScreenBody>> _passiveScreens = new();
        [ShowInModelViewer] private readonly Dictionary<int, Dictionary<Type, IScreenBody>> _activeScreens = new();
        [ShowInModelViewer] private readonly Dictionary<int, Dictionary<int, IScreenBody>> _activeLayerScreens = new();
        [ShowInModelViewer] private readonly Dictionary<int, Dictionary<ScreenTag, List<IScreenBody>>> _activeTagScreens = new();

        private Transform _poolParent;

        public bool IsPostConstructed { get; set; }
        public bool IsDeConstructed { get; set; }

        public void PostConstruct()
        {
            CreatePoolParent();
        }

        public void Deconstruct()
        {
            foreach (var managerScreens in _activeScreens.Values)
            {
                foreach (var screen in managerScreens.Values)
                {
                    if (screen is Component component && component != null)
                        Object.Destroy(component.gameObject);
                }
            }

            foreach (var screenList in _passiveScreens.Values)
            {
                foreach (var screen in screenList)
                {
                    if (screen is Component component && component != null)
                        Object.Destroy(component.gameObject);
                }
            }

            _activeScreens.Clear();
            _passiveScreens.Clear();
            _activeLayerScreens.Clear();
            _activeTagScreens.Clear();

            if (_poolParent != null)
                Object.Destroy(_poolParent.gameObject);
        }

        private void CreatePoolParent()
        {
            _poolParent = new GameObject("[Screen_Pool]").transform;

            Object.DontDestroyOnLoad(_poolParent.gameObject);
        }

        public void AddToPassivePool(IScreenBody screenBody)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenRuntimeModel][AddToPassivePool] {screenBody.Data.ScreenType.Name}");

            screenBody.Data.AddState(ScreenState.InPool);

            (int, Type) key = (screenBody.Data.ManagerId, screenBody.Data.ScreenType);

            if (!_passiveScreens.ContainsKey(key))
                _passiveScreens[key] = new List<IScreenBody>();

            _passiveScreens[key].Add(screenBody);
            screenBody.transform.SetParent(_poolParent);
            //screenBody.gameObject.SetActive(false);
        }

        public bool GetScreen<T>(int managerId, out T screen) where T : IScreenBody
        {
            (int, Type) key = (managerId, typeof(T));

            if (!_passiveScreens.TryGetValue(key, out List<IScreenBody> pooled) || pooled.Count == 0)
            {
                screen = default;
                return false;
            }

            IScreenBody pooledScreen = pooled[0];
            pooled.RemoveAt(0);

            screen = (T) pooledScreen;
            return true;
        }

        public void RemoveFromPassivePool(IScreenBody screenBody)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenRuntimeModel][RemoveFromPassivePool] {screenBody.Data.ScreenType.Name}");

            screenBody.Data.RemoveState(ScreenState.InUse);
            screenBody.Data.RemoveState(ScreenState.InPool);

            (int, Type) key = (screenBody.Data.ManagerId, screenBody.Data.ScreenType);
            if (!_passiveScreens.TryGetValue(key, out List<IScreenBody> passiveScreens)) return;

            passiveScreens.Remove(screenBody);
        }

        public void AddToActivePools(IScreenBody screenBody)
        {
            screenBody.Data.RemoveState(ScreenState.InPool);
            screenBody.Data.AddState(ScreenState.InUse);
            var managerId = screenBody.Data.ManagerId;

            if (!_activeScreens.ContainsKey(managerId))
                _activeScreens[managerId] = new Dictionary<Type, IScreenBody>();
            _activeScreens[managerId][screenBody.Data.ScreenType] = screenBody;

            if (!_activeLayerScreens.ContainsKey(managerId))
                _activeLayerScreens[managerId] = new Dictionary<int, IScreenBody>();
            _activeLayerScreens[managerId][screenBody.Data.LayerIndex] = screenBody;

            if (!_activeTagScreens.ContainsKey(managerId))
                _activeTagScreens[managerId] = new Dictionary<ScreenTag, List<IScreenBody>>();

            if (!_activeTagScreens[managerId].ContainsKey(screenBody.Data.Tag))
                _activeTagScreens[managerId].Add(screenBody.Data.Tag, new List<IScreenBody>());

            _activeTagScreens[managerId][screenBody.Data.Tag].Add(screenBody);
        }

        public void RemoveFromActivePools(IScreenBody screenBody)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenRuntimeModel][RemoveFromActivePools] {screenBody.Data.ScreenType.Name}");

            screenBody.Data.RemoveState(ScreenState.InUse);

            var managerId = screenBody.Data.ManagerId;

            _activeScreens[managerId].Remove(screenBody.Data.ScreenType);
            _activeLayerScreens[managerId].Remove(screenBody.Data.LayerIndex);
            _activeTagScreens[managerId][screenBody.Data.Tag].Remove(screenBody);
        }

        public bool IsLayerFull(int layerIndex, int managerId, out IScreenBody screenBody)
        {
            screenBody = null;

            if (!_activeLayerScreens.ContainsKey(managerId)) return false;
            if (!_activeLayerScreens[managerId].ContainsKey(layerIndex)) return false;
            screenBody = _activeLayerScreens[managerId][layerIndex];
            return screenBody != null;
        }

        public bool IsScreenActive(Type screenType, int managerId, out IScreenBody screenBody)
        {
            screenBody = null;

            if (!_activeScreens.ContainsKey(managerId)) return false;
            if (!_activeScreens[managerId].ContainsKey(screenType)) return false;
            screenBody = _activeScreens[managerId][screenType];
            return screenBody != null;
        }

        public List<IScreenBody> GetAllActiveScreens()
        {
            List<IScreenBody> list = new List<IScreenBody>();
            foreach (var managerScreens in _activeScreens.Values)
                list.AddRange(managerScreens.Values);

            return list;
        }

        public bool GetActiveManagerScreens(int managerId, out List<IScreenBody> list)
        {
            list = null;
            if (!_activeScreens.ContainsKey(managerId)) return false;
            list = new List<IScreenBody>(_activeScreens[managerId].Values);
            return list != null;
        }

        public bool GetActiveTagScreens(ScreenTag tag, int managerId, out List<IScreenBody> list)
        {
            list = null;
            if (!_activeTagScreens.ContainsKey(managerId)) return false;
            if (!_activeTagScreens[managerId].ContainsKey(tag)) return false;
            list = _activeTagScreens[managerId][tag];
            return list != null;
        }
    }
}