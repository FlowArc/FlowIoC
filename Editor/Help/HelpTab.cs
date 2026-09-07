#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// A second reading of the same topic, reachable from the tabs at the top right of a page.
    /// The introduction says what a thing is; a tab beside it can carry the rules, the corner
    /// cases, or a longer worked example without crowding the first thing a reader sees.
    /// </summary>
    public class HelpTab
    {
        private readonly Action<HelpPainter> _draw;

        public HelpTab(string title, Action<HelpPainter> draw, string headline = null,
            string tagline = null)
        {
            Title = title;
            Headline = headline;
            Tagline = tagline;
            _draw = draw;
        }

        public string Title { get; }

        /// <summary>
        /// The one line this reading opens with, drawn by the window in the band under the banner
        /// rather than by the page itself. It is declared here because each reading of a topic
        /// opens on something of its own, and the band is above the scroll view: the sentence a
        /// reader leaves with should not be the first thing to scroll off the top.
        /// </summary>
        public string Headline { get; }

        /// <summary>The paragraph under the headline. Null for a reading whose name says enough.</summary>
        public string Tagline { get; }

        public void Draw(HelpPainter painter) => _draw(painter);
    }
}

#endif