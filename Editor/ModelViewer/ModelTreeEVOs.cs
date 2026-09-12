#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root;

namespace FlowIoC.Editor.ModelViewer
{
    /// <summary>
    /// What an object a module bound is, read off its type name. The badge on its row; also the
    /// order the rows come in, so a module's models sit above its services.
    /// </summary>
    internal enum ModelKind
    {
        Model = 0,
        Service = 1,
        System = 2,
        SubService = 3,
        SubSystem = 4,
        Other = 5
    }

    /// <summary>One object a context bound: the instance, the type it was bound as, its binding name, its kind.</summary>
    internal class ModelObjectEVO
    {
        public object Value;
        public Type Key;
        public string Name;
        public ModelKind Kind;
    }

    /// <summary>A sub context with something to list.</summary>
    internal class ModelContextEVO
    {
        public IContext Context;
        public List<ModelObjectEVO> Objects = new List<ModelObjectEVO>();
    }

    /// <summary>One Root: its role for the colour, the main context's objects, and the sub contexts that list something.</summary>
    internal class ModelRootEVO
    {
        public RootBase Root;
        public FlowRole Role;
        public List<ModelObjectEVO> Objects = new List<ModelObjectEVO>();
        public List<ModelContextEVO> SubContexts = new List<ModelContextEVO>();
    }
}

#endif
