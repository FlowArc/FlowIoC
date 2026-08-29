#if UNITY_EDITOR

using System.Collections.Generic;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// One entry in the sidebar: either a topic that opens straight away, or a category that
    /// folds open to show what is inside it.
    ///
    /// A category holds sections rather than pages, so a category can hold another category. That
    /// is what lets the reference topics and the Editor's own tools sit inside Wiki instead of
    /// beside it, and what lets a module bring a category of its own.
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
            Page = page;
            Children = new HelpSection[0];
            IsCategory = false;
        }

        /// <summary>A category of topics. Clicking it folds them open and shut.</summary>
        public HelpSection(string title, string icon, params IHelpPage[] pages)
            : this(title, icon, ToSections(pages))
        {
        }

        /// <summary>
        /// A category that holds sections, which may themselves be categories. Mixing the two is
        /// allowed and reads in the order given: Wiki puts its own topics first and the categories
        /// that go deeper after them.
        /// </summary>
        public HelpSection(string title, string icon, params HelpSection[] children)
        {
            Title = title;
            Subtitle = string.Empty;
            Icon = icon;
            Featured = false;
            Page = null;
            Children = children ?? new HelpSection[0];
            IsCategory = true;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string Icon { get; }
        public bool Featured { get; }
        public bool IsCategory { get; }

        /// <summary>What folds out below a category. Empty for a topic.</summary>
        public IReadOnlyList<HelpSection> Children { get; }

        /// <summary>The topic this section opens, or null when it is a category.</summary>
        public IHelpPage Page { get; }

        /// <summary>Every page at or below this section, categories flattened away.</summary>
        public IEnumerable<IHelpPage> Pages
        {
            get
            {
                if (Page != null)
                    yield return Page;

                foreach (HelpSection child in Children)
                {
                    foreach (IHelpPage page in child.Pages)
                        yield return page;
                }
            }
        }

        /// <summary>This section and everything below it, in the order the sidebar draws them.</summary>
        public IEnumerable<HelpSection> Descendants()
        {
            yield return this;

            foreach (HelpSection child in Children)
            {
                foreach (HelpSection descendant in child.Descendants())
                    yield return descendant;
            }
        }

        public bool Contains(IHelpPage page)
        {
            foreach (IHelpPage candidate in Pages)
            {
                if (ReferenceEquals(candidate, page))
                    return true;
            }

            return false;
        }

        private static HelpSection[] ToSections(IHelpPage[] pages)
        {
            if (pages == null)
                return new HelpSection[0];

            var sections = new HelpSection[pages.Length];

            for (int i = 0; i < pages.Length; i++)
                sections[i] = new HelpSection(pages[i]);

            return sections;
        }
    }
}

#endif