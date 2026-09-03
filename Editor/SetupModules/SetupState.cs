#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;

namespace FlowIoC.Editor.SetupModules
{
    /// <summary>
    /// Whether this project has already been offered the setup modules, and the record that says
    /// so.
    ///
    /// The record is a plain file under ProjectSettings rather than an asset, because it has to be
    /// committed and it has nothing to do with the Assets tree a game reorganises freely. Every
    /// clone of the project reads the same answer, which is the whole point: without it the set
    /// would install itself a second time on top of modules the game has been editing.
    ///
    /// Nothing here throws. A file that is missing, unreadable or nonsense means the set has not
    /// been installed, which is the answer that leads to the checks the installer makes anyway.
    /// </summary>
    internal class SetupState
    {
        internal const string FileName = "FlowIoCSetup.json";
        private const string SettingsFolder = "ProjectSettings";

        private readonly string _projectRoot;

        internal SetupState(string projectRoot)
        {
            _projectRoot = projectRoot;
        }

        private string MarkerPath => Path.Combine(_projectRoot, SettingsFolder, FileName);

        internal bool IsInstalled()
        {
            try
            {
                if (!File.Exists(MarkerPath))
                    return false;

                var record = JsonUtility.FromJson<SetupRecord>(File.ReadAllText(MarkerPath));

                return record != null && record.setupModulesInstalled;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// The FlowIoC version the marker was written at, or empty when this project has none. It
        /// is the one record that says a project has had FlowIoC in it before, which is what lets
        /// What's New tell a reader meeting the package from one who has simply never been shown
        /// the notes.
        /// </summary>
        internal string InstalledVersion()
        {
            try
            {
                if (!File.Exists(MarkerPath))
                    return string.Empty;

                var record = JsonUtility.FromJson<SetupRecord>(File.ReadAllText(MarkerPath));

                return record == null ? string.Empty : record.installedVersion ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        internal void MarkInstalled(string version)
        {
            var record = new SetupRecord
            {
                setupModulesInstalled = true,
                installedVersion = version,
                installedAt = DateTime.UtcNow.ToString("o")
            };

            Directory.CreateDirectory(Path.Combine(_projectRoot, SettingsFolder));
            File.WriteAllText(MarkerPath, JsonUtility.ToJson(record, true));
        }

        /// <summary>
        /// The file's shape. The fields are lower camel case because that is what the file says and
        /// JsonUtility matches on the field name; renaming them to the project's style would stop
        /// it reading anything.
        /// </summary>
        [Serializable]
        private class SetupRecord
        {
            public bool setupModulesInstalled;
            public string installedVersion;
            public string installedAt;
        }
    }
}

#endif