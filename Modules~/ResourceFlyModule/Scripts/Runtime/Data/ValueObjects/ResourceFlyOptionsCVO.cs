using System;
using System.Collections.Generic;
using Modules.ResourceFlyModule.Data.UnityObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>How flights look: the defaults every flight starts from, and the named looks a flight can pick.</summary>
    [Serializable]
    public class ResourceFlyOptionsCVO
    {
        [Tooltip("The motion a flight plays when its counter names none.")]
        public CD_ResourceFlyMotion Motion;

        [Min(1)] [Tooltip("The most icons one flight uses, when the counter names no limit of its own.")]
        public int MaxIcons = 10;

        [Min(0)]
        [Tooltip("How much one icon carries, when the counter names no value of its own: 100 flies 1000 in ten icons "
                 + "and 250 in three. 0: one icon per unit. Either way a flight uses no more than MaxIcons.")]
        public int UnitsPerIcon;

        [Min(0f)] [Tooltip("Seconds between one icon leaving the source and the next.")]
        public float StaggerSeconds = 0.04f;

        [Tooltip("How a counter answers an icon landing when it names no landing of its own. Empty: it only counts.")]
        public CD_ResourceFlyLanding Landing;

        [Min(0f)] [Tooltip("Seconds the count takes to reach the value a landing brings.")]
        public float CountUpSeconds = 0.3f;

        [Tooltip("Looks a flight picks by name - from its route, or a look passed from code. Each field left empty "
                 + "falls back to the counter, then to the values above.")]
        public List<ResourceFlyLookVO> Looks = new();
    }
}