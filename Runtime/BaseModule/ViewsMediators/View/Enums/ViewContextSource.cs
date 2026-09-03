namespace FlowIoC.BaseModule.ViewsMediators.View.Enums
{
    /// <summary>
    /// How a view finds the Context it registers against. One value rather than a pair of
    /// toggles, because the three ways are alternatives: two booleans could say both at once and
    /// the inspector had to hide one of them to keep the answer single.
    ///
    /// A context named by the loader through <see cref="Injectable.Components.ViewInjector.AssignContext"/>
    /// outranks every value here - that is a screen's situation, and the object does not know it
    /// in the editor.
    /// </summary>
    public enum ViewContextSource
    {
        /// <summary>
        /// Walk up the hierarchy until a Root is found. What an ordinary view authored under its
        /// module's Root wants, and the default.
        /// </summary>
        BubbleUp = 0,

        /// <summary>
        /// Register against the Root named on the injector. A scene reference, so a prefab asset
        /// cannot hold one - use <see cref="RootName"/> there.
        /// </summary>
        SelectedRoot = 1,

        /// <summary>
        /// Register against the Root whose GameObject carries this name. The way a prefab reaches
        /// a Root it cannot reference, resolved when the object starts.
        /// </summary>
        RootName = 2
    }
}
