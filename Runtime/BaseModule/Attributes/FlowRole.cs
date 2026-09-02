namespace FlowIoC.BaseModule.Attributes
{
    /// <summary>
    /// What a component is in FlowIoC's vocabulary. The inspector's header bar takes its colour
    /// from this, so the role is readable before the name is.
    /// </summary>
    public enum FlowRole
    {
        Root,
        Service,
        System,
        View,
        Mediator,
        Screen,
        Connector,
        Adapter,
        Test
    }
}
