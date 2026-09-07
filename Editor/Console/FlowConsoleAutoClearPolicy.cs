#if UNITY_EDITOR
namespace FlowIoC.Editor.Console
{
    /// <summary>What happened that might mean the console should empty itself.</summary>
    public enum FlowConsoleClearTrigger
    {
        EnteringPlayMode = 0,
        CompilationStarted = 1,
        BuildStarted = 2
    }

    public class FlowConsoleAutoClearPolicy
    {
        public bool ShouldClear(FlowConsoleClearTrigger trigger, bool clearOnPlay,
            bool clearOnRecompile, bool clearOnBuild)
        {
            switch (trigger)
            {
                case FlowConsoleClearTrigger.EnteringPlayMode: return clearOnPlay;
                case FlowConsoleClearTrigger.CompilationStarted: return clearOnRecompile;
                case FlowConsoleClearTrigger.BuildStarted: return clearOnBuild;
                default: return false;
            }
        }
    }
}
#endif
