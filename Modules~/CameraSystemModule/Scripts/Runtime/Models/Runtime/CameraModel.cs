using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CameraSystemModule.Shared.Data.ValueObjects;
using Modules.CameraSystemModule.RootsContexts;
using Modules.CameraSystemModule.Shared.Enums;
using Unity.Cinemachine;
using UnityEngine;

namespace Modules.CameraSystemModule.Models.Runtime
{
    internal class CameraModel : ICameraModel, IConstructable
    {
        [Inject(nameof(CameraContext))] private GameObject _root { get; set; }
        [ShowInModelViewer] private readonly Dictionary<CameraName, CameraCVO> _cameras = new();
        [ShowInModelViewer] private CinemachineCamera _activeCamera;
        [ShowInModelViewer] private CinemachineBlenderSettings _sharedBlenderSettings;
        private const int ACTIVE_PRIORITY = 10;
        private const int INACTIVE_PRIORITY = 0;

        public bool IsPostConstructed { get; set; }
        public bool IsDeConstructed { get; set; }

        [ShowInModelViewer] private Dictionary<CameraName, Vector3> _cameraLastPosition = new();

        public void PostConstruct()
        {
            var adapter = _root.GetComponent<RootAdapter>();
            _sharedBlenderSettings = adapter.GetScriptable<CinemachineBlenderSettings>("CD_CameraCustomBlends");
        }

        public bool TryGetCameraLastPos(CameraName type, out Vector3 pos) => _cameraLastPosition.TryGetValue(type, out pos);
        public void SetCameraLastPos(CameraName type, Vector3 pos) => _cameraLastPosition[type] = pos;

        public void RegisterCamera(CameraName cameraId, CameraCVO config)
        {
            _cameras[cameraId] = config;

            if (config.ActivateAtRegister)
            {
                _activeCamera = config.Camera;
                _activeCamera.Priority = ACTIVE_PRIORITY;
            }
            else
                config.Camera.Priority = INACTIVE_PRIORITY;

            if (_sharedBlenderSettings != null && config.BlenderSettings != null)
                UpdateBlenderSettingsEntry(config.BlenderSettings, config.OverrideBlends);
        }

        public void UnregisterCamera(CameraName cameraId)
        {
            if (_sharedBlenderSettings != null)
                RemoveBlenderSettingsEntry(cameraId);

            _cameras.Remove(cameraId);
        }

        public bool TryGetCamera(CameraName cameraId, out CameraCVO config)
        {
            return _cameras.TryGetValue(cameraId, out config);
        }

        public void SetActiveCamera(CameraName cameraId)
        {
            if (_cameras.TryGetValue(cameraId, out var config))
            {
                if (_activeCamera != null) _activeCamera.Priority = INACTIVE_PRIORITY;

                if (config.Camera != null)
                {
                    _activeCamera = config.Camera;
                    _activeCamera.Priority = ACTIVE_PRIORITY;
                }
            }
            else
            {
                FlowLogger.LogError(FlowLogType.CameraSystemModule, $"No active camera {cameraId}");
            }
        }

        public CinemachineCamera GetActiveCamera() => _activeCamera;
        public Transform GetCameraFollowTarget(CameraName cameraId) => _cameras[cameraId].Camera.Follow;
        public Transform GetCameraLookAtTarget(CameraName cameraId) => _cameras[cameraId].Camera.LookAt;

        private void UpdateBlenderSettingsEntry(CinemachineBlenderSettings.CustomBlend[] settings, bool overrideBlends)
        {
            if (_sharedBlenderSettings?.CustomBlends != null && settings != null)
            {
                var currentBlends = _sharedBlenderSettings.CustomBlends.ToList();
                foreach (var blend in settings)
                {
                    if (overrideBlends)
                    {
                        var existingBlendIndex = currentBlends.FindIndex(b => b.From == blend.From && b.To == blend.To);
                        if (existingBlendIndex >= 0)
                        {
                            currentBlends[existingBlendIndex] = blend;
                        }
                        else
                        {
                            currentBlends.Add(blend);
                        }
                    }
                    else if (!currentBlends.Any(b => b.From == blend.From && b.To == blend.To))
                    {
                        currentBlends.Add(blend);
                    }
                }

                _sharedBlenderSettings.CustomBlends = currentBlends.ToArray();
            }
        }

        private void RemoveBlenderSettingsEntry(CameraName cameraId)
        {
            if (_cameras.TryGetValue(cameraId, out var config) &&
                config.BlenderSettings != null &&
                _sharedBlenderSettings?.CustomBlends != null)
            {
                var currentBlends = _sharedBlenderSettings.CustomBlends.ToList();

                foreach (var blend in config.BlenderSettings)
                {
                    currentBlends.RemoveAll(b => b.From == blend.From && b.To == blend.To);
                }

                _sharedBlenderSettings.CustomBlends = currentBlends.ToArray();
            }
        }
    }
}
