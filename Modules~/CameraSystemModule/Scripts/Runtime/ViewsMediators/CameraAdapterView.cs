using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using Modules.CameraSystemModule.Shared.Data.ValueObjects;
using Modules.CameraSystemModule.Shared.Enums;
using UnityEngine;
using UnityEngine.Rendering;

namespace Modules.CameraSystemModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public class CameraAdapterView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private SerializedDictionary<CameraName, CameraCVO> _cameraConfigs = new();
        [SerializeField] private Transform _cameraTarget;

        public Action<SerializedDictionary<CameraName, CameraCVO>> OnUnregisterCameras;

        public SerializedDictionary<CameraName, CameraCVO> GetCameraConfigs() => _cameraConfigs;

        public Transform GetCameraTarget() => _cameraTarget;

        private void OnDestroy()
        {
            OnUnregisterCameras?.Invoke(_cameraConfigs);
        }
    }
}
