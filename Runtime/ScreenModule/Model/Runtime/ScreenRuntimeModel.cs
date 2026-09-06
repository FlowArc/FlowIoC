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

            (int, Type) key = (screenBody.Data.ManagerId, screenBody.Data.ScreenType);

            if (!_passiveScreens.TryGetValue(key, out List<IScreenBody> pooled))
            {
                pooled = new List<IScreenBody>();
                _passiveScreens[key] = pooled;
            }

            // A screen shown twice used to be told to hide twice, and each hide parked it - so the
            // same instance sat in the pool twice and was opened as two screens at once.
            if (pooled.Contains(screenBody))
            {
                FlowLogger.LogWarning(SystemLogType.Screen,
                    $"[ScreenRuntimeModel.AddToPassivePool] {screenBody.Data.ScreenType.Name} is already pooled at manager {screenBody.Data.ManagerId}. Skipping duplicate return.");
                return;
            }

            screenBody.Data.AddState(ScreenState.InPool);

            pooled.Add(screenBody);
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

            List<IScreenBody> tagged = _activeTagScreens[managerId][screenBody.Data.Tag];

            // The other two registers are keyed, so showing a screen twice overwrites its entry.
            // This one is a list and would hold the same screen twice.
            if (!tagged.Contains(screenBody))
                tagged.Add(screenBody);
        }

        /// <summary>
        /// Takes a screen out of the three active registers. Each one is checked for this screen
        /// rather than for its slot: a screen force-opened over another takes that other's layer,
        /// and the one it replaced closing later would otherwise clear the layer out from under
        /// the screen now standing in it.
        /// </summary>
        public void RemoveFromActivePools(IScreenBody screenBody)
        {
            FlowLogger.Log(SystemLogType.Screen, $"[ScreenRuntimeModel][RemoveFromActivePools] {screenBody.Data.ScreenType.Name}");

            screenBody.Data.RemoveState(ScreenState.InUse);

            int managerId = screenBody.Data.ManagerId;

            if (_activeScreens.TryGetValue(managerId, out Dictionary<Type, IScreenBody> byType)
                && byType.TryGetValue(screenBody.Data.ScreenType, out IScreenBody registered)
                && registered == screenBody)
                byType.Remove(screenBody.Data.ScreenType);

            if (_activeLayerScreens.TryGetValue(managerId, out Dictionary<int, IScreenBody> byLayer)
                && byLayer.TryGetValue(screenBody.Data.LayerIndex, out IScreenBody inLayer)
                && inLayer == screenBody)
                byLayer.Remove(screenBody.Data.LayerIndex);

            if (_activeTagScreens.TryGetValue(managerId, out Dictionary<ScreenTag, List<IScreenBody>> byTag)
                && byTag.TryGetValue(screenBody.Data.Tag, out List<IScreenBody> tagged))
                tagged.Remove(screenBody);
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

        /// <summary>
        /// A copy, the way <see cref="GetActiveManagerScreens"/> already answered. Hiding the
        /// screens of a tag takes each one out of this very list, and handing back the live one
        /// meant the caller was removing from the collection it was walking.
        /// </summary>
        public bool GetActiveTagScreens(ScreenTag tag, int managerId, out List<IScreenBody> list)
        {
            list = null;

            if (!_activeTagScreens.TryGetValue(managerId, out Dictionary<ScreenTag, List<IScreenBody>> byTag))
                return false;

            if (!byTag.TryGetValue(tag, out List<IScreenBody> tagged))
                return false;

            list = new List<IScreenBody>(tagged);
            return true;
        }
    }
}