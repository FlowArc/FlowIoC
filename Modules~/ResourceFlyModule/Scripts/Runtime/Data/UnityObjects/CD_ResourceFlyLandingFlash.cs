using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.UnityObjects
{
    /// <summary>The counter's icon runs through a gradient and back to its own colour, with no change of size.</summary>
    [CreateAssetMenu(fileName = "CD_ResourceFlyLanding_Flash", menuName = "FlowIoC/ResourceFlyModule/Data/CD_ResourceFlyLandingFlash")]
    public class CD_ResourceFlyLandingFlash : CD_ResourceFlyLanding
    {
        [Min(0f)] public float FlashSeconds = 0.3f;

        [Tooltip("The colour over the landing, 0 as the icon lands and 1 at rest. Alpha is how much of it covers the icon: keep it 0 at both ends.")]
        public Gradient Colors = new()
        {
            colorKeys = new[] {new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f)},
            alphaKeys = new[] {new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f)}
        };

        public override float Seconds => FlashSeconds;

        public override ResourceFlyLandingVO Evaluate(float t) => new(1f, Colors.Evaluate(t));
    }
}
