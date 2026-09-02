#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Where a type's source text comes from. Behind an interface so the help parsing can be
    /// tested without an AssetDatabase, and so a type whose source is not in the project answers
    /// null instead of throwing.
    /// </summary>
    internal interface IFlowScriptText
    {
        string Read(Type type);
    }
}

#endif
