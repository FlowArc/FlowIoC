using UnityEngine;

namespace FlowIoC.BaseModule
{
    /// <summary>
    /// The objects of a type in the loaded scenes, found the way the running Unity version wants
    /// them found. Unity 6.4 deprecated the FindObjectsByType overloads that take a sort mode and
    /// added ones without it; 6.0 to 6.3 have only the old ones. FlowIoC always asked for no sort,
    /// which is what the new overloads do, so what comes back is the same either way - and the
    /// version switch lives here, once, rather than beside every search.
    /// </summary>
    internal class SceneObjects
    {
        internal T[] All<T>(FindObjectsInactive inactive = FindObjectsInactive.Exclude) where T : Object
        {
#if UNITY_6000_4_OR_NEWER
            return Object.FindObjectsByType<T>(inactive);
#else
            return Object.FindObjectsByType<T>(inactive, FindObjectsSortMode.None);
#endif
        }
    }
}
