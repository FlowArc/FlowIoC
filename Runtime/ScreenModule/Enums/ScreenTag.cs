namespace FlowIoC.ScreenModule.Enums
{
    /// <summary>
    /// Groups screens for bulk load, hide and unload. Numbered because a Root serialises the tag
    /// it overrides a screen with, and an unnumbered value moves when one is added above it.
    /// </summary>
    public enum ScreenTag
    {
        Default = 0,
        GroupA = 1,
        GroupB = 2,
        GroupC = 3,
        GroupD = 4,
        GroupE = 5,
        GroupF = 6,
        GroupG = 7,
        GroupH = 8
    }
}
