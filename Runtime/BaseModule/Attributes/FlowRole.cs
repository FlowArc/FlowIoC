namespace FlowIoC.BaseModule.Attributes
{
    /// <summary>
    /// What a component is in FlowIoC's vocabulary. The inspector's header bar takes its colour
    /// from this, so the role is readable before the name is.
    /// </summary>
    public enum FlowRole
    {
        Root = 0,

        /// <summary>
        /// Part of the project's frame rather than of its game. A project holds exactly one, its
        /// Initialize Order is reserved rather than chosen, and a game extends it instead of
        /// authoring it - the way a screen module is listed on a Root, or a Connector sub-context
        /// is added to the Connector. A Core module carries no suffix, because there is one Main
        /// and one Screen in a project and the name has nothing to disambiguate from, so this is
        /// the one role a Root can only take from the attribute.
        /// </summary>
        Core = 1,
        Service = 2,
        System = 3,
        View = 4,
        Mediator = 5,
        Screen = 6,
        Connector = 7,
        Adapter = 8,
        Test = 9
    }
}
