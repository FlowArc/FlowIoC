using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.UnityObjects
{
    /// <summary>The icons burst out around the source, then gather into the counter.</summary>
    [CreateAssetMenu(fileName = "CD_ResourceFlyMotion_Scatter", menuName = "FlowIoC/ResourceFlyModule/Data/CD_ResourceFlyMotionScatter")]
    public class CD_ResourceFlyMotionScatter : CD_ResourceFlyMotion
    {
        [Min(0f)] [Tooltip("Canvas units around the source an icon scatters to before it heads for the counter.")]
        public float ScatterRadius = 140f;

        [Min(0f)] public float ScatterSeconds = 0.3f;
        public AnimationCurve ScatterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Min(0f)] public float GatherSeconds = 0.45f;
        public AnimationCurve GatherCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public override float Seconds => ScatterSeconds + GatherSeconds;

        public override Vector3 Evaluate(in ResourceFlyPathVO path, float t)
        {
            Vector3 scatterPoint = path.From + (Vector3) (path.Random * ScatterRadius * path.Scale);
            float split = Seconds <= 0f ? 0f : ScatterSeconds / Seconds;

            if (t < split)
                return Vector3.LerpUnclamped(path.From, scatterPoint, ScatterCurve.Evaluate(t / split));

            float gather = split >= 1f ? 1f : (t - split) / (1f - split);
            return Vector3.LerpUnclamped(scatterPoint, path.To, GatherCurve.Evaluate(gather));
        }
    }
}
