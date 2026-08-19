using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;

namespace FlowIoC.BaseModule.Injectable.CrossContext
{
    [HideInModelViewer]
    public class InjectionBinderCrossContext : InjectionBinder
    {
        internal readonly Stack<object> PostConstructedObjects = new();
    }
}