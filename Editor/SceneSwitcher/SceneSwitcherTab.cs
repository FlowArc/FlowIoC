#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER

namespace FlowIoC.Editor.SceneSwitcher
{
    /// <summary>The tabs of the scene switcher, in the order they are drawn.</summary>
    public enum SceneSwitcherTab
    {
        /// <summary>The scenes this developer opens most.</summary>
        Frequent = 0,

        /// <summary>Every scene, grouped under the module it belongs to.</summary>
        Modules = 1,

        /// <summary>The scenes screen modules' test modules run.</summary>
        Screen = 2,

        /// <summary>The scenes modules' test modules run.</summary>
        Test = 3
    }
}

#endif
