#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

namespace FlowIoC.Editor.ModuleScan
{
    /// <summary>
    /// The folders a module may live in. Assets/Modules is the obvious one; an embedded package
    /// may hold modules too - the private addons package does - and a scan that missed them
    /// would report the index as drifted against modules it never looked at.
    ///
    /// This used to be a private method on ModuleIndexRebuilder. It is a class of its own now
    /// because the rebuild and the scan have to agree on it: two answers to "what is a module
    /// folder" is exactly how an index and a report end up contradicting each other.
    /// </summary>
    internal class ModuleScanRoots
    {
        private const string ASSETS = "Assets";
        private const string PACKAGES = "Packages";
        private const string MODULES = "Modules";

        private readonly Func<string, bool> _folderExists;
        private readonly Func<string, string[]> _directoriesIn;
        private readonly Func<string, string, string[]> _directoriesNamed;

        internal ModuleScanRoots() : this(
            Directory.Exists,
            Directory.GetDirectories,
            (path, name) => Directory.GetDirectories(path, name, SearchOption.AllDirectories))
        {
        }

        internal ModuleScanRoots(
            Func<string, bool> folderExists,
            Func<string, string[]> directoriesIn,
            Func<string, string, string[]> directoriesNamed)
        {
            _folderExists = folderExists;
            _directoriesIn = directoriesIn;
            _directoriesNamed = directoriesNamed;
        }

        /// <summary>
        /// Assets/Modules first when it exists, then every Modules folder inside an embedded
        /// package. A project without Assets/Modules is a fresh install rather than an error,
        /// and its packages are still worth scanning.
        /// </summary>
        internal IEnumerable<string> All(string projectRoot)
        {
            if (string.IsNullOrEmpty(projectRoot)) yield break;

            string assetsModules = Path.Combine(projectRoot, ASSETS, MODULES);
            if (_folderExists(assetsModules))
                yield return assetsModules;

            string packages = Path.Combine(projectRoot, PACKAGES);
            if (!_folderExists(packages)) yield break;

            foreach (string package in _directoriesIn(packages))
            foreach (string modules in _directoriesNamed(package, MODULES))
                yield return modules;
        }
    }
}

#endif
