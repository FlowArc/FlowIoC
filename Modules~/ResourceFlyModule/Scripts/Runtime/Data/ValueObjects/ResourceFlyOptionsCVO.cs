using System;
using Modules.ResourceFlyModule.Data.UnityObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>How a flight looks: how many icons, which motion when the counter names none, how the counter answers.</summary>
    [Serializable]
    public class ResourceFlyOptionsCVO
    {
        [Tooltip("The motion a flight plays when its counter names none.")]
        public CD_ResourceFlyMotion Motion;

        [Min(1)] [Tooltip("The most icons one flight uses; an amount smaller than this flies one icon per unit.")]
        public int MaxIcons = 10;

        [Min(0f)] [Tooltip("Seconds between one icon leaving the source and the next.")]
        public float StaggerSeconds = 0.04f;

        [Min(1f)] [Tooltip("How large the counter's icon gets as an icon lands.")]
        public float PunchScale = 1.25f;

        [Min(0f)] public float PunchSeconds = 0.15f;

        [Min(0f)] [Tooltip("Seconds the count takes to reach the value a landing brings.")]
        public float CountUpSeconds = 0.3f;
    }
}