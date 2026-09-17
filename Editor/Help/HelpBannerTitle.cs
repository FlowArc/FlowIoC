#if UNITY_EDITOR

using FlowIoC.BaseModule.Attributes;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// The line on a page's banner. A topic is its title alone; a module page follows the title
    /// with the role its Root wears - <c>Ads Module / Service</c>, the role in italics so the two
    /// read as a name and a kind rather than as one long name - and then, when the module carries
    /// a version, the version in parentheses: <c>(v.1.0.1)</c>. Rich text, drawn by a style that
    /// reads it.
    /// </summary>
    internal class HelpBannerTitle
    {
        internal string Of(string title, FlowRole? role, string version)
        {
            string line = title;

            if (role.HasValue)
                line += $" <i>/ {role.Value}</i>";

            if (!string.IsNullOrEmpty(version))
                line += $" (v.{version})";

            return line;
        }
    }
}

#endif
