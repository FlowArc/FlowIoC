namespace Modules.WorldPointerModule.Enums
{
    /// <summary>What an indicator is told, on its first tick and then only when it changes.</summary>
    public enum WorldPointerState
    {
        /// <summary>Outside the frame or behind the camera in Hide mode, and once unregistered.</summary>
        Hidden = 0,

        /// <summary>Inside the frame.</summary>
        InFrame = 1,

        /// <summary>Outside the frame and pinned to its edge, in ClampToEdge mode.</summary>
        OnEdge = 2,
    }
}
