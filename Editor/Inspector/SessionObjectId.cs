#if UNITY_EDITOR
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// What names an object in a SessionState key: an id that holds while the Editor runs, domain
    /// reloads included, which is as long as SessionState keeps anything.
    ///
    /// Which id that is depends on the version. Unity 6.2 added GetEntityId, 6.4 deprecated
    /// GetInstanceID and 6.5 made calling it a compile error - and 6.0 and 6.1 have nothing else.
    /// </summary>
    internal class SessionObjectId
    {
        internal string For(Object target)
        {
#if UNITY_6000_2_OR_NEWER
            return target.GetEntityId().ToString();
#else
            return target.GetInstanceID().ToString();
#endif
        }
    }
}
#endif
