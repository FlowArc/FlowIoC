#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.ModelViewer
{
    /// <summary>
    /// One member of an object, or one child of a value, as the reader found it: what it is
    /// called, what it held when read, the type it was declared as, and - when reading it threw -
    /// the exception instead of a value.
    /// </summary>
    internal class ModelNodeEVO
    {
        public string Name;
        public object Value;
        public Type Type;
        public string Fault;
    }
}

#endif
