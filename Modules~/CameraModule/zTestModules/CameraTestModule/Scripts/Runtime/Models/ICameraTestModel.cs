#if UNITY_EDITOR

using Modules.CameraModule.Shared.Enums;

namespace Modules.CameraModule.CameraTestModule.Models
{
    /// <summary>Which of the scene's two cameras is live, how far a move may wander from its cube, and how far a zoom may go.</summary>
    internal interface ICameraTestModel
    {
        CameraName Live { get; }
        float MoveRadius { get; }
        float MoveSeconds { get; }
        float MinDistance { get; }
        float MaxDistance { get; }
        void SetLive(CameraName camera);
    }
}

#endif
