#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// The part every page shares: it holds its diagram, the reader's position in it, and the
    /// tabs it is read through. The state lives here rather than in the window so that each page
    /// remembers its own walk and its own tab.
    /// </summary>
    internal abstract class HelpPage : IHelpPage
    {
        private IReadOnlyList<HelpTab> _tabs;
        private int _tab;

        protected HelpPage(HelpGraph graph)
        {
            Graph = graph;
            Stepper = new HelpGraphStepper(graph == null ? 0 : graph.Steps.Count);
        }

        public abstract string Title { get; }

        public virtual string Subtitle => string.Empty;

        public abstract string Icon { get; }

        public virtual bool Featured => false;

        public HelpGraph Graph { get; }

        protected HelpGraphStepper Stepper { get; }

        /// <summary>What the first tab is called. The body of the page is what it shows.</summary>
        protected virtual string BodyTabTitle => "Introduction";

        /// <summary>Tabs beside the body. Empty for a page that is one reading only.</summary>
        protected virtual IReadOnlyList<HelpTab> MoreTabs => new HelpTab[0];

        public IReadOnlyList<HelpTab> Tabs
        {
            get
            {
                if (_tabs != null)
                    return _tabs;

                var tabs = new List<HelpTab> {new HelpTab(BodyTabTitle, DrawBody)};
                tabs.AddRange(MoreTabs);
                _tabs = tabs;

                return _tabs;
            }
        }

        /// <summary>
        /// Which reading is open. The window sets this from the banner it draws above the scroll
        /// view, and the page remembers it while the window lives.
        /// </summary>
        public int SelectedTab
        {
            get => _tab;
            set => _tab = value < 0 || value >= Tabs.Count ? 0 : value;
        }

        public void Draw(HelpPainter painter) => Tabs[SelectedTab].Draw(painter);

        protected abstract void DrawBody(HelpPainter painter);
    }
}

#endif