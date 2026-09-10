namespace Modules.WorldPointerModule.Enums
{
    /// <summary>What a pointer does once its target is outside the frame or behind the camera.</summary>
    public enum OffScreenMode
    {
        /// <summary>Hidden outside the frame or behind the camera; placed only while in the frame.</summary>
        Hide = 0,

        /// <summary>
        /// Never hidden. Outside the frame it sits on the frame's edge, on the ray from the frame's
        /// centre towards the target, and is told OnEdge.
        /// </summary>
        ClampToEdge = 1,

        /// <summary>Placed wherever the projection lands; told InFrame once and never again.</summary>
        Ignore = 2,
    }
}
