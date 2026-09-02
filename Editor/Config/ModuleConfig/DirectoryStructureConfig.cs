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
        [field: NonSerialized] protected internal virtual List<FolderEVO> RootFolders { get; protected set; } = new List<FolderEVO>();

        protected virtual void InitializeDefaultFolderStructure()
        {
            RootFolders = new List<FolderEVO> { };
        }

        protected virtual FolderEVO CreateFolder(string folderName, FolderEVO.FolderType folderType, List<FolderEVO> subFolders = null,
            bool isMandatory = false, bool isOptional = false,
            bool isNamespaceProvider = true)
        {
            return new FolderEVO
            {
                FolderName = folderName,
                Type = folderType,
                SubFolders = subFolders ?? new List<FolderEVO>(),
                IsMandatory = isMandatory,
                IsOptional = isOptional,
                IsNamespaceProvider = isNamespaceProvider
            };
        }

        protected internal virtual string FindFullFolderPathByID(FolderEVO.FolderType folderName, string basePath)
        {
            return FindFullFolderPathByID(folderName, basePath, out _);
        }

        /// <summary>
        /// The same lookup, plus whether the <see cref="FolderEVO"/> it landed on is marked
        /// optional. The flag rides along on the walk that already finds the node rather than
        /// costing a second one, so a caller that warns about a folder missing from disk can
        /// stay quiet about the ones a module was never required to have in the first place.
        /// </summary>
        protected internal virtual string FindFullFolderPathByID(FolderEVO.FolderType folderName, string basePath, out bool isOptional)
        {
            return FindFolderPathByID(folderName, RootFolders, basePath, out isOptional);
        }

        protected virtual string FindFolderPathByID(FolderEVO.FolderType folderID, List<FolderEVO> folders, string basePath,
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
        internal bool EnsureSharedBranch(ED_CodeGenerator codeGenSettings)
        {
            if (codeGenSettings == null || RootFolders == null) return false;
            if (ContainsFolderType(RootFolders, FolderEVO.FolderType.Shared)) return false;

            FolderEVO scripts = FindFolderByName(RootFolders, "Scripts");
            if (scripts == null)
            {
                Debug.LogWarning($"<color=cyan>FlowIoC:</color> the {GetType().Name} directory structure has no 'Scripts' folder, so the " +
                                 "Shared branch could not be added to it. Add a Shared folder to the config asset by hand if this " +
                                 "module layout is meant to have one.");
                return false;
            }

            scripts.SubFolders ??= new List<FolderEVO>();
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
        internal bool EnsureSharedSignalsFolder(ED_CodeGenerator codeGenSettings)
        {
            if (codeGenSettings == null || RootFolders == null) return false;
            if (ContainsFolderType(RootFolders, FolderEVO.FolderType.SharedSignals)) return false;

            FolderEVO shared = FindFolderByType(RootFolders, FolderEVO.FolderType.Shared);
            if (shared == null) return false;

            shared.SubFolders ??= new List<FolderEVO>();
            shared.SubFolders.Add(
                CreateFolder(codeGenSettings.FolderNameFor(FolderEVO.FolderType.SharedSignals, "Signals"),
                    FolderEVO.FolderType.SharedSignals, null, true));

            RegisterSharedFolderNames(codeGenSettings);

            return true;
        }

        /// <summary>
        /// Takes a folder type out of the tree wherever it sits. A folder retires in code, but a
        /// project's config asset is serialized and keeps offering it; GetOrCreateConfig calls this
        /// so the asset catches up the next time the Editor opens.
        /// </summary>
        internal bool RemoveFolderType(FolderEVO.FolderType folderType) => RemoveFolderType(RootFolders, folderType);

        /// <summary>
        /// Marks a folder optional in a config asset that has it as mandatory. The screen layout
        /// shipped with `Scriptables` mandatory in the half of its declaration that gets
        /// serialized and optional in the half that does not, and correcting the code cannot reach
        /// an asset already written into a project.
        ///
        /// It went unnoticed for as long as nothing acted on the flag - Create Module only reads
        /// the optional ones, to decide which checkboxes to offer. Module Scan creates whatever is
        /// mandatory and missing, so it created a `Scriptables` folder in every screen module that
        /// did not have one.
        /// </summary>
        internal bool MakeFolderOptional(string folderName) => MakeFolderOptional(RootFolders, folderName);

        private static bool MakeFolderOptional(List<FolderEVO> folders, string folderName)
        {
            if (folders == null) return false;

            bool changed = false;

            foreach (FolderEVO folder in folders)
            {
                if (folder.FolderName == folderName && folder.IsMandatory)
                {
                    folder.IsMandatory = false;
                    folder.IsOptional = true;
                    changed = true;
                }

                changed |= MakeFolderOptional(folder.SubFolders, folderName);
            }

            return changed;
        }

        private static bool RemoveFolderType(List<FolderEVO> folders, FolderEVO.FolderType folderType)
        {
            if (folders == null) return false;

            bool removed = folders.RemoveAll(folder => folder.Type == folderType) > 0;

            foreach (FolderEVO folder in folders)
                removed |= RemoveFolderType(folder.SubFolders, folderType);

            return removed;
        }

        /// <summary>
        /// The Shared folder as every layout that has one lays it out: the data a module publishes,
        /// the enums and constants that data needs, and the module's public signal holder.
        /// </summary>
        protected FolderEVO BuildSharedBranch(ED_CodeGenerator codeGenSettings)
        {
            return CreateFolder(codeGenSettings.FolderNameFor(FolderEVO.FolderType.Shared, "Shared"), FolderEVO.FolderType.Shared,
                new List<FolderEVO>
                {
                    CreateFolder("Data", FolderEVO.FolderType.Folder, new List<FolderEVO>
                    {
                        CreateFolder(codeGenSettings.FolderNameFor(FolderEVO.FolderType.SharedUnityObjects, "UnityObjects"),
                            FolderEVO.FolderType.SharedUnityObjects, null, true),
                        CreateFolder(codeGenSettings.FolderNameFor(FolderEVO.FolderType.SharedValueObjects, "ValueObjects"),
                            FolderEVO.FolderType.SharedValueObjects, null, true)
                    }, true),
                    CreateFolder(codeGenSettings.FolderNameFor(FolderEVO.FolderType.SharedEnums, "Enums"),
                        FolderEVO.FolderType.SharedEnums, null, true),
                    CreateFolder(codeGenSettings.FolderNameFor(FolderEVO.FolderType.SharedConstants, "Constants"),
                        FolderEVO.FolderType.SharedConstants, null, true),
                    CreateFolder(codeGenSettings.FolderNameFor(FolderEVO.FolderType.SharedSignals, "Signals"),
                        FolderEVO.FolderType.SharedSignals, null, true)
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
        protected void RegisterSharedFolderNames(ED_CodeGenerator codeGenSettings)
        {
            IReadOnlyDictionary<FolderEVO.FolderType, string> defaults = new CodeGeneratorDefaults().SharedFolderNames;

            bool added = false;

            foreach (KeyValuePair<FolderEVO.FolderType, string> entry in defaults)
            {
                if (codeGenSettings.DirectoryStructureConfigMap.ContainsKey(entry.Key)) continue;

                codeGenSettings.DirectoryStructureConfigMap[entry.Key] = entry.Value;
                added = true;
            }

            if (!added) return;

            EditorUtility.SetDirty(codeGenSettings);
        }

        protected bool ContainsFolderType(List<FolderEVO> folders, FolderEVO.FolderType folderType)
        {
            if (folders == null) return false;

            foreach (FolderEVO folder in folders)
            {
                if (folder.Type == folderType) return true;
                if (ContainsFolderType(folder.SubFolders, folderType)) return true;
            }

            return false;
        }

        protected FolderEVO FindFolderByName(List<FolderEVO> folders, string folderName)
        {
            if (folders == null) return null;

            foreach (FolderEVO folder in folders)
            {
                if (string.Equals(folder.FolderName, folderName, StringComparison.OrdinalIgnoreCase)) return folder;

                FolderEVO found = FindFolderByName(folder.SubFolders, folderName);
                if (found != null) return found;
            }

            return null;
        }

        protected FolderEVO FindFolderByType(List<FolderEVO> folders, FolderEVO.FolderType folderType)
        {
            if (folders == null) return null;

            foreach (FolderEVO folder in folders)
            {
                if (folder.Type == folderType) return folder;

                FolderEVO found = FindFolderByType(folder.SubFolders, folderType);
                if (found != null) return found;
            }

            return null;
        }
    }
}
#endif