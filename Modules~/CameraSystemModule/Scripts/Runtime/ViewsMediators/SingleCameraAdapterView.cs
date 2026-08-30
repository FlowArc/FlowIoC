using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using Modules.CameraSystemModule.Data.ValueObjects;
using Modules.CameraSystemModule.Shared.Enums;
using UnityEngine;

namespace Modules.CameraSystemModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public class SingleCameraAdapterView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        public CameraName CameraKey;
        public CameraCVO CameraConfig = new();

        public Action OnUnregisterCamera;

        private void OnDestroy()
        {
            OnUnregisterCamera?.Invoke();
        }
    }
}
