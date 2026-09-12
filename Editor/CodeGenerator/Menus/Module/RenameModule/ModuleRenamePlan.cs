#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.Addressables;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// Everything one press of Rename will do, worked out before anything is written.
    ///
    /// The picked module and the nested modules that carry its name are the modules that change;
    /// for each, the assemblies its folders declare, the namespace its files sit in, the classes
    /// Create Module named after it, the prefabs and scenes in its own folders that carry its stem,
    /// and its Flow Console channel. What is left alone is written down with a reason, so the
    /// preview says as much about what stays as about what moves. A blocker is anything that would
    /// leave the project unable to compile, and the window keeps the button off while there is one.
    ///
    /// Nothing here touches Unity. The disk and the domain come in through RenameProjectLookups,
    /// which is what lets a test describe a project in a dictionary and read the whole plan back.
    /// </summary>
    internal class ModuleRenamePlan
    {
        private const string ASMDEF = ".asmdef";
        private const string SOURCE = ".cs";
        private const string PREFAB = ".prefab";
        private const string SCENE = ".unity";
        private const string SETTINGS = ".csproj.DotSettings";

        private static readonly FolderEVO.FolderType[] AssetFolders =
        {
            FolderEVO.FolderType.Prefabs, FolderEVO.FolderType.Scenes, FolderEVO.FolderType.Resources
        };

        private readonly RenameProjectLookups _lookups;
        private readonly ModuleStems _stems = new ModuleStems();
        private readonly RenameFollowers _followers = new RenameFollowers();
        private readonly ModuleAssemblyName _assemblyNames = new ModuleAssemblyName();
        private readonly ModuleNamespaceBuilder _namespaces = new ModuleNamespaceBuilder();
        private readonly ScreenAddressableEntries _addresses = new ScreenAddressableEntries();

        internal ModuleRenamePlan() : this(new RenameProjectLookups())
        {
        }

        internal ModuleRenamePlan(RenameProjectLookups lookups)
        {
            _lookups = lookups;
        }

        /// <summary>
        /// <paramref name="typedStem"/> is what the field holds - the name without its kind suffix.
        /// <paramref name="ticked"/> answers, by module name, whether a carrier keeps its tick.
        /// </summary>
        internal ModuleRenamePlanEVO Build(ModuleTreeRowEVO<ModulePickEVO> picked, string typedStem, Func<string, bool> ticked)
        {
            var plan = new ModuleRenamePlanEVO();

            string stem = typedStem?.Trim() ?? string.Empty;
            string invalid = _stems.WhyInvalid(stem);

            if (invalid != null)
            {
                plan.Blockers.Add(invalid);

                return plan;
            }

            string newName = _stems.NameFor(stem, picked.Row.Kind);

            if (newName == picked.Row.Name)
            {
                plan.Blockers.Add("That is the name it has now.");

                return plan;
            }

            AddModules(plan, picked, newName, ticked);

            foreach (ModuleRenameEVO module in plan.Modules)
            {
                AddAssemblies(plan, module);
                AddClasses(plan, module);
                AddAssets(plan, module);
                AddScreenAddress(plan, module);
                AddChannel(plan, module);
                CheckFolder(plan, module);
            }

            AddSettingsFiles(plan, picked);
            CheckAssemblies(plan);
            CheckClasses(plan, picked);

            return plan;
        }

        private void AddModules(
            ModuleRenamePlanEVO plan, ModuleTreeRowEVO<ModulePickEVO> picked, string newName, Func<string, bool> ticked)
        {
            var newNames = new Dictionary<ModuleTreeRowEVO<ModulePickEVO>, string> {[picked] = newName};

            plan.Modules.Add(Module(picked, newName, newNames, true));

            foreach (FollowerRenameEVO follower in _followers.Of(picked, newName, ticked))
            {
                plan.Carriers.Add(follower);

                if (!follower.Follows)
                {
                    plan.Kept.Add(follower.OldName + " " + follower.Reason);

                    continue;
                }

                newNames[follower.Row] = follower.NewName;
                plan.Modules.Add(Module(follower.Row, follower.NewName, newNames, false));
            }
        }

        private ModuleRenameEVO Module(
            ModuleTreeRowEVO<ModulePickEVO> row, string newName,
            Dictionary<ModuleTreeRowEVO<ModulePickEVO>, string> newNames, bool isPicked)
        {
            return new ModuleRenameEVO
            {
                Row = row,
                Kind = row.Row.Kind,
                IsPicked = isPicked,
                OldName = row.Row.Name,
                NewName = newName,
                OldStem = _stems.FullStem(row.Row.Name),
                NewStem = _stems.FullStem(newName),
                OldPath = row.Row.Path,
                OldNamespace = Namespace(row, r => r.Row.Name),
                NewNamespace = Namespace(row, r => newNames.TryGetValue(r, out string renamed) ? renamed : r.Row.Name)
            };
        }

        /// <summary>The chain from the top module down, the way ModuleNamespaceBuilder reads it: nearest ancestor first.</summary>
        private string Namespace(ModuleTreeRowEVO<ModulePickEVO> row, Func<ModuleTreeRowEVO<ModulePickEVO>, string> nameOf)
        {
            var ancestors = new List<string>();

            for (ModuleTreeRowEVO<ModulePickEVO> parent = row.Parent; parent != null; parent = parent.Parent)
                ancestors.Add(nameOf(parent));

            return _namespaces.Build(ancestors, nameOf(row));
        }

        /// <summary>
        /// The module's own asmdef, its Shared and its Signals - each renamed only when its file is
        /// named what the old folder name derives, the same rule Module Scanner checks it against.
        /// </summary>
        private void AddAssemblies(ModuleRenamePlanEVO plan, ModuleRenameEVO module)
        {
            string oldBase = _assemblyNames.From(module.OldName);
            string newBase = _assemblyNames.From(module.NewName);

            AddAssembly(plan, module, module.OldPath, oldBase, newBase);

            AddAssembly(plan, module, Folder(module, FolderEVO.FolderType.Shared),
                oldBase + SharedAssemblyDefinition.ASSEMBLY_SUFFIX, newBase + SharedAssemblyDefinition.ASSEMBLY_SUFFIX);

            AddAssembly(plan, module, Folder(module, FolderEVO.FolderType.PublicSignals),
                oldBase + SignalsAssemblyDefinition.ASSEMBLY_SUFFIX, newBase + SignalsAssemblyDefinition.ASSEMBLY_SUFFIX);
        }

        private void AddAssembly(ModuleRenamePlanEVO plan, ModuleRenameEVO module, string folder, string expectedOld, string expectedNew)
        {
            if (string.IsNullOrEmpty(folder)) return;

            foreach (string path in _lookups.FilesIn(folder))
            {
                if (!path.EndsWith(ASMDEF, StringComparison.OrdinalIgnoreCase)) continue;

                string name = Path.GetFileNameWithoutExtension(path);

                if (name == expectedOld)
                    plan.Assemblies.Add(new AssemblyRenameEVO {AsmdefPath = path, OldName = name, NewName = expectedNew});
                else
                    plan.Kept.Add("Assembly " + name + " keeps its name: it is not the one " + module.OldName + " derives.");
            }
        }

        /// <summary>
        /// The folders Create Module writes a module's named classes into. A screen's View and
        /// Mediator are named after the screen too, so a screen module adds ViewsMediators.
        /// </summary>
        private void AddClasses(ModuleRenamePlanEVO plan, ModuleRenameEVO module)
        {
            var folders = new List<string>
            {
                Folder(module, FolderEVO.FolderType.RootsAndContexts),
                Folder(module, FolderEVO.FolderType.Signals),
                Folder(module, FolderEVO.FolderType.PublicSignals)
            };

            if (module.Kind == ModuleKind.Screen)
                folders.Add(Folder(module, FolderEVO.FolderType.ViewsAndMediators));

            var finder = new GeneratedSetFinder(
                folder => _lookups.FilesIn(folder).Where(path => path.EndsWith(SOURCE, StringComparison.OrdinalIgnoreCase)),
                _lookups.ReadText);

            plan.Classes.AddRange(finder.In(folders, module.OldStem, module.NewStem, plan.Kept));
        }

        /// <summary>
        /// The prefabs and scenes in the module's own Prefabs, Scenes and Resources folders whose
        /// name starts with the stem - the test scene, the screen prefab, a Root prefab named after
        /// its class. RenameAsset keeps a GUID, so nothing that points at them notices.
        /// </summary>
        private void AddAssets(ModuleRenamePlanEVO plan, ModuleRenameEVO module)
        {
            foreach (FolderEVO.FolderType type in AssetFolders)
            {
                string folder = Folder(module, type);
                if (string.IsNullOrEmpty(folder)) continue;

                foreach (string path in _lookups.FilesIn(folder))
                {
                    if (!path.EndsWith(PREFAB, StringComparison.OrdinalIgnoreCase)
                        && !path.EndsWith(SCENE, StringComparison.OrdinalIgnoreCase)) continue;

                    string file = Path.GetFileNameWithoutExtension(path);
                    if (!_stems.Carries(file, module.OldStem)) continue;

                    plan.Assets.Add(new AssetRenameEVO
                    {
                        Path = path,
                        NewPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                            _stems.Carried(file, module.OldStem, module.NewStem) + Path.GetExtension(path))
                    });
                }
            }
        }

        /// <summary>
        /// A screen's prefab is Prefabs/&lt;stem&gt;.prefab and its address is the stem, both written
        /// by the generator. Whether the address is still that is Addressables' answer and is read
        /// at run time; here the entry is planned when the prefab is where the generator put it.
        /// </summary>
        private void AddScreenAddress(ModuleRenamePlanEVO plan, ModuleRenameEVO module)
        {
            if (module.Kind != ModuleKind.Screen) return;

            string prefabs = Folder(module, FolderEVO.FolderType.Prefabs);
            string expected = prefabs == null ? null : Normalize(Path.Combine(prefabs, module.OldStem + PREFAB));

            AssetRenameEVO prefab = expected == null
                ? null
                : plan.Assets.FirstOrDefault(asset => string.Equals(Normalize(asset.Path), expected, StringComparison.OrdinalIgnoreCase));

            if (prefab == null)
            {
                plan.Kept.Add("No Prefabs/" + module.OldStem + PREFAB + " in " + module.OldName
                              + ": the screen's prefab and its address keep their names.");

                return;
            }

            plan.ScreenAddresses.Add(new ScreenAddressRenameEVO
            {
                PrefabPath = prefab.Path,
                NewPrefabPath = prefab.NewPath,
                OldAddress = module.OldStem,
                NewAddress = module.NewStem,
                OldGroup = _addresses.For(module.OldStem).GroupName,
                NewGroup = _addresses.For(module.NewStem).GroupName
            });
        }

        /// <summary>A channel is named after the module folder; a test module has none.</summary>
        private void AddChannel(ModuleRenamePlanEVO plan, ModuleRenameEVO module)
        {
            if (module.Kind == ModuleKind.Test) return;

            plan.Channels.Add(new ChannelRenameEVO {OldName = module.OldName, NewName = module.NewName});
        }

        private void CheckFolder(ModuleRenamePlanEVO plan, ModuleRenameEVO module)
        {
            string parent = Path.GetDirectoryName(module.OldPath);

            if (parent != null && _lookups.DirectoryExists(Normalize(Path.Combine(parent, module.NewName))))
                plan.Blockers.Add("A folder named " + module.NewName + " is already in " + Path.GetFileName(parent) + ".");
        }

        /// <summary>
        /// One line per settings file at the project root: the renamed assemblies' files take new
        /// names, and every module still inside the folder under its old name has its paths
        /// rewritten in place - its file names the parent folder.
        /// </summary>
        private void AddSettingsFiles(ModuleRenamePlanEVO plan, ModuleTreeRowEVO<ModulePickEVO> picked)
        {
            foreach (AssemblyRenameEVO assembly in plan.Assemblies)
                plan.SettingsFiles.Add(assembly.OldName + SETTINGS + " → " + assembly.NewName + SETTINGS);

            foreach (ModuleTreeRowEVO<ModulePickEVO> row in picked.Descendants)
            {
                if (plan.Modules.Any(module => module.Row == row)) continue;

                plan.SettingsFiles.Add(_assemblyNames.From(row.Row.Name) + SETTINGS + " - rewritten, the paths inside change");
            }
        }

        /// <summary>
        /// A new name another asmdef already declares blocks: two asmdefs with one name stop the
        /// project compiling. An old name the package ships as a ready-made module only warns, but
        /// it is worth the line - the installer recognises an installed module by its assembly
        /// name, so the renamed module counts as the game's own and the original is offered again.
        /// </summary>
        private void CheckAssemblies(ModuleRenamePlanEVO plan)
        {
            var taken = new HashSet<string>(_lookups.AllAssemblyNames(), StringComparer.Ordinal);
            var leaving = new HashSet<string>(plan.Assemblies.Select(assembly => assembly.OldName), StringComparer.Ordinal);
            var shipped = new HashSet<string>(_lookups.ShippedAssemblyNames(), StringComparer.Ordinal);

            foreach (AssemblyRenameEVO assembly in plan.Assemblies)
            {
                if (taken.Contains(assembly.NewName) && !leaving.Contains(assembly.NewName))
                    plan.Blockers.Add("An assembly named " + assembly.NewName + " already exists.");

                if (shipped.Contains(assembly.OldName))
                    plan.Warnings.Add(assembly.OldName + " is the assembly of a module the package ships. Renamed, the "
                                      + "installer will not see it as installed and will offer the original again - "
                                      + "a second copy, not an update.");
            }
        }

        /// <summary>
        /// Two collisions, two answers. An old name that is also a type somewhere outside the
        /// module is skipped with a warning: the rename is a text pass over the whole project, and
        /// it would reach the other type's every mention. A new name already declared inside the
        /// module blocks, because the compile that follows would find two; declared elsewhere it
        /// only warns, since two assemblies may each have a type of that name.
        /// </summary>
        private void CheckClasses(ModuleRenamePlanEVO plan, ModuleTreeRowEVO<ModulePickEVO> picked)
        {
            var own = new HashSet<string>(OwnAssemblies(picked), StringComparer.Ordinal);

            foreach (ClassRenameEVO file in plan.Classes)
            {
                for (int index = file.Identifiers.Count - 1; index >= 0; index--)
                {
                    IdentifierRenameEVO identifier = file.Identifiers[index];

                    TypeHomeEVO elsewhere = _lookups.TypesNamed(identifier.Old)
                        .FirstOrDefault(home => !own.Contains(home.AssemblyName));

                    if (elsewhere != null)
                    {
                        plan.Warnings.Add(identifier.Old + " keeps its name: " + elsewhere.FullName + " in "
                                          + elsewhere.AssemblyName + " is called that too, and a rename would reach it.");
                        file.Identifiers.RemoveAt(index);

                        if (Path.GetFileNameWithoutExtension(file.Path) == identifier.Old) file.NewPath = null;

                        continue;
                    }

                    foreach (TypeHomeEVO home in _lookups.TypesNamed(identifier.New))
                    {
                        if (own.Contains(home.AssemblyName))
                            plan.Blockers.Add(identifier.New + " already exists in " + home.AssemblyName + ".");
                        else
                            plan.Warnings.Add(identifier.New + " is also the name of " + home.FullName + " in " + home.AssemblyName + ".");
                    }
                }
            }
        }

        /// <summary>The assemblies of the picked module and everything inside it, by the names their folders derive.</summary>
        private IEnumerable<string> OwnAssemblies(ModuleTreeRowEVO<ModulePickEVO> picked)
        {
            foreach (ModuleTreeRowEVO<ModulePickEVO> row in new[] {picked}.Concat(picked.Descendants))
            {
                string assembly = _assemblyNames.From(row.Row.Name);

                yield return assembly;
                yield return assembly + SharedAssemblyDefinition.ASSEMBLY_SUFFIX;
                yield return assembly + SignalsAssemblyDefinition.ASSEMBLY_SUFFIX;
            }
        }

        private string Folder(ModuleRenameEVO module, FolderEVO.FolderType type) =>
            _lookups.FolderOf(module.OldPath, module.Kind, type);

        private static string Normalize(string path) => path.Replace('\\', '/');
    }
}
#endif
