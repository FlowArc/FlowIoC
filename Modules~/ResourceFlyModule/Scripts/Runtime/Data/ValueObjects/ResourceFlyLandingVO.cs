using UnityEngine;

namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>How the counter's icon looks at one moment of a landing.</summary>
    public readonly struct ResourceFlyLandingVO
    {
        /// <summary>A multiple of the icon's own size: 1 leaves it alone.</summary>
        public readonly float Scale;

        /// <summary>The colour laid over the icon, its alpha how much of it: alpha 0 leaves the colour alone.</summary>
        public readonly Color Tint;

        public ResourceFlyLandingVO(float scale, Color tint)
        {
            Scale = scale;
            Tint = tint;
        }

        /// <summary>The icon as it is at rest.</summary>
        public static ResourceFlyLandingVO Rest => new(1f, Color.clear);
    }
}
