using System;

namespace FlowIoC.BaseModule.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SubContextAttribute : Attribute
    {
        public Type ContextType;
        public bool AutoSetup;
    }
}