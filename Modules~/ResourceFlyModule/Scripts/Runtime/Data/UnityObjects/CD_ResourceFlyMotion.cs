using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.UnityObjects
{
    /// <summary>
    /// How an icon travels from the source to the counter. It only says where the icon is at a
    /// moment; the icon plays it and reports landed or lost, so a motion of the game's own cannot
    /// leave a flight unfinished. Derive from it for a motion of your own, make an asset, and put
    /// it in CD_ResourceFly or on a counter.
    /// </summary>
    public abstract class CD_ResourceFlyMotion : ScriptableObject
    {
        /// <summary>Seconds one icon takes from leaving the source to landing.</summary>
        public abstract float Seconds { get; }

        /// <summary>Where the icon is at t, 0 at the source and 1 on the target. Keep no state: it is asked every frame.</summary>
        public abstract Vector3 Evaluate(in ResourceFlyPathVO path, float t);
    }
}
