namespace Modules.DeviceDebuggerModule.Enums
{
    /// <summary>
    /// What an option row is drawn as, decided from the annotated field's payload type or from the
    /// step it names. Value is an Outgoing signal's last payload; Unsupported is drawn disabled
    /// with the reason.
    /// </summary>
    public enum DebugOptionKind
    {
        Button = 0,
        Toggle = 1,
        Number = 2,
        Text = 3,
        Choice = 4,
        Value = 5,
        Unsupported = 6
    }
}
