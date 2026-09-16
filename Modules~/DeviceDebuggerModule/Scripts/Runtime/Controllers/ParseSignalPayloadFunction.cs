using System;
using System.Globalization;
using FlowIoC.BaseModule.Function.ReturnableFunctions;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Entities;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// Text typed on the panel into the boxed value a signal's payload type wants, invariant
    /// culture, or the reason it cannot. A bool takes true/false, 1/0 and on/off in any case; an
    /// enum takes a name in any case.
    /// </summary>
    internal class ParseSignalPayloadFunction : FunctionReturn<ParsedPayloadVO, Type, string>
    {
        private readonly PayloadTypeRule _rule = new();

        public override ParsedPayloadVO Execute(Type type, string text)
        {
            if (!_rule.IsSupported(type))
                return ParsedPayloadVO.Failed("payload " + (type == null ? "null" : type.Name) + " cannot be typed on the panel");

            text = text?.Trim() ?? "";

            if (type == typeof(string)) return ParsedPayloadVO.Parsed(text);

            if (type == typeof(bool))
            {
                switch (text.ToLowerInvariant())
                {
                    case "true":
                    case "1":
                    case "on":
                    case "yes":
                        return ParsedPayloadVO.Parsed(true);
                    case "false":
                    case "0":
                    case "off":
                    case "no":
                        return ParsedPayloadVO.Parsed(false);
                    default:
                        return ParsedPayloadVO.Failed("'" + text + "' is not a Boolean");
                }
            }

            if (type == typeof(int))
            {
                return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)
                    ? ParsedPayloadVO.Parsed(i)
                    : ParsedPayloadVO.Failed("'" + text + "' is not an Int32");
            }

            if (type == typeof(float))
            {
                return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)
                    ? ParsedPayloadVO.Parsed(f)
                    : ParsedPayloadVO.Failed("'" + text + "' is not a Single");
            }

            if (type == typeof(double))
            {
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                    ? ParsedPayloadVO.Parsed(d)
                    : ParsedPayloadVO.Failed("'" + text + "' is not a Double");
            }

            try
            {
                return ParsedPayloadVO.Parsed(Enum.Parse(type, text, true));
            }
            catch (ArgumentException)
            {
                return ParsedPayloadVO.Failed("'" + text + "' is not a " + type.Name + ": " + string.Join(", ", Enum.GetNames(type)));
            }
        }
    }
}
