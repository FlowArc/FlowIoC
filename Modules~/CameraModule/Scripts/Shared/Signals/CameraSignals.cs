using FlowIoC.BaseModule.Signals;
using Modules.CameraModule.Shared.Data.ValueObjects;
using Modules.CameraModule.Shared.Enums;
using UnityEngine;

namespace Modules.CameraModule.Shared.Signals
{
    public class CameraSignals : ISignalHolder
    {
        public CameraSignalsIncoming Incoming = new();
        public CameraSignalsOutgoing Outgoing = new();

        public class CameraSignalsIncoming
        {
            public Signal<CameraName, CameraCVO> RegisterCamera = new();
            public Signal<CameraName> UnregisterCamera = new();

            public Signal<CameraName> SwitchCamera = new();
            public Signal<Transform> SetCameraTarget = new();
            public Signal<Vector3, float> MoveCamera = new();
            public Signal<int, float> SetCameraDistance = new();
            public Signal<CameraName> SetCameraLastPos = new();
            public Signal<CameraName, float> MoveCameraToLastPos = new();
            public Signal PublishCameraTarget = new();
        }

        public class CameraSignalsOutgoing
        {
            public Signal<Transform> CameraTargetReady = new();
        }
    }
}
