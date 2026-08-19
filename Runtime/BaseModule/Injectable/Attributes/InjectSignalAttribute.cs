using System;

namespace FlowIoC.BaseModule.Injectable.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InjectSignalAttribute : Attribute
    {
        public string Name = "";

        public InjectSignalAttribute()
        {
            
        }

        public InjectSignalAttribute(string name)
        {
            Name = name;
        }
    }
} 