using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.UnityObjects
{
    /// <summary>The icons fly straight from the source to the counter.</summary>
    [CreateAssetMenu(fileName = "CD_ResourceFlyMotion_Direct", menuName = "FlowIoC/ResourceFlyModule/Data/CD_ResourceFlyMotionDirect")]
    public class CD_ResourceFlyMotionDirect : CD_ResourceFlyMotion
    {
        [Min(0f)] public float FlySeconds = 0.6f;
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public override float Seconds => FlySeconds;

        public override Vector3 Evaluate(in ResourceFlyPathVO path, float t) =>
            Vector3.LerpUnclamped(path.From, path.To, Curve.Evaluate(t));
    }
}
