using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.UnityObjects
{
    /// <summary>Every icon arcs to the counter along a curve of its own, bending to a random side.</summary>
    [CreateAssetMenu(fileName = "CD_ResourceFlyMotion_Curved", menuName = "FlowIoC/ResourceFlyModule/Data/CD_ResourceFlyMotionCurved")]
    public class CD_ResourceFlyMotionCurved : CD_ResourceFlyMotion
    {
        [Min(0f)] public float FlySeconds = 0.6f;
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Range(0f, 1f)] [Tooltip("How far the arc swings out, as a share of the distance flown.")]
        public float Bend = 0.35f;

        public override float Seconds => FlySeconds;

        public override Vector3 Evaluate(in ResourceFlyPathVO path, float t)
        {
            Vector3 line = path.To - path.From;
            // The perpendicular in the screen plane, as long as the line itself.
            var across = new Vector3(-line.y, line.x, 0f);
            float side = path.RandomValue(0) < 0.5f ? -1f : 1f;
            // Never a straight line: the swing is at least half the Bend.
            float swing = Bend * Mathf.Lerp(0.5f, 1f, path.RandomValue(1));
            Vector3 control = (path.From + path.To) * 0.5f + across * (side * swing);

            float k = Curve.Evaluate(t);
            float m = 1f - k;
            return m * m * path.From + 2f * m * k * control + k * k * path.To;
        }
    }
}