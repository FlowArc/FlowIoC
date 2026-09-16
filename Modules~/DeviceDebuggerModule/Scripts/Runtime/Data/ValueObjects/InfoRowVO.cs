namespace Modules.DeviceDebuggerModule.Data.ValueObjects
{
    public class InfoRowVO
    {
        public string Label = "";
        public string Value = "";

        public InfoRowVO()
        {
        }

        public InfoRowVO(string label, string value)
        {
            Label = label ?? "";
            Value = value ?? "";
        }
    }
}
