using System;

namespace Modules.DeviceDebuggerModule.Entities
{
    /// <summary>
    /// Which payload types the panel can take from a field: the primitives a tester types, and
    /// an enum picked from a list. The parser and both scans ask the same rule, so a row that
    /// says it can fire is one the parser will answer.
    /// </summary>
    public class PayloadTypeRule
    {
        public bool IsSupported(Type type)
        {
            if (type == null) return false;

            return type == typeof(bool)
                   || type == typeof(int)
                   || type == typeof(float)
                   || type == typeof(double)
                   || type == typeof(string)
                   || type.IsEnum;
        }

        public bool IsNumber(Type type) =>
            type == typeof(int) || type == typeof(float) || type == typeof(double);
    }
}
