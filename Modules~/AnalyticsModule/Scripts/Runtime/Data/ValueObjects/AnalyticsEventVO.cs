using System.Collections.Generic;
using System.Text;

namespace Modules.AnalyticsModule.Data.ValueObjects
{
    /// <summary>
    /// What a caller logs: a name and its parameters, built in one line -
    /// <c>new AnalyticsEventVO("level_start").With("level", 3).With("mode", "hard")</c>. The module
    /// keeps no names of its own; a game keeps them beside the Commands that log them.
    /// </summary>
    public class AnalyticsEventVO
    {
        private readonly List<AnalyticsParameterVO> _parameters = new();

        public string Name { get; }

        public IReadOnlyList<AnalyticsParameterVO> Parameters => _parameters;

        public AnalyticsEventVO(string name)
        {
            Name = name;
        }

        /// <summary>An event with its parameters already built; what the trimmer returns.</summary>
        public AnalyticsEventVO(string name, IEnumerable<AnalyticsParameterVO> parameters)
        {
            Name = name;
            _parameters.AddRange(parameters);
        }

        public AnalyticsEventVO With(string name, string value)
        {
            _parameters.Add(new AnalyticsParameterVO(name, value));
            return this;
        }

        public AnalyticsEventVO With(string name, long value)
        {
            _parameters.Add(new AnalyticsParameterVO(name, value));
            return this;
        }

        public AnalyticsEventVO With(string name, double value)
        {
            _parameters.Add(new AnalyticsParameterVO(name, value));
            return this;
        }

        public AnalyticsEventVO With(string name, bool value)
        {
            _parameters.Add(new AnalyticsParameterVO(name, value));
            return this;
        }

        /// <summary><c>level_start { level=3, mode=hard }</c> - the line the console shows.</summary>
        public override string ToString()
        {
            if (_parameters.Count == 0)
                return Name;

            var text = new StringBuilder(Name).Append(" { ");

            for (int i = 0; i < _parameters.Count; i++)
            {
                if (i > 0)
                    text.Append(", ");

                text.Append(_parameters[i].Name).Append('=').Append(_parameters[i].ValueText);
            }

            return text.Append(" }").ToString();
        }
    }
}
