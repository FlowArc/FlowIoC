#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.Icons;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// The modules every package brings, gathered into one category per package under Modules.
    /// The pages are found rather than listed: a package declares a ModulePage beside its
    /// Modules~ folder and the window picks it up, which is what lets FlowIoC ship without
    /// knowing any other package exists - and what lets FlowIoC's own modules be found the same
    /// way, under FlowModules.
    ///
    /// A package with no page produces no category at all rather than an empty one, so a project
    /// with only FlowIoC sees FlowModules and nothing after it.
    /// </summary>
    internal class PackageModuleSections
    {
        /// <summary>A folder per package, which is what a category of somebody's modules is.</summary>
        private const FlowIcon CategoryIcon = FlowIcon.Folder;

        private readonly IReadOnlyList<ModulePage> _pages;
        private readonly Func<ModulePage, ModuleGroup> _groupOf;
        private readonly ProjectAsmdefs _asmdefs;

        internal PackageModuleSections()
            : this(new ProjectAsmdefs(new ProjectRoot().Resolve()))
        {
        }

        /// <summary>
        /// Every package's pages over one walk of the project's asmdefs, shared with the setup
        /// pages beside them, so the whole library reads what is installed once.
        /// </summary>
        internal PackageModuleSections(ProjectAsmdefs asmdefs)
            : this(Found(), page => ModuleGroup.Of(page.GetType().Assembly), asmdefs)
        {
        }

        internal PackageModuleSections(IReadOnlyList<ModulePage> pages, Func<ModulePage, ModuleGroup> groupOf)
            : this(pages, groupOf, new ProjectAsmdefs(new ProjectRoot().Resolve()))
        {
        }

        internal PackageModuleSections(IReadOnlyList<ModulePage> pages, Func<ModulePage, ModuleGroup> groupOf,
            ProjectAsmdefs asmdefs)
        {
            _pages = pages ?? new ModulePage[0];
            _groupOf = groupOf;
            _asmdefs = asmdefs;
        }

        /// <summary>
        /// Every page declared anywhere in the project, in no particular order.
        ///
        /// Three kinds of type are stepped over. An abstract one is somebody's base class rather
        /// than a module. One with no parameterless constructor cannot be built without knowing
        /// what to hand it. And a nested one is a test double: a page is a class of its own, and
        /// picking up the doubles would put them in the help window of every project that has a
        /// test assembly loaded. The startup notice reads the same list to say which installed
        /// modules have updates.
        /// </summary>
        internal static IReadOnlyList<ModulePage> Found()
        {
            var pages = new List<ModulePage>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<ModulePage>())
            {
                if (type.IsAbstract || type.IsNested || type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                try
                {
                    pages.Add((ModulePage) Activator.CreateInstance(type));
                }
                catch (Exception exception)
                {
                    Debug.LogError($"<color=cyan>[FlowIoC]</color> {type.Name} could not be read as "
                                   + $"a module page: {exception.Message}");
                }
            }

            return pages;
        }

        /// <summary>
        /// One category per package that declares a page, FlowIoC's own first and the rest by
        /// title; inside each, the pages by title. Titles order the rows because TypeCache
        /// answers in whatever order the assemblies were scanned, and rows that move between
        /// reloads read as a bug.
        /// </summary>
        internal IReadOnlyList<HelpSection> Categories()
        {
            var groups = new List<ModuleGroup>();
            var pagesOf = new Dictionary<string, List<ModulePage>>();

            foreach (ModulePage page in _pages)
            {
                // The setup set has a category of its own, listed by the catalogue in reading order.
                if (page.InSetupSet)
                    continue;

                ModuleGroup group = _groupOf(page);

                if (!pagesOf.TryGetValue(group.Key, out List<ModulePage> pages))
                {
                    pages = new List<ModulePage>();
                    pagesOf[group.Key] = pages;
                    groups.Add(group);
                }

                pages.Add(page);
            }

            groups.Sort(Compare);

            var categories = new HelpSection[groups.Count];

            for (int index = 0; index < groups.Count; index++)
                categories[index] = CategoryOf(groups[index], pagesOf[groups[index].Key]);

            return categories;
        }

        private static int Compare(ModuleGroup left, ModuleGroup right)
        {
            if (left.IsOwn != right.IsOwn)
                return left.IsOwn ? -1 : 1;

            return string.CompareOrdinal(left.Title, right.Title);
        }

        private HelpSection CategoryOf(ModuleGroup group, List<ModulePage> pages)
        {
            pages.Sort((left, right) => string.CompareOrdinal(left.Title, right.Title));

            var sections = new IHelpPage[pages.Count];

            for (int index = 0; index < pages.Count; index++)
                sections[index] = new ModulePageAdapter(pages[index], _asmdefs);

            return new HelpSection(group.Title, CategoryIcon, sections);
        }
    }
}

#endif