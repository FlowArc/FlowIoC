using System;

namespace FlowIoC.ScreenModule.Enums
{
    [Flags]
    public enum ScreenState
    {
        None =                  0,
        Loading =               1 << 0,
        Unloading =             1 << 1,
        InPool =                1 << 2,
        InUse =                 1 << 3,
        InShowAnimation =       1 << 4,
        InHideAnimation =       1 << 5,
        
        AvailableToSendSignal = InUse,
    }
}