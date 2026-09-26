#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER

namespace FlowIoC.Editor.SceneSwitcher
{
    /// <summary>
    /// What a scene under the modules folder is there for, read off where it sits. The scene
    /// switcher sorts and badges by it, so the one scene the game runs does not drown among the
    /// test scenes every module brings.
    /// </summary>
    public enum SceneKind
    {
        /// <summary>A scene in a module's own Scenes folder - the game itself, MainScene.</summary>
        Game = 0,

        /// <summary>A device trial, in a sub module whose name ends in Check.</summary>
        Check = 1,

        /// <summary>The scene a module's test module runs.</summary>
        Test = 2,

        /// <summary>The scene a screen module's test module runs.</summary>
        ScreenTest = 3
    }
}

#endif
