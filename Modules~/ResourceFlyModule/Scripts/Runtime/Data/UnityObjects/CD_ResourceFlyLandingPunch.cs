using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.UnityObjects
{
    /// <summary>The counter's icon grows and settles back, once per landing.</summary>
    [CreateAssetMenu(fileName = "CD_ResourceFlyLanding_Punch", menuName = "FlowIoC/ResourceFlyModule/Data/CD_ResourceFlyLandingPunch")]
    public class CD_ResourceFlyLandingPunch : CD_ResourceFlyLanding
    {
        [Min(1f)] [Tooltip("How large the counter's icon gets as an icon lands.")]
        public float PunchScale = 1.25f;

        [Min(0f)] public float PunchSeconds = 0.15f;

        public override float Seconds => PunchSeconds;

        // Up and back down in one punch: 1 at both ends, the full scale in the middle.
        public override ResourceFlyLandingVO Evaluate(float t) =>
            new(Mathf.Lerp(1f, PunchScale, Mathf.Sin(t * Mathf.PI)), Color.clear);
    }
}
