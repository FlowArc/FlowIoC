using System.Collections.Generic;
using System.Text.RegularExpressions;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Enums;

namespace Modules.AnalyticsModule.Services
{
    /// <summary>
    /// An event cut to what an SDK accepts: whitespace out of the name, the name and each
    /// parameter name to their lengths, each string value to its length, parameters past the
    /// count dropped. The report names every cut so the provider can warn and the developer
    /// can fix the name; it is empty when nothing changed, and then the same event comes back.
    /// </summary>
    public class AnalyticsEventTrimmer
    {
        private readonly Regex _whitespace = new(@"\s+");

        public AnalyticsEventVO Trim(AnalyticsEventVO analyticsEvent, AnalyticsLimitsVO limits, out string report)
        {
            var changes = new List<string>();

            string name = _whitespace.Replace(analyticsEvent.Name ?? string.Empty, string.Empty);
            if (name != analyticsEvent.Name)
                changes.Add("whitespace removed from the name");

            if (name.Length > limits.NameLength)
            {
                name = name.Substring(0, limits.NameLength);
                changes.Add($"name cut to {limits.NameLength}");
            }

            var parameters = new List<AnalyticsParameterVO>(analyticsEvent.Parameters.Count);

            foreach (AnalyticsParameterVO parameter in analyticsEvent.Parameters)
            {
                if (parameters.Count >= limits.ParameterCount)
                {
                    changes.Add($"parameter '{parameter.Name}' dropped, past {limits.ParameterCount}");
                    continue;
                }

                AnalyticsParameterVO trimmed = parameter;
                string parameterName = parameter.Name ?? string.Empty;

                if (parameterName.Length > limits.ParameterNameLength)
                {
                    trimmed = trimmed.Renamed(parameterName.Substring(0, limits.ParameterNameLength));
                    changes.Add($"parameter '{parameterName}' cut to {limits.ParameterNameLength}");
                }

                if (trimmed.Kind == AnalyticsParameterKind.String
                    && trimmed.StringValue != null
                    && trimmed.StringValue.Length > limits.StringValueLength)
                {
                    trimmed = trimmed.Shortened(trimmed.StringValue.Substring(0, limits.StringValueLength));
                    changes.Add($"value of '{parameterName}' cut to {limits.StringValueLength}");
                }

                parameters.Add(trimmed);
            }

            report = changes.Count == 0 ? string.Empty : string.Join("; ", changes);
            return changes.Count == 0 ? analyticsEvent : new AnalyticsEventVO(name, parameters);
        }
    }
}
