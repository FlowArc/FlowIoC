#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// A second reading of the same topic, reachable from the tabs at the top right of a page.
    /// The introduction says what a thing is; a tab beside it can carry the rules, the corner
    /// cases, or a longer worked example without crowding the first thing a reader sees.
    /// </summary>
    internal class HelpTab
    {
        private readonly Action<HelpPainter> _draw;

        public HelpTab(string title, Action<HelpPainter> draw)
        {
            Title = title;
            _draw = draw;
        }

        public string Title { get; }

        public void Draw(HelpPainter painter) => _draw(painter);
    }
}

#endif
