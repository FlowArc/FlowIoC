using System;

namespace FlowIoC.BaseModule.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExcludeFromContextWindowAttribute : Attribute
    {
    }
}