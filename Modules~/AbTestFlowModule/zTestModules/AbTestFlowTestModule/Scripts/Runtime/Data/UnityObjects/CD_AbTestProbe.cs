#if UNITY_EDITOR

using UnityEngine;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Data.UnityObjects
{
    /// <summary>
    /// A stand-in for a game's config asset: two numbers the scene can show, so an override is
    /// something a tester sees land. The original and each variant are assets of this type.
    /// </summary>
    public class CD_AbTestProbe : ScriptableObject
    {
        public int Lives = 3;
        public float Speed = 1f;
    }
}

#endif
