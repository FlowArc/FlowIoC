#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Help.Graph;
using FlowIoC.Editor.Icons;

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
        /// The role the page wears. A module page wears what its Root wears in the inspector, and
        /// the window draws the banner and the marks inside the page in that colour. Null for a
        /// page that is about no one module, which wears the window's own violet.
        /// </summary>
        FlowRole? Role { get; }

        /// <summary>
        /// A second line under the title in the sidebar, for a topic whose name does not say
        /// enough on its own. Empty for a page that needs no gloss.
        /// </summary>
        string Subtitle { get; }

        /// <summary>
        /// The drawing beside the topic in the sidebar. One of FlowIoC's own, so it is drawn at
        /// the pixels it was made for and takes the colour of the name next to it.
        /// </summary>
        FlowIcon Icon { get; }

        /// <summary>
        /// Whether the sidebar draws this topic on the banner colour. The introduction is the one
        /// page that earns it; everything else is an ordinary row.
        /// </summary>
        bool Featured { get; }

        /// <summary>The flag on the page's sidebar row, or null - which is every page but a module's.</summary>
        SidebarFlagEVO SidebarFlag { get; }

        /// <summary>
        /// The version the banner shows beside the page's action, or null - which is every page
        /// but a module's. A module page shows the installed version, or the shipped one while
        /// the module is not here, so the number is read where the button that acts on it sits.
        /// </summary>
        string Version { get; }

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