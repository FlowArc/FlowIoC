#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Pages;
using FlowIoC.Editor.Help.Pages.Modules;
using FlowIoC.Editor.Help.Pages.Tools;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// What the sidebar shows, in reading order. Closed, it is three entries: the introduction,
    /// everything there is to know about FlowIoC itself, and the modules that ship with it.
    /// Wiki is where the reference lives, so the architecture topics and the Editor's own tools
    /// fold out inside it rather than competing with it at the top level.
    ///
    /// Adding a topic is one class and one line here. Adding a module is a page under Modules.
    /// </summary>
    internal class HelpPageCatalog
    {
        public HelpPageCatalog()
        {
            Sections = new List<HelpSection>
            {
                new HelpSection(new WelcomePage()),
                new HelpSection("Wiki", "TextAsset Icon",
                    new HelpSection(new CreatingModulePage()),
                    new HelpSection(new FolderLayoutPage()),
                    new HelpSection(new DataTypesPage()),
                    new HelpSection(new OrderingRootsPage()),
                    new HelpSection("Structure", "UnityEditor.SceneHierarchyWindow",
                        new RootContextPage(),
                        new SignalsPage(),
                        new ControllersPage(),
                        new ModelPage(),
                        new ViewMediatorPage(),
                        new ConnectorsPage()),
                    new HelpSection("Editor Tools", "Settings",
                        new CodeGeneratorsPage(),
                        new ModuleScanPage(),
                        new FlowConsolePage(),
                        new ModelViewerPage(),
                        new FolderPainterPage(),
                        new ScreensPage(),
                        new AgentRulesPage(),
                        new AgentSkillsPage())),
                new HelpSection("Modules", "Prefab Icon", ModuleSections())
            };

            var pages = new List<IHelpPage>();

            foreach (HelpSection section in Sections)
                pages.AddRange(section.Pages);

            Pages = pages;
            OpeningPage = Find("Welcome");
        }

        /// <summary>
        /// The modules the package ships, and after them the ones a private package brings. The
        /// private category is absent rather than empty when there is no private package, so a
        /// project without one sees the sidebar it has always seen.
        /// </summary>
        private HelpSection[] ModuleSections()
        {
            var sections = new List<HelpSection>
            {
                new HelpSection(new SetupModulesPage()),
                new HelpSection(new CounterModulePage()),
                new HelpSection(new CameraSystemModulePage()),
                new HelpSection(new InputModulePage())
            };

            HelpSection privateModules = new PrivateModuleSections().Category();

            if (privateModules != null)
                sections.Add(privateModules);

            return sections.ToArray();
        }

        public IReadOnlyList<HelpSection> Sections { get; }

        /// <summary>Every page in the catalogue, categories flattened away.</summary>
        public IReadOnlyList<IHelpPage> Pages { get; }

        /// <summary>
        /// Where the window opens. The introduction, so a reader who has never seen FlowIoC
        /// meets what it is before meeting how its folders are arranged.
        /// </summary>
        public IHelpPage OpeningPage { get; }

        /// <summary>
        /// Where the window opens when a reader asks for one of the top level sections by name -
        /// the Tools/FlowIoC/Help menu has an entry per section and each has to land somewhere.
        /// A category answers with the first topic inside it, however deep that topic sits; a
        /// section that is a topic answers with itself.
        ///
        /// Only the top level is searched. A category nested inside one is on the sidebar but is
        /// not offered a menu entry, so being unable to name it here is the point rather than a
        /// gap. An unknown title answers null, and the caller opens the window where it would
        /// have opened anyway.
        /// </summary>
        public IHelpPage FirstPageOf(string sectionTitle)
        {
            foreach (HelpSection section in Sections)
            {
                if (section.Title != sectionTitle)
                    continue;

                foreach (IHelpPage page in section.Pages)
                    return page;

                return null;
            }

            return null;
        }

        /// <summary>
        /// Every category a page sits inside, outermost first. A topic two levels down needs both
        /// of them folded open before it is on screen, so the caller gets the whole chain rather
        /// than only the category immediately above the page.
        /// </summary>
        public IReadOnlyList<HelpSection> CategoriesContaining(IHelpPage page)
        {
            var chain = new List<HelpSection>();

            foreach (HelpSection section in Sections)
            {
                if (TryBuildChain(section, page, chain))
                    break;

                chain.Clear();
            }

            return chain;
        }

        private static bool TryBuildChain(HelpSection section, IHelpPage page, List<HelpSection> chain)
        {
            if (!section.IsCategory)
                return ReferenceEquals(section.Page, page);

            chain.Add(section);

            foreach (HelpSection child in section.Children)
            {
                if (TryBuildChain(child, page, chain))
                    return true;
            }

            chain.RemoveAt(chain.Count - 1);

            return false;
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

        /// <summary>
        /// The page with this exact title, or null when the catalogue has none. A caller that
        /// links to a page - the inspector's header bar does - needs to know when the page it
        /// wants was never written, so it can leave the link out rather than open the wrong one.
        /// </summary>
        public IHelpPage FindPage(string title)
        {
            foreach (IHelpPage page in Pages)
            {
                if (page.Title == title)
                    return page;
            }

            return null;
        }
    }
}

#endif