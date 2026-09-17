#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Every asmdef under Assets, by the assembly name it declares, walked once and kept.
    ///
    /// Whether a module is installed is asked of the assemblies rather than the folders - a game
    /// may rename or move a module and is still credited with having it - and answering that
    /// used to mean walking every folder under Assets, per installer, per question. The Help
    /// window asked it for sixteen module pages, twice each, once a second, and froze for half a
    /// second every second while the Module Library was open. One instance is shared by everything
    /// that asks, and it walks again only when told the project changed - which is what
    /// installing a module and the Editor's own projectChanged event do.
    /// </summary>
    internal class ProjectAsmdefs
    {
        private Dictionary<string, string> _folders;

        internal ProjectAsmdefs(string projectRoot)
        {
            ProjectRoot = projectRoot;
        }

        internal string ProjectRoot { get; }

        /// <summary>The folder holding the asmdef that declares this assembly, or null when none does.</summary>
        internal string FolderOf(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
                return null;

            _folders ??= Walk();

            return _folders.TryGetValue(assemblyName, out string folder) ? folder : null;
        }

        /// <summary>The next question walks Assets again. Called when the project has changed underneath.</summary>
        internal void Invalidate() => _folders = null;

        /// <summary>
        /// The name out of one asmdef, or null when the file cannot be read or declares none. An
        /// unreadable asmdef is somebody else's problem to report: here it only means this file
        /// is not the one being looked for.
        /// </summary>
        internal string NameIn(string asmdefPath)
        {
            try
            {
                var declaration = JsonUtility.FromJson<AssemblyDefinitionName>(File.ReadAllText(asmdefPath));

                return declaration == null || string.IsNullOrEmpty(declaration.name) ? null : declaration.name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Dictionary<string, string> Walk()
        {
            var folders = new Dictionary<string, string>(StringComparer.Ordinal);
            string assets = Path.Combine(ProjectRoot, "Assets");

            if (!Directory.Exists(assets))
                return folders;

            foreach (string asmdef in Directory.GetFiles(assets, "*.asmdef", SearchOption.AllDirectories))
            {
                string name = NameIn(asmdef);

                // The first folder declaring a name answers, the way the walk answered before it
                // was kept; a second declaration is the compile error Unity already reports.
                if (name != null && !folders.ContainsKey(name))
                    folders[name] = Path.GetDirectoryName(asmdef);
            }

            return folders;
        }

        /// <summary>
        /// Just enough of an asmdef to read its assembly name. The field is lower case because
        /// that is what the file says and JsonUtility matches on the field name - renaming it to
        /// match the project's style would simply stop it reading anything.
        /// </summary>
        [Serializable]
        private class AssemblyDefinitionName
        {
            public string name;
        }
    }
}

#endif
