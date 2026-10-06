using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.CameraModule.Data.ValueObjects;
using Modules.CameraModule.Shared.Data.ValueObjects;
using Modules.CameraModule.RootsContexts;
using Modules.CameraModule.Shared.Enums;
using Unity.Cinemachine;
using UnityEngine;

namespace Modules.CameraModule.Models.Runtime
{
    internal class CameraModel : ICameraModel, IConstructable
    {
        [Inject(nameof(CameraServiceContext))] private GameObject _root { get; set; }
        [ShowInModelViewer] private readonly Dictionary<CameraName, CameraVO> _cameras = new();
        [ShowInModelViewer] private CinemachineCamera _activeCamera;
        [ShowInModelViewer] private CinemachineBlenderSettings _sharedBlenderSettings;
        private const int ACTIVE_PRIORITY = 10;
        private const int INACTIVE_PRIORITY = 0;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public void PostConstruct()
        {
            var adapter = _root.GetComponent<RootAdapter>();
            _sharedBlenderSettings = adapter.GetScriptable<CinemachineBlenderSettings>("CD_CameraCustomBlends");
        }

        public bool TryGetCameraLastPos(CameraName type, out Vector3 pos)
        {
            bool has = _cameras.TryGetValue(type, out CameraVO record) && record.HasLastPosition;
            pos = has ? record.LastPosition : Vector3.zero;
            return has;
        }

        public void SetCameraLastPos(CameraName type, Vector3 pos)
        {
            CameraVO record = GetOrCreate(type);
            record.LastPosition = pos;
            record.HasLastPosition = true;
        }

        public void RegisterCamera(CameraName cameraId, CameraCVO config)
        {
            CameraVO record = GetOrCreate(cameraId);
            record.Config = config;
            record.IsRegistered = true;

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

        /// <summary>The config goes; the record stays, so the camera's last position is there when it is registered again.</summary>
        public void UnregisterCamera(CameraName cameraId)
        {
            if (!_cameras.TryGetValue(cameraId, out CameraVO record) || !record.IsRegistered)
                return;

            if (_sharedBlenderSettings != null)
                RemoveBlenderSettingsEntry(record.Config);

            record.Config = default;
            record.IsRegistered = false;
        }

        public bool TryGetCamera(CameraName cameraId, out CameraCVO config)
        {
            bool registered = _cameras.TryGetValue(cameraId, out CameraVO record) && record.IsRegistered;
            config = registered ? record.Config : default;
            return registered;
        }

        public void SetActiveCamera(CameraName cameraId)
        {
            if (TryGetCamera(cameraId, out CameraCVO config))
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
                FlowLogger.LogError($"No active camera {cameraId}");
            }
        }

        public CinemachineCamera GetActiveCamera() => _activeCamera;
        public Transform GetCameraFollowTarget(CameraName cameraId) => _cameras[cameraId].Config.Camera.Follow;
        public Transform GetCameraLookAtTarget(CameraName cameraId) => _cameras[cameraId].Config.Camera.LookAt;

        private CameraVO GetOrCreate(CameraName cameraId)
        {
            if (!_cameras.TryGetValue(cameraId, out CameraVO record))
            {
                record = new CameraVO();
                _cameras[cameraId] = record;
            }

            return record;
        }

        /// <summary>
        /// Adds the camera's blends to the shared asset, one pass over what is there and one over
        /// what is added: a blend already in the asset for the same pair is replaced only when the
        /// camera overrides blends.
        /// </summary>
        private void UpdateBlenderSettingsEntry(CinemachineBlenderSettings.CustomBlend[] settings, bool overrideBlends)
        {
            if (_sharedBlenderSettings?.CustomBlends == null || settings == null)
                return;

            var currentBlends = new List<CinemachineBlenderSettings.CustomBlend>(_sharedBlenderSettings.CustomBlends);
            var indexByPair = new Dictionary<(string, string), int>();

            for (int i = 0; i < currentBlends.Count; i++)
                indexByPair.TryAdd((currentBlends[i].From, currentBlends[i].To), i);

            foreach (var blend in settings)
            {
                if (indexByPair.TryGetValue((blend.From, blend.To), out int existing))
                {
                    if (overrideBlends)
                        currentBlends[existing] = blend;

                    continue;
                }

                indexByPair[(blend.From, blend.To)] = currentBlends.Count;
                currentBlends.Add(blend);
            }

            _sharedBlenderSettings.CustomBlends = currentBlends.ToArray();
        }

        private void RemoveBlenderSettingsEntry(CameraCVO config)
        {
            if (config.BlenderSettings == null || _sharedBlenderSettings?.CustomBlends == null)
                return;

            var pairs = new HashSet<(string, string)>();

            foreach (var blend in config.BlenderSettings)
                pairs.Add((blend.From, blend.To));

            var currentBlends = new List<CinemachineBlenderSettings.CustomBlend>(_sharedBlenderSettings.CustomBlends);
            currentBlends.RemoveAll(b => pairs.Contains((b.From, b.To)));
            _sharedBlenderSettings.CustomBlends = currentBlends.ToArray();
        }
    }
}