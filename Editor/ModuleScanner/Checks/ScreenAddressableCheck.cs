#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FlowIoC.Editor.Addressables;
using FlowIoC.Editor.Modules;
using FlowIoC.Editor.Root;
using FlowIoC.ScreenModule.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// A screen whose context loads it by address has an Addressables entry at that address.
    ///
    /// Without one nothing complains until the screen is opened: the load fails at runtime with
    /// "Failed to load addressable asset", usually in the middle of a flow and long after whoever
    /// made the screen has moved on. Create Module registers the prefab as it writes it, but a
    /// generator that could not work out the path, a prefab moved or renamed by hand, or a screen
    /// written before its module existed all end up unregistered - HiveCanvas had five of them.
    ///
    /// The declaration is read off the compiled context, the way the Root's inspector reads it,
    /// so no scene is opened. A module that is not compiled - one shipped under Modules~ - has
    /// nothing to read and is skipped.
    ///
    /// The fix registers the prefab of the same name in the module's own folder, as the
    /// generator would have. A module with no such prefab is Manual: which prefab the address
    /// means is not the scanner's to guess.
    /// </summary>
    internal class ScreenAddressableCheck : IModuleCheck
    {
        private readonly Func<ModuleTargetEVO, IEnumerable<ScreenCVO>> _declarationsOf;
        private readonly Func<string, bool> _isAddressed;
        private readonly Func<ModuleTargetEVO, string, string> _prefabOf;
        private readonly Action<string, string> _register;

        internal ScreenAddressableCheck() : this(DeclarationsOf, IsAddressed, PrefabOf, Register)
        {
        }

        internal ScreenAddressableCheck(
            Func<ModuleTargetEVO, IEnumerable<ScreenCVO>> declarationsOf,
            Func<string, bool> isAddressed,
            Func<ModuleTargetEVO, string, string> prefabOf,
            Action<string, string> register)
        {
            _declarationsOf = declarationsOf;
            _isAddressed = isAddressed;
            _prefabOf = prefabOf;
            _register = register;
        }

        public string Id => "screen-addressable";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            List<string> missing = MissingAddresses(module);

            if (missing.Count == 0)
                return FindingEVO.Ok(Id, "Screen addressable");

            string address = missing[0];
            string prefab = _prefabOf(module, address);

            if (string.IsNullOrEmpty(prefab))
            {
                return FindingEVO.Manual(
                    Id,
                    $"The screen loads by the address \"{address}\", and no Addressables entry has it. No prefab "
                    + $"named {address} sits in the module to register - mark the screen's prefab addressable "
                    + "with that address, or correct the address in the context's ScreenCVO.",
                    module?.AssetPath);
            }

            return FindingEVO.Fixable(
                Id,
                $"The screen loads by the address \"{address}\", and no Addressables entry has it, so opening it "
                + $"fails at runtime. Fix registers {Path.GetFileName(prefab)} under that address.",
                prefab);
        }

        public void Fix(ModuleTargetEVO module)
        {
            foreach (string address in MissingAddresses(module))
            {
                string prefab = _prefabOf(module, address);

                if (!string.IsNullOrEmpty(prefab))
                    _register(address, prefab);
            }
        }

        private List<string> MissingAddresses(ModuleTargetEVO module)
        {
            var missing = new List<string>();

            if (module == null || module.Kind != ModuleKind.Screen)
                return missing;

            foreach (ScreenCVO declaration in _declarationsOf(module) ?? Enumerable.Empty<ScreenCVO>())
            {
                if (declaration == null)
                    continue;

                ScreenLoadCVO load = declaration.Load;

                if (load.Kind != ScreenLoadType.Addressable || !load.IsValid)
                    continue;

                if (!_isAddressed(load.Key) && !missing.Contains(load.Key))
                    missing.Add(load.Key);
            }

            return missing;
        }

        /// <summary>
        /// The declarations of the screen contexts compiled into the module's own assembly. A
        /// context that throws while it is read is left to the Root's inspector, which reports it.
        /// </summary>
        private static IEnumerable<ScreenCVO> DeclarationsOf(ModuleTargetEVO module)
        {
            if (module == null || string.IsNullOrEmpty(module.ExpectedAssemblyName))
                yield break;

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetName().Name == module.ExpectedAssemblyName);

            if (assembly == null)
                yield break;

            var declarations = new ScreenSubContextDeclarations();

            foreach (Type type in SafeTypes(assembly))
            {
                if (declarations.TryRead(type, out ScreenCVO declaration, out _))
                    yield return declaration;
            }
        }

        private static IEnumerable<Type> SafeTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        private static bool IsAddressed(string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);

            if (settings == null)
                return false;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry != null && entry.address == address)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The prefab named after the address inside the module's folder, outside any Resources
        /// folder - a prefab there is loaded by path and is not the one an address means.
        /// </summary>
        private static string PrefabOf(ModuleTargetEVO module, string address)
        {
            if (module == null || string.IsNullOrEmpty(module.AssetPath))
                return null;

            var entries = new ScreenAddressableEntries();

            foreach (string guid in AssetDatabase.FindAssets($"{address} t:Prefab", new[] {module.AssetPath}))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetFileNameWithoutExtension(path) == address && entries.IsAddressable(path))
                    return path;
            }

            return null;
        }

        private static void Register(string address, string prefabPath)
        {
            ScreenAddressableEntry entry = new ScreenAddressableEntries().For(address);
            entry.AssetPath = prefabPath;

            new ScreenAddressables().Register(entry);
            AssetDatabase.SaveAssets();
        }
    }
}

#endif
