#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// Facts in, block body out. A line with nothing to say is left out rather than printed
    /// empty: a Connector announces no signals and a screen module has no Root of its own, and
    /// an empty label reads as a defect where an absent line reads as the truth.
    /// </summary>
    internal class ModuleCardBodyBuilder
    {
        private const string SEPARATOR = " · ";

        internal string Build(ModuleFactsEVO facts)
        {
            var groups = new List<List<string>>();

            // Kind and Assemblies run together on one line: they answer the same question and
            // each is short.
            var head = new List<string> {Line("Kind", facts.Kind)};
            Add(head, Labelled("Assemblies", facts.Assemblies, ", "));

            var identity = new List<string> {string.Join(SEPARATOR, head)};
            Add(identity, RootLine(facts));
            groups.Add(identity);

            var signals = new List<string>();
            Add(signals, Labelled("Incoming", facts.Incoming, SEPARATOR));
            Add(signals, Labelled("Outgoing", facts.Outgoing, SEPARATOR));
            groups.Add(signals);

            var surface = new List<string>();
            Add(surface, Labelled("Publishes", facts.Publishes, ", "));
            Add(surface, Labelled("Services", facts.Services, ", "));
            Add(surface, Labelled("Systems", facts.Systems, ", "));
            Add(surface, Labelled("Sub modules", facts.SubModules, SEPARATOR));
            groups.Add(surface);

            var builder = new StringBuilder();

            foreach (List<string> group in groups)
            {
                if (group.Count == 0) continue;
                if (builder.Length > 0) builder.Append('\n');

                builder.Append(string.Join("\n", group));
            }

            return builder.ToString();
        }

        private string Line(string label, string value) => "**" + label + "** " + value;

        private string Labelled(string label, IReadOnlyList<string> values, string separator)
        {
            return values == null || values.Count == 0 ? null : Line(label, string.Join(separator, values));
        }

        private string RootLine(ModuleFactsEVO facts)
        {
            if (!string.IsNullOrEmpty(facts.RootType) && !string.IsNullOrEmpty(facts.ContextType))
                return Line("Root", facts.RootType + " → " + facts.ContextType);

            if (!string.IsNullOrEmpty(facts.ContextType))
                return Line("Context", facts.ContextType);

            return null;
        }

        private void Add(List<string> lines, string line)
        {
            if (!string.IsNullOrEmpty(line)) lines.Add(line);
        }
    }
}

#endif
