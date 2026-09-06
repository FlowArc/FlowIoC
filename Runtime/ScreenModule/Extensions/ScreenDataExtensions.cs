using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.ScreenModule.Extensions
{
    /// <summary>
    /// A screen's state is a set of flags rather than one value, because a screen can be in use and
    /// animating at the same time. These read and write that set.
    /// </summary>
    public static class ScreenDataExtensions
    {
        /// <summary>Adds a state, leaving the others as they are.</summary>
        public static void AddState(this ScreenVO data, ScreenState value)
        {
            data.State |= value;
        }

        /// <summary>Takes a state away, leaving the others as they are.</summary>
        public static void RemoveState(this ScreenVO data, ScreenState value)
        {
            data.State &= ~value;
        }

        /// <summary>
        /// Whether the state is set. Tested with a mask rather than <c>Enum.HasFlag</c>, which boxes
        /// both sides on Unity's runtimes - and a screen's Mediator asks this on every handler it
        /// has, which is the guard that keeps a tap during an animation from becoming a signal.
        /// </summary>
        public static bool HasState(this ScreenVO data, ScreenState value)
        {
            return (data.State & value) == value;
        }

        /// <summary>Adds the state if it is missing and takes it away if it is there.</summary>
        public static void ToggleState(this ScreenVO data, ScreenState value)
        {
            data.State ^= value;
        }
    }
}