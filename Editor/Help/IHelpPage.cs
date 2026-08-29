#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// One topic in the help window. A page knows the painter and nothing else - not the window,
    /// not the catalog, not its own position in the list.
    /// </summary>
    internal interface IHelpPage
    {
        string Title { get; }

        /// <summary>
        /// A second line under the title in the sidebar, for a topic whose name does not say
        /// enough on its own. Empty for a page that needs no gloss.
        /// </summary>
        string Subtitle { get; }

        /// <summary>
        /// The built-in Editor icon drawn beside the topic in the sidebar, by the name
        /// EditorGUIUtility.IconContent takes. Skin-neutral names only: Unity picks the dark
        /// variant itself.
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// Whether the sidebar draws this topic on the banner colour. The introduction is the one
        /// page that earns it; everything else is an ordinary row.
        /// </summary>
        bool Featured { get; }

        /// <summary>Null for a page that has no diagram.</summary>
        HelpGraph Graph { get; }

        /// <summary>
        /// The readings this page offers. The first is the introduction; a page with only that
        /// one draws no tab bar at all.
        /// </summary>
        IReadOnlyList<HelpTab> Tabs { get; }

        /// <summary>Which of those readings is open. The window drives it from the banner.</summary>
        int SelectedTab { get; set; }

        /// <summary>
        /// The one thing this page can do, drawn as a button on the right of its banner. Null for
        /// a page that only explains something, which is most of them.
        /// </summary>
        HelpAction Action { get; }

        void Draw(HelpPainter painter);
    }
}

#endif