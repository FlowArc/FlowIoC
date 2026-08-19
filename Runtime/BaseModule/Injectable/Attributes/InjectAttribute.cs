using System;

namespace FlowIoC.BaseModule.Injectable.Attributes
{
    [AttributeUsage(AttributeTargets.Property )]// | AttributeTargets.Field)]
    public class InjectAttribute : Attribute
    {
        public string Name = "";

        public InjectAttribute()
        {
            
        }

        public InjectAttribute(string name)
        {
            Name = name;
        }
    }
}