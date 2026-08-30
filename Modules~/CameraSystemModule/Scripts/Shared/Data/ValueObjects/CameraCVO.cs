using System;
using Unity.Cinemachine;

namespace Modules.CameraSystemModule.Shared.Data.ValueObjects
{
    [Serializable]
    public struct CameraCVO
    {
        public CinemachineCamera Camera;
        public bool OverrideBlends;
        public bool ActivateAtRegister;
        public CinemachineBlenderSettings.CustomBlend[] BlenderSettings;

        public CameraCVO(CinemachineCamera camera, CinemachineBlenderSettings.CustomBlend[] blenderSettings = null)
        {
            Camera = camera;
            OverrideBlends = true;
            ActivateAtRegister = false;
            BlenderSettings = blenderSettings ?? new CinemachineBlenderSettings.CustomBlend[0];
        }
    }
}
