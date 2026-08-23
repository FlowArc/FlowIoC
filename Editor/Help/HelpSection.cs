#if UNITY_EDITOR

using System.Collections.Generic;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// One entry in the sidebar: either a topic that opens straight away, or a category that
    /// folds open to show the topics inside it.
    /// </summary>
    internal class HelpSection
    {
        /// <summary>A topic of its own, with no category above it.</summary>
        public HelpSection(IHelpPage page)
        {
            Title = page.Title;
            Subtitle = page.Subtitle;
            Icon = page.Icon;
            Featured = page.Featured;
            Pages = new List<IHelpPage> { page };
            IsCategory = false;
        }

        /// <summary>A category. Clicking it folds the topics inside it open and shut.</summary>
        public HelpSection(string title, string icon, params IHelpPage[] pages)
        {
            Title = title;
            Subtitle = string.Empty;
            Icon = icon;
            Featured = false;
            Pages = pages;
            IsCategory = true;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string Icon { get; }
        public bool Featured { get; }
        public bool IsCategory { get; }
        public IReadOnlyList<IHelpPage> Pages { get; }

        public IHelpPage Page => Pages[0];

        public bool Contains(IHelpPage page)
        {
            foreach (IHelpPage candidate in Pages)
            {
                if (ReferenceEquals(candidate, page))
                    return true;
            }

            return false;
        }
    }
}

#endif
