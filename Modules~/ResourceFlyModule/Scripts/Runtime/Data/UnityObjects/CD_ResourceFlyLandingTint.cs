using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.UnityObjects
{
    /// <summary>The counter's icon takes one colour the moment an icon lands and fades back to its own, with no change of size.</summary>
    [CreateAssetMenu(fileName = "CD_ResourceFlyLanding_Tint", menuName = "FlowIoC/ResourceFlyModule/Data/CD_ResourceFlyLandingTint")]
    public class CD_ResourceFlyLandingTint : CD_ResourceFlyLanding
    {
        [Min(0f)] public float TintSeconds = 0.4f;

        [Tooltip("The colour the icon takes as an icon lands. Alpha is how much of it covers the icon.")]
        public Color TintColor = new(1f, 0.4f, 0.25f, 1f);

        [Tooltip("How much of the colour is left, 1 as the icon lands and 0 at rest.")]
        public AnimationCurve Fade = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        public override float Seconds => TintSeconds;

        public override ResourceFlyLandingVO Evaluate(float t)
        {
            Color tint = TintColor;
            tint.a *= Mathf.Clamp01(Fade.Evaluate(t));
            return new ResourceFlyLandingVO(1f, tint);
        }
    }
}
