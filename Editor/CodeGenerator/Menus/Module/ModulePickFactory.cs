#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.Modules;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// Turns the module index into the picks a generator offers as a parent. The index holds
    /// every module and its folder; which module each one lives in is read off those folders
    /// here, so the picker gets a flat list it can hand to ModuleTree like any other.
    /// </summary>
    internal class ModulePickFactory
    {
        internal List<ModulePickEVO> From(ModuleRegistry registry)
        {
            var picks = new List<ModulePickEVO>();

            foreach (ModuleDescriptorEVO module in registry.Modules)
            {
                string assetPath = registry.PathOf(module);

                // A module the AssetDatabase no longer has a folder for is not offered: the
                // generator would write into a path that is not there.
                if (string.IsNullOrEmpty(assetPath)) continue;

                picks.Add(new ModulePickEVO
                {
                    Name = module.Name,
                    Kind = module.Kind,
                    ParentName = registry.AncestorsOf(module).FirstOrDefault()?.Name,
                    Path = ToAbsolutePath(assetPath),
                    Descriptor = module
                });
            }

            return picks;
        }

        /// <summary>
        /// The inverse of ModuleAssetPathResolver.ToAssetPath, kept byte-for-byte identical in
        /// separator style to what Directory.GetDirectories hands back (forward slashes through
        /// Application.dataPath, platform separators after). ModuleGenerator still compares a
        /// pick against Path.Combine(Application.dataPath, "Modules") by plain string equality,
        /// so drifting the format here would silently break parent-module detection downstream.
        /// </summary>
        private string ToAbsolutePath(string assetPath)
        {
            string relative = assetPath.Substring("Assets".Length).Replace('/', Path.DirectorySeparatorChar);

            return Application.dataPath + relative;
        }
    }
}

#endif
