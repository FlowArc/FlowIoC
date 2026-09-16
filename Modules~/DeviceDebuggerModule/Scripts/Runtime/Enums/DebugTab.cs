namespace Modules.DeviceDebuggerModule.Enums
{
    /// <summary>
    /// The panel's pages. Last is not a page: it is what Show is handed when the caller wants
    /// whichever page was open before, or the Console when an error is waiting to be read.
    /// </summary>
    public enum DebugTab
    {
        Last = 0,
        Console = 1,
        Options = 2,
        Signals = 3,
        Stats = 4,
        Info = 5
    }
}
