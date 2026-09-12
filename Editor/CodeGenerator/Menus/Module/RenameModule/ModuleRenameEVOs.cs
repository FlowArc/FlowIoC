#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>One identifier of the generated set: the class as declared, and its name after the rename.</summary>
    internal class IdentifierRenameEVO
    {
        internal string Old { get; set; }
        internal string New { get; set; }
    }

    /// <summary>One file of the generated set. NewPath is null when the file keeps its name because its own identifier was skipped.</summary>
    internal class ClassRenameEVO
    {
        internal string Path { get; set; }
        internal string NewPath { get; set; }
        internal List<IdentifierRenameEVO> Identifiers { get; } = new List<IdentifierRenameEVO>();
    }

    /// <summary>One module that changes name: the one picked, or a follower inside it.</summary>
    internal class ModuleRenameEVO
    {
        internal ModuleTreeRowEVO<ModulePickEVO> Row { get; set; }
        internal ModuleKind Kind { get; set; }
        internal bool IsPicked { get; set; }

        /// <summary>Folder names.</summary>
        internal string OldName { get; set; }

        internal string NewName { get; set; }

        /// <summary>Full stems - the folder name without Module.</summary>
        internal string OldStem { get; set; }

        internal string NewStem { get; set; }

        /// <summary>The folder, absolute, where it is now. Where it will be is its parent's folder plus NewName.</summary>
        internal string OldPath { get; set; }

        /// <summary>The namespace root the module's files declare - "Modules.CounterModule.CounterTestModule".</summary>
        internal string OldNamespace { get; set; }

        internal string NewNamespace { get; set; }
    }

    /// <summary>One asmdef that changes name, with the file it is in.</summary>
    internal class AssemblyRenameEVO
    {
        internal string AsmdefPath { get; set; }
        internal string OldName { get; set; }
        internal string NewName { get; set; }
    }

    /// <summary>One prefab, scene or resource in the module's own folders that carries the stem.</summary>
    internal class AssetRenameEVO
    {
        internal string Path { get; set; }
        internal string NewPath { get; set; }
    }

    /// <summary>A screen module's prefab and the Addressables entry the generator gave it.</summary>
    internal class ScreenAddressRenameEVO
    {
        internal string PrefabPath { get; set; }
        internal string NewPrefabPath { get; set; }
        internal string OldAddress { get; set; }
        internal string NewAddress { get; set; }
        internal string OldGroup { get; set; }
        internal string NewGroup { get; set; }
    }

    /// <summary>A Flow Console channel. Channels are named after module folders, so old and new are the folder names.</summary>
    internal class ChannelRenameEVO
    {
        internal string OldName { get; set; }
        internal string NewName { get; set; }
    }

    /// <summary>Where a loaded type with a given simple name lives, for the collision checks.</summary>
    internal class TypeHomeEVO
    {
        internal string FullName { get; set; }
        internal string AssemblyName { get; set; }
    }

    /// <summary>
    /// Everything one press will do, computed before anything is written. The window draws it,
    /// the renamer runs it, and a test reads it.
    /// </summary>
    internal class ModuleRenamePlanEVO
    {
        /// <summary>The picked module first, then every follower that follows, in tree order.</summary>
        internal List<ModuleRenameEVO> Modules { get; } = new List<ModuleRenameEVO>();

        /// <summary>Every carrier, following or not, for the ticks.</summary>
        internal List<FollowerRenameEVO> Carriers { get; } = new List<FollowerRenameEVO>();

        internal List<AssemblyRenameEVO> Assemblies { get; } = new List<AssemblyRenameEVO>();
        internal List<ClassRenameEVO> Classes { get; } = new List<ClassRenameEVO>();
        internal List<AssetRenameEVO> Assets { get; } = new List<AssetRenameEVO>();
        internal List<ScreenAddressRenameEVO> ScreenAddresses { get; } = new List<ScreenAddressRenameEVO>();
        internal List<ChannelRenameEVO> Channels { get; } = new List<ChannelRenameEVO>();

        /// <summary>The settings files at the project root, one line each: renamed, or rewritten in place.</summary>
        internal List<string> SettingsFiles { get; } = new List<string>();

        internal List<string> Kept { get; } = new List<string>();
        internal List<string> Warnings { get; } = new List<string>();
        internal List<string> Blockers { get; } = new List<string>();

        internal ModuleRenameEVO Picked => Modules.Count == 0 ? null : Modules[0];
        internal bool CanRun => Blockers.Count == 0 && Picked != null;
    }
}
#endif