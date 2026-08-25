using UnityEngine;

namespace FlowIoC.ExtensionModule
{
    public static class AlgebraExtension
    {
        public static float Map(this float value, float fromSource, float toSource, float fromTarget, float toTarget) {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        public static float GetRandomSign(this float value)
        {
            return value * (Random.Range(0, 2) * 2 - 1);
        }
        
        public static float FixNaN(this float x)
        {
            return float.IsNaN(x) ? 0 : x;
        }
    }
}