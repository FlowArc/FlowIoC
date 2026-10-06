using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.UnityObjects
{
    /// <summary>
    /// How the counter answers an icon landing on it. It only says how the counter's icon looks at
    /// a moment; the counter plays it and returns the icon to rest when it is over, so a landing of
    /// the game's own cannot leave the icon changed. Derive from it for a landing of your own, make
    /// an asset, and put it in CD_ResourceFly or on a counter.
    /// </summary>
    public abstract class CD_ResourceFlyLanding : ScriptableObject
    {
        /// <summary>Seconds one landing plays.</summary>
        public abstract float Seconds { get; }

        /// <summary>How the icon looks at t, 0 as the icon lands and 1 back at rest. Keep no state: it is asked every frame.</summary>
        public abstract ResourceFlyLandingVO Evaluate(float t);
    }
}
