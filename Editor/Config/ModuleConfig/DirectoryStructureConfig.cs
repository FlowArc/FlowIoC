#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Config.ModuleConfig
{
    public abstract class DirectoryStructureConfig : ScriptableObject
    {
        [field: NonSerialized] protected internal virtual List<FolderConfig> RootFolders { get; protected set; } = new List<FolderConfig>();

        protected virtual void InitializeDefaultFolderStructure()
        {
            RootFolders = new List<FolderConfig> { };
        }

        protected virtual FolderConfig CreateFolder(string folderName, FolderConfig.FolderType folderType, List<FolderConfig> subFolders = null,
            bool isMandatory = false, bool isOptional = false,
            bool isNamespaceProvider = true)
        {
            return new FolderConfig
            {
                FolderName = folderName,
                Type = folderType,
                SubFolders = subFolders ?? new List<FolderConfig>(),
                IsMandatory = isMandatory,
                IsOptional = isOptional,
                IsNamespaceProvider = isNamespaceProvider
            };
        }

        protected internal virtual string FindFullFolderPathByID(FolderConfig.FolderType folderName, string basePath)
        {
            return FindFullFolderPathByID(folderName, basePath, out _);
        }

        /// <summary>
        /// The same lookup, plus whether the <see cref="FolderConfig"/> it landed on is marked
        /// optional. The flag rides along on the walk that already finds the node rather than
        /// costing a second one, so a caller that warns about a folder missing from disk can
        /// stay quiet about the ones a module was never required to have in the first place.
        /// </summary>
        protected internal virtual string FindFullFolderPathByID(FolderConfig.FolderType folderName, string basePath, out bool isOptional)
        {
            return FindFolderPathByID(folderName, RootFolders, basePath, out isOptional);
        }

        protected virtual string FindFolderPathByID(FolderConfig.FolderType folderID, List<FolderConfig> folders, string basePath,
            out bool isOptional)
        {
            isOptional = false;
            return string.Empty;
        }

        /// <summary>
        /// Adds the Shared branch to a config asset written before the branch existed, and returns
        /// whether it changed anything.
        ///
        /// A layout's structure in code is only the default a brand new asset is stamped with;
        /// every project that already ran the code generator has its own serialized copy, which
        /// the GetOrCreateConfig of each layout loads untouched. Without this the Shared folder
        /// would never appear in an existing project, and asking people to delete the asset to get
        /// it would throw away whatever they had customized in the inspector. So this only ever
        /// appends, and only when the project has no Shared folder at all.
        /// </summary>
        internal bool EnsureSharedBranch(CodeGeneratorSettings codeGenSettings)
        {
            if (codeGenSettings == null || RootFolders == null) return false;
            if (ContainsFolderType(RootFolders, FolderConfig.FolderType.Shared)) return false;

            FolderConfig scripts = FindFolderByName(RootFolders, "Scripts");
            if (scripts == null)
            {
                Debug.LogWarning($"<color=cyan>FlowIoC:</color> the {GetType().Name} directory structure has no 'Scripts' folder, so the " +
                                 "Shared branch could not be added to it. Add a Shared folder to the config asset by hand if this " +
                                 "module layout is meant to have one.");
                return false;
            }

            scripts.SubFolders ??= new List<FolderConfig>();
            scripts.SubFolders.Add(BuildSharedBranch(codeGenSettings));

            RegisterSharedFolderNames(codeGenSettings);

            return true;
        }

        /// <summary>
        /// Adds the Signals folder to a Shared branch written before the public signal holder moved
        /// into it, and returns whether it changed anything.
        ///
        /// <see cref="EnsureSharedBranch"/> only fires for a config with no Shared folder at all,
        /// so a project that adopted Shared while it still held data alone would never grow the
        /// folder its signals now belong in. This is the same append-only heal, one level down.
        /// </summary>
        internal bool EnsureSharedSignalsFolder(CodeGeneratorSettings codeGenSettings)
        {
            if (codeGenSettings == null || RootFolders == null) return false;
            if (ContainsFolderType(RootFolders, FolderConfig.FolderType.SharedSignals)) return false;

            FolderConfig shared = FindFolderByType(RootFolders, FolderConfig.FolderType.Shared);
            if (shared == null) return false;

            shared.SubFolders ??= new List<FolderConfig>();
            shared.SubFolders.Add(
                CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedSignals, "Signals"),
                    FolderConfig.FolderType.SharedSignals, null, true));

            RegisterSharedFolderNames(codeGenSettings);

            return true;
        }

        /// <summary>
        /// The Shared folder as every layout that has one lays it out: the data a module publishes,
        /// the enums and constants that data needs, and the module's public signal holder.
        /// </summary>
        protected FolderConfig BuildSharedBranch(CodeGeneratorSettings codeGenSettings)
        {
            return CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.Shared, "Shared"), FolderConfig.FolderType.Shared,
                new List<FolderConfig>
                {
                    CreateFolder("Data", FolderConfig.FolderType.Folder, new List<FolderConfig>
                    {
                        CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedUnityObjects, "UnityObjects"),
                            FolderConfig.FolderType.SharedUnityObjects, null, true),
                        CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedValueObjects, "ValueObjects"),
                            FolderConfig.FolderType.SharedValueObjects, null, true)
                    }, true),
                    CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedEnums, "Enums"),
                        FolderConfig.FolderType.SharedEnums, null, true),
                    CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedConstants, "Constants"),
                        FolderConfig.FolderType.SharedConstants, null, true),
                    CreateFolder(codeGenSettings.FolderNameFor(FolderConfig.FolderType.SharedSignals, "Signals"),
                        FolderConfig.FolderType.SharedSignals, null, true)
                }, false, true);
        }

        /// <summary>
        /// Puts the Shared folder types into the settings map, which is what makes their folders
        /// rename-tracked: ModuleIndexRegistrar records a GUID per type in that map, and
        /// ApplyConfiguredFolderNames renames per type in it. A settings asset written before
        /// these types existed has none of them, so the branch this accompanies would otherwise
        /// arrive untracked.
        ///
        /// This runs only on the pass that adds the branch, never on every load. Removing an entry
        /// from the settings inspector is a deliberate act, and a heal that ran unconditionally
        /// would put it straight back.
        /// </summary>
        protected void RegisterSharedFolderNames(CodeGeneratorSettings codeGenSettings)
        {
            IReadOnlyDictionary<FolderConfig.FolderType, string> defaults = new CodeGeneratorDefaults().SharedFolderNames;

            bool added = false;

            foreach (KeyValuePair<FolderConfig.FolderType, string> entry in defaults)
            {
                if (codeGenSettings.DirectoryStructureConfigMap.ContainsKey(entry.Key)) continue;

                codeGenSettings.DirectoryStructureConfigMap[entry.Key] = entry.Value;
                added = true;
            }

            if (!added) return;

            EditorUtility.SetDirty(codeGenSettings);
        }

        protected bool ContainsFolderType(List<FolderConfig> folders, FolderConfig.FolderType folderType)
        {
            if (folders == null) return false;

            foreach (FolderConfig folder in folders)
            {
                if (folder.Type == folderType) return true;
                if (ContainsFolderType(folder.SubFolders, folderType)) return true;
            }

            return false;
        }

        protected FolderConfig FindFolderByName(List<FolderConfig> folders, string folderName)
        {
            if (folders == null) return null;

            foreach (FolderConfig folder in folders)
            {
                if (string.Equals(folder.FolderName, folderName, StringComparison.OrdinalIgnoreCase)) return folder;

                FolderConfig found = FindFolderByName(folder.SubFolders, folderName);
                if (found != null) return found;
            }

            return null;
        }

        protected FolderConfig FindFolderByType(List<FolderConfig> folders, FolderConfig.FolderType folderType)
        {
            if (folders == null) return null;

            foreach (FolderConfig folder in folders)
            {
                if (folder.Type == folderType) return folder;

                FolderConfig found = FindFolderByType(folder.SubFolders, folderType);
                if (found != null) return found;
            }

            return null;
        }
    }
}
#endif