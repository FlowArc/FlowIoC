namespace Modules.DeviceDebuggerModule.Enums
{
    /// <summary>Which half of a holder a signal field sits in. Other is a nested object the scans skip.</summary>
    public enum SignalHalf
    {
        Incoming = 0,
        Outgoing = 1,
        Other = 2
    }
}
