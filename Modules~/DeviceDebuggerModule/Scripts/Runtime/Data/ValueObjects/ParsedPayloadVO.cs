namespace Modules.DeviceDebuggerModule.Data.ValueObjects
{
    /// <summary>What ParseSignalPayloadFunction answers: the boxed value, or why there is none.</summary>
    public class ParsedPayloadVO
    {
        public bool Ok;
        public object Value;
        public string Reason = "";

        public static ParsedPayloadVO Parsed(object value) => new() {Ok = true, Value = value};

        public static ParsedPayloadVO Failed(string reason) => new() {Ok = false, Reason = reason ?? ""};
    }
}
