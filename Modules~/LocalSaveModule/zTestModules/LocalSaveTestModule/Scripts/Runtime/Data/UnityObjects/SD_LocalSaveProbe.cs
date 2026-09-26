#if UNITY_EDITOR

using UnityEngine;

namespace Modules.LocalSaveModule.LocalSaveTestModule.Data.UnityObjects
{
    /// <summary>
    /// One number, so the scene can show something that survives a restart and nothing else.
    /// SD_ because it is loaded at startup and saved again whenever it changes.
    /// </summary>
    public class SD_LocalSaveProbe : ScriptableObject
    {
        public int Counter;
    }
}

#endif
