using System;

namespace FlowIoC.BaseModule.Injectable.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SignalParamAttribute : Attribute
    {
        /// <summary>
        /// Which value of this property's own type to take from the signal payload,
        /// counting from zero. Only meaningful when <see cref="HasIndex"/> is true.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// False for <c>[SignalParam]</c>, which takes the next value of its type that
        /// no other property has claimed. True for <c>[SignalParam(n)]</c>, which takes
        /// the n-th value of its type whether or not anything else wanted it.
        /// </summary>
        public bool HasIndex { get; }

        public SignalParamAttribute()
        {
            Index = 0;
            HasIndex = false;
        }

        public SignalParamAttribute(int index)
        {
            Index = index;
            HasIndex = true;
        }
    }
}