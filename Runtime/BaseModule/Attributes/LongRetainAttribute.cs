using System;

namespace FlowIoC.BaseModule.Attributes
{
    /// <summary>
    /// A Command whose retained step may stay open longer than the ten seconds the watch over
    /// retained steps allows. With seconds, the watch warns only after that long; without, it never
    /// warns about the step and Play's end does not list it. Nothing about how the step runs
    /// changes: the step still ends when the Command calls Release() or Stop(), and only then.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LongRetainAttribute : Attribute
    {
        public double Seconds { get; }

        /// <summary>The step waits on something with no bound - the player's tap, a download.</summary>
        public LongRetainAttribute() => Seconds = double.PositiveInfinity;

        /// <summary>The step normally ends within <paramref name="seconds"/> - a thirty-second ad.</summary>
        public LongRetainAttribute(double seconds) => Seconds = seconds;
    }
}
