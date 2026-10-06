#if UNITY_EDITOR
using FlowIoC.BaseModule.Root;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Whether a Root's lifecycle moved since the last look, so its inspector repaints when a badge
    /// has something new to say and not otherwise.
    ///
    /// The inspector used to ask Unity for a repaint on every frame of play. That redraws the whole
    /// Inspector window - every component on the Root's GameObject, an Odin-drawn adapter with its
    /// lists included - for five flags that change five times in a session, and with a Root
    /// selected the game lost frames to it.
    /// </summary>
    internal class RootLifecycleWatch
    {
        private int _last = -1;

        internal bool Moved(RootBase root)
        {
            if (root == null)
                return false;

            int now = Phases(root);
            bool moved = now != _last;
            _last = now;

            return moved;
        }

        private int Phases(RootBase root)
        {
            return (root.injectionsBound ? 1 : 0)
                   | (root.mediationsBound ? 2 : 0)
                   | (root.hasInitialized ? 4 : 0)
                   | (root.hasSetUp ? 8 : 0)
                   | (root.hasLaunched ? 16 : 0);
        }
    }
}
#endif
