using System;

namespace FlowIoC.BaseModule.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class HideInModelViewerAttribute : Attribute
    {
        
    }
}