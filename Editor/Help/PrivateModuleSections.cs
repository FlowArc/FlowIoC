#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// The modules a private package brings, gathered into one category under Modules. The pages
    /// are found rather than listed: a private package declares a PrivateModulePage and the
    /// window picks it up, which is what lets FlowIoC ship without knowing any private package
    /// exists.
    ///
    /// A project with no private package produces no category at all rather than an empty one, so
    /// the help window such a project sees is exactly the one it saw before this existed.
    /// </summary>
    internal class PrivateModuleSections
    {
        internal const string CategoryTitle = "Private Modules";

        /// <summary>
        /// A lock, because what puts these modules in a package of their own is that they cannot
        /// be published.
        /// </summary>
        private const string CategoryIcon = "InspectorLock";

        private readonly IReadOnlyList<PrivateModulePage> _pages;

        internal PrivateModuleSections() : this(Found())
        {
        }

        internal PrivateModuleSections(IReadOnlyList<PrivateModulePage> pages)
        {
            _pages = pages ?? new PrivateModulePage[0];
        }

        /// <summary>
        /// Every page declared anywhere in the project, in no particular order.
        ///
        /// Three kinds of type are stepped over. An abstract one is somebody's base class rather
        /// than a module. One with no parameterless constructor cannot be built without knowing
        /// what to hand it. And a nested one is a test double: a page is a class of its own, and
        /// picking up the doubles would put them in the help window of every project that has a
        /// test assembly loaded.
        /// </summary>
        private static IReadOnlyList<PrivateModulePage> Found()
        {
            var pages = new List<PrivateModulePage>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<PrivateModulePage>())
            {
                if (type.IsAbstract || type.IsNested || type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                try
                {
                    pages.Add((PrivateModulePage) Activator.CreateInstance(type));
                }
                catch (Exception exception)
                {
                    Debug.LogError($"<color=cyan>[FlowIoC]</color> {type.Name} could not be read as "
                                   + $"a private module page: {exception.Message}");
                }
            }

            return pages;
        }

        /// <summary>
        /// The category, or null when there is nothing to put in it. Titles order the rows,
        /// because TypeCache answers in whatever order the assemblies were scanned and rows that
        /// move between reloads read as a bug.
        /// </summary>
        internal HelpSection Category()
        {
            if (_pages.Count == 0)
                return null;

            var sorted = new List<PrivateModulePage>(_pages);
            sorted.Sort((left, right) => string.CompareOrdinal(left.Title, right.Title));

            var sections = new HelpSection[sorted.Count];

            for (int index = 0; index < sorted.Count; index++)
                sections[index] = new HelpSection(new PrivateModulePageAdapter(sorted[index]));

            return new HelpSection(CategoryTitle, CategoryIcon, sections);
        }
    }
}

#endif
