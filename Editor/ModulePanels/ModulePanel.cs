#if UNITY_EDITOR

using FlowIoC.BaseModule.Attributes;

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// A window a module ships for the developer's use in the Editor - what the save module's
    /// shows about the file on disk, or resets. The module declares what the panel is made of and
    /// FlowIoC draws it, the way a <see cref="Help.ModulePage"/> declares a Help page: the bar,
    /// the rows and the buttons wear the house style once, in the window and the painter, and a
    /// panel written in any package looks like every other.
    ///
    /// A panel opens from the module's own menu item under <c>Tools/FlowIoC-Modules/&lt;Module&gt;/</c>,
    /// which ships inside the module and calls <see cref="ModulePanelWindow.Open{TPanel}"/>. Unity
    /// offers no way to add a menu item at run time, so that one line stays with the module -
    /// which is also what keeps FlowIoC's own menu free of anything a module adds.
    ///
    /// Every member is public: the window lives in another assembly and cannot read protected
    /// members, and a protected override paired with a public accessor would double the surface.
    /// </summary>
    public abstract class ModulePanel
    {
        /// <summary>What the window's tab and bar call the panel - "Local Save".</summary>
        public abstract string Title { get; }

        /// <summary>The module the panel belongs to, named on the bar's strip - "LocalSaveModule".</summary>
        public abstract string Module { get; }

        /// <summary>One line under the title on the bar. Empty for a title that says enough.</summary>
        public virtual string Subtitle => string.Empty;

        /// <summary>
        /// The colour the bar wears: the role of the module's Root, so a Service's panel is the
        /// colour its Root is painted in the Inspector.
        /// </summary>
        public virtual FlowRole Role => FlowRole.Root;

        /// <summary>The title of the Help page the bar's help icon opens, or null for no icon.</summary>
        public virtual string HelpPage => null;

        /// <summary>
        /// The one button the bar carries beside its help icon - Select asset, say: what is done to
        /// the panel as a whole rather than to a row of it. Null for none. Read every repaint, so
        /// a panel may offer it only while it applies.
        /// </summary>
        public virtual ModulePanelAction? BarAction => null;

        /// <summary>
        /// True for a panel that lists things down its left side and opens one of them on the
        /// right - the tests of an experiment set, one of which is being shaped. The window then
        /// splits under the bar: the list from <see cref="DrawSidebar"/> on the left, the rows from
        /// <see cref="Draw"/> beside it.
        /// </summary>
        public virtual bool HasSidebar => false;

        /// <summary>The list down the left side, in the sidebar's marks; drawn only while <see cref="HasSidebar"/>.</summary>
        public virtual void DrawSidebar(ModulePanelSidebarPainter sidebar)
        {
        }

        /// <summary>The rows of the panel, drawn every repaint with the marks the painter offers.</summary>
        public abstract void Draw(ModulePanelPainter painter);
    }
}

#endif
