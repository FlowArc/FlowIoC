#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Pages;
using FlowIoC.Editor.Help.Pages.Tools;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// What the sidebar shows, in reading order: the introduction on its own, then the reference
    /// topics folded into one category, the architecture into another and the Editor's own tools
    /// into a third. Adding a topic is one class and one line here.
    /// </summary>
    internal class HelpPageCatalog
    {
        public HelpPageCatalog()
        {
            Sections = new List<HelpSection>
            {
                new HelpSection(new WelcomePage()),
                new HelpSection("Wiki", "TextAsset Icon",
                    new FolderLayoutPage(),
                    new DataTypesPage()),
                new HelpSection("Structure", "UnityEditor.SceneHierarchyWindow",
                    new RootContextPage(),
                    new SignalsPage(),
                    new ControllersPage(),
                    new ModelPage(),
                    new ViewMediatorPage(),
                    new ConnectorsPage()),
                new HelpSection("Editor Tools", "Settings",
                    new CodeGeneratorsPage(),
                    new ModuleConfigurationPage(),
                    new FlowConsolePage(),
                    new ModelViewerPage(),
                    new FolderDrawerPage(),
                    new ScreenConfigManagerPage(),
                    new AgentRulesPage(),
                    new AgentSkillsPage())
            };

            var pages = new List<IHelpPage>();

            foreach (HelpSection section in Sections)
                pages.AddRange(section.Pages);

            Pages = pages;
            OpeningPage = Find("Folder Layout");
        }

        public IReadOnlyList<HelpSection> Sections { get; }

        /// <summary>Every page in the catalogue, categories flattened away.</summary>
        public IReadOnlyList<IHelpPage> Pages { get; }

        /// <summary>
        /// Where the window opens. The folder tree is the first thing worth explaining, so the
        /// window lands there rather than on whichever page happens to be first.
        /// </summary>
        public IHelpPage OpeningPage { get; }

        public HelpSection SectionOf(IHelpPage page)
        {
            foreach (HelpSection section in Sections)
            {
                if (section.Contains(page))
                    return section;
            }

            return null;
        }

        private IHelpPage Find(string title)
        {
            foreach (IHelpPage page in Pages)
            {
                if (page.Title == title)
                    return page;
            }

            return Pages[0];
        }
    }
}

#endif