#if UNITY_EDITOR

using FlowIoC.BaseModule.Attributes;
using Modules.CameraModule.Shared.Enums;

namespace Modules.CameraModule.CameraTestModule.Models
{
    /// <summary>
    /// The scene's state. Menu is camera A, on cube A, which the shipped Root brings live; Gameplay
    /// is camera B, on cube B.
    /// </summary>
    internal class CameraTestModel : ICameraTestModel
    {
        [ShowInModelViewer] public CameraName Live { get; private set; } = CameraName.Menu;

        public float MoveRadius => 3f;

        public float MoveSeconds => 0.6f;

        public float MinDistance => 5f;

        public float MaxDistance => 18f;

        public void SetLive(CameraName camera) => Live = camera;
    }
}

#endif
