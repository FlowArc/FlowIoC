using System;

namespace FlowIoC.ConsoleModule
{
    [Flags]
    public enum FlowTextStyle
    {
        None = 0,
        Bold = 1 << 0,
        Italic = 1 << 1,
        Underline = 1 << 2
    }
}
