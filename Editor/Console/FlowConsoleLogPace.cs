#if UNITY_EDITOR

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// How often arriving logs may repaint the console: at most ten times a second.
    ///
    /// Every repaint after a log arrives refilters the whole list, so a flow that logs on every
    /// frame used to rebuild and redraw the console on every frame - about 3 ms with a few hundred
    /// logs and 10 ms with five thousand, taken out of the game's frame while it played. A reader
    /// cannot follow a list scrolling faster than this, and the first log after a quiet spell is
    /// still shown at once: only the ones behind it wait for the next slot.
    ///
    /// Only arrivals are paced. A click, a key or a switch in the filters repaints as it always
    /// did.
    /// </summary>
    internal class FlowConsoleLogPace
    {
        internal const double Interval = 0.1;

        private double _last = double.NegativeInfinity;

        /// <summary>Whether a repaint for arrived logs may happen now; true also takes the slot.</summary>
        internal bool Due(double now)
        {
            if (now - _last < Interval)
                return false;

            _last = now;
            return true;
        }
    }
}

#endif
