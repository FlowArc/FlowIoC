using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using Modules.CameraModule.Shared.Data.ValueObjects;
using Modules.CameraModule.Shared.Enums;
using UnityEngine;

namespace Modules.CameraModule.ViewsMediators
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
