using Modules.CameraModule.Shared.Data.ValueObjects;
using Modules.CameraModule.Shared.Enums;
using Unity.Cinemachine;
using UnityEngine;

namespace Modules.CameraModule.Models.Runtime
{
    internal interface ICameraModel
    {
        bool TryGetCameraLastPos(CameraName type, out Vector3 pos);
        void SetCameraLastPos(CameraName type, Vector3 pos);
        void RegisterCamera(CameraName cameraId, CameraCVO config);
        void UnregisterCamera(CameraName cameraId);
        bool TryGetCamera(CameraName cameraId, out CameraCVO config);
        void SetActiveCamera(CameraName cameraId);
        CinemachineCamera GetActiveCamera();
        Transform GetCameraFollowTarget(CameraName cameraId);
        Transform GetCameraLookAtTarget(CameraName cameraId);
    }
}
