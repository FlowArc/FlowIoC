using UnityEngine;

namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>One icon's way to its counter as a motion reads it this frame.</summary>
    public readonly struct ResourceFlyPathVO
    {
        /// <summary>Where the source was when the icon launched.</summary>
        public readonly Vector3 From;

        /// <summary>Where the target is this frame, so a counter that moves is homed in on.</summary>
        public readonly Vector3 To;

        /// <summary>A point in the unit circle, fixed for the icon's whole flight.</summary>
        public readonly Vector2 Random;

        /// <summary>The icons' canvas scale, so distances authored in canvas units hold at any resolution.</summary>
        public readonly float Scale;

        public ResourceFlyPathVO(Vector3 from, Vector3 to, Vector2 random, float scale)
        {
            From = from;
            To = to;
            Random = random;
            Scale = scale;
        }
    }
}
