using System.Collections.Generic;
using FlowIoC.ScreenModule.Layer;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.ScreenModule.Entity
{
    public class ScreenSafeArea : MonoBehaviour
    {
        [SerializeField] private bool _useWithSimulator;
        [SerializeField] private bool _runInUpdate;
        private List<RectTransform> _layerRects = new ();
        private ScreenOrientation _lastOrientation;
        private Vector2 _lastResolution = Vector2.zero;
        private Rect _lastSafeArea = Rect.zero;
        void Awake()
        {
            foreach (ScreenLayer layer in transform.GetComponentsInChildren<ScreenLayer>())
            {
                if (layer.IsSafeAreaExists)
                    _layerRects.Add(layer.GetComponent<RectTransform>());
            }

            SetVariables();

            ApplySafeArea();
        }

        private void SetVariables()
        {
            _lastOrientation = UnityEngine.Screen.orientation;
            _lastResolution.x = UnityEngine.Screen.width;
            _lastResolution.y = UnityEngine.Screen.height;
            _lastSafeArea = UnityEngine.Screen.safeArea;
        }

        void Update()
        {
            if (!_runInUpdate)
                return;
            
            if (Application.isMobilePlatform && UnityEngine.Screen.orientation != _lastOrientation) OrientationChanged();

            if (UnityEngine.Screen.width != _lastResolution.x || UnityEngine.Screen.height != _lastResolution.y) ResolutionChanged();

            if (UnityEngine.Screen.safeArea != _lastSafeArea) SafeAreaChanged();
        }

        void ApplySafeArea()
        {
            var anchorMin = _lastSafeArea.position;
            var anchorMax = _lastSafeArea.position + _lastSafeArea.size;

            if (_useWithSimulator)
            {
                anchorMin.x /= UnityEngine.Screen.currentResolution.width;
                anchorMin.y /= UnityEngine.Screen.currentResolution.height;
                anchorMax.x /= UnityEngine.Screen.currentResolution.width;
                anchorMax.y /= UnityEngine.Screen.currentResolution.height;
            }
            else
            {
                anchorMin.x /= UnityEngine.Screen.width;
                anchorMin.y /= UnityEngine.Screen.height;
                anchorMax.x /= UnityEngine.Screen.width;
                anchorMax.y /= UnityEngine.Screen.height;
            }
            
            foreach (var rect in _layerRects)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
            }
        }
        private void SafeAreaChanged()
        {
            FlowLogger.Log(SystemLogType.Screen, "[ScreenSafeArea.SafeAreaChanged][from(" + _lastSafeArea + ")][to(" + UnityEngine.Screen.safeArea + ")][time(" + Time.time + ")]");

            _lastSafeArea = UnityEngine.Screen.safeArea;
            
            ApplySafeArea();
        }

        private void OrientationChanged()
        {
            FlowLogger.Log(SystemLogType.Screen, "[ScreenSafeArea.OrientationChanged][from(" + _lastOrientation + ")][to(" + UnityEngine.Screen.orientation + ")][time(" + Time.time + ")]");

            _lastOrientation = UnityEngine.Screen.orientation;
            _lastResolution.x = UnityEngine.Screen.width;
            _lastResolution.y = UnityEngine.Screen.height;
        }

        private void ResolutionChanged()
        {
            FlowLogger.Log(SystemLogType.Screen, "[ScreenSafeArea.ResolutionChanged][from(" + _lastResolution + ")][to(" + UnityEngine.Screen.width + ", " + UnityEngine.Screen.height + ")][time(" + Time.time + ")]");

            _lastResolution.x = UnityEngine.Screen.width;
            _lastResolution.y = UnityEngine.Screen.height;
        }
    }
}