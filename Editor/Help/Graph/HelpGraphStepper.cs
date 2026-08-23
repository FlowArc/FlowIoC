#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Graph
{
    /// <summary>
    /// Where the reader is in a diagram's walk. The page owns one of these, so the position
    /// survives between repaints without any static state.
    /// </summary>
    internal class HelpGraphStepper
    {
        public HelpGraphStepper(int stepCount)
        {
            Count = stepCount < 0 ? 0 : stepCount;
        }

        public int Index { get; private set; }
        public int Count { get; }

        public bool CanGoNext => Index < Count - 1;
        public bool CanGoPrevious => Index > 0;

        public void Next()
        {
            if (CanGoNext)
                Index++;
        }

        public void Previous()
        {
            if (CanGoPrevious)
                Index--;
        }

        public void Reset() => Index = 0;
    }
}

#endif
