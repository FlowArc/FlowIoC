using System.Globalization;
using Modules.AnalyticsModule.Enums;

namespace Modules.AnalyticsModule.Data.ValueObjects
{
    /// <summary>One named value on an event. Four kinds, one constructor each; the SDK decides how each kind travels.</summary>
    public readonly struct AnalyticsParameterVO
    {
        public readonly string Name;
        public readonly AnalyticsParameterKind Kind;
        public readonly string StringValue;
        public readonly long LongValue;
        public readonly double DoubleValue;
        public readonly bool BoolValue;

        public AnalyticsParameterVO(string name, string value) : this(name, AnalyticsParameterKind.String, value, 0, 0, false)
        {
        }

        public AnalyticsParameterVO(string name, long value) : this(name, AnalyticsParameterKind.Long, null, value, 0, false)
        {
        }

        public AnalyticsParameterVO(string name, double value) : this(name, AnalyticsParameterKind.Double, null, 0, value, false)
        {
        }

        public AnalyticsParameterVO(string name, bool value) : this(name, AnalyticsParameterKind.Bool, null, 0, 0, value)
        {
        }

        private AnalyticsParameterVO(string name, AnalyticsParameterKind kind, string stringValue, long longValue, double doubleValue, bool boolValue)
        {
            Name = name;
            Kind = kind;
            StringValue = stringValue;
            LongValue = longValue;
            DoubleValue = doubleValue;
            BoolValue = boolValue;
        }

        /// <summary>The value as the console shows it.</summary>
        public string ValueText => Kind switch
        {
            AnalyticsParameterKind.Long => LongValue.ToString(CultureInfo.InvariantCulture),
            AnalyticsParameterKind.Double => DoubleValue.ToString(CultureInfo.InvariantCulture),
            AnalyticsParameterKind.Bool => BoolValue ? "true" : "false",
            _ => StringValue ?? string.Empty
        };

        /// <summary>The same value under another name; what the trimmer builds.</summary>
        public AnalyticsParameterVO Renamed(string name) => new(name, Kind, StringValue, LongValue, DoubleValue, BoolValue);

        /// <summary>The same parameter with a shorter string; what the trimmer builds.</summary>
        public AnalyticsParameterVO Shortened(string stringValue) => new(Name, Kind, stringValue, LongValue, DoubleValue, BoolValue);
    }
}
