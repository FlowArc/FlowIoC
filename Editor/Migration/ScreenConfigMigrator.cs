#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.Signals;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.Editor.Root;
using FlowIoC.ScreenModule.Data;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Migration
{
    /// <summary>
    /// Turns the CD_Screen assets a project still has into screen contexts. Runs once per session
    /// from the migration bootstrap, on an editor tick where the AssetDatabase is writable.
    ///
    /// The assets are deleted only after the owner agrees, and only the ones whose context was
    /// generated here: a reported one still carries the values the owner has to copy.
    /// </summary>
    internal class ScreenConfigMigrator
    {
        private const string CompletedKey = "FlowIoC_ScreenConfigMigration_Completed";
        private const string Prefix = "<color=cyan>[FlowIoC]</color> ";

        private readonly ScreenConfigMigrationPlan _plan = new ScreenConfigMigrationPlan();
        private readonly ScreenContextTemplate _template = new ScreenContextTemplate();

        internal void MigrateIfNeeded()
        {
            if (SessionState.GetBool(CompletedKey, false)) return;
            SessionState.SetBool(CompletedKey, true);

            Migrate();
        }

        private void Migrate()
        {
            string[] guids = AssetDatabase.FindAssets("t:CD_Screen");
            if (guids.Length == 0) return;

            List<string> generated = new List<string>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                CD_Screen asset = AssetDatabase.LoadAssetAtPath<CD_Screen>(assetPath);
                if (asset == null) continue;

                LegacyScreenConfig config = Read(asset, assetPath);
                MonoScript viewScript = FindScript(config.ViewTypeName);
                string viewScriptPath = viewScript == null ? null : AssetDatabase.GetAssetPath(viewScript);

                ScreenConfigMigrationStep probe = _plan.For(config, viewScriptPath, contextExists: false);
                bool contextExists = probe.ContextPath != null && File.Exists(ToDiskPath(probe.ContextPath));
                ScreenConfigMigrationStep step = _plan.For(config, viewScriptPath, contextExists);

                switch (step.Action)
                {
                    case ScreenConfigMigrationAction.Skip:
                        Debug.LogWarning(Prefix + step.Reason);
                        break;

                    case ScreenConfigMigrationAction.ReportBlock:
                        Debug.LogWarning(Prefix + step.Reason + "\n\n" + _template.RenderScreenBlock(step.Settings));
                        break;

                    case ScreenConfigMigrationAction.GenerateContext:
                        Generate(step, viewScript.GetClass()?.Namespace);
                        generated.Add(assetPath);
                        break;
                }
            }

            if (generated.Count == 0) return;

            AssetDatabase.Refresh();
            OfferToDelete(generated);
        }

        private static LegacyScreenConfig Read(CD_Screen asset, string assetPath)
        {
            return new LegacyScreenConfig
            {
                AssetPath = assetPath,
                ViewTypeName = asset.ViewTypeName,
                MediatorTypeName = asset.MediatorTypeName,
                Layer = asset.DefaultLayer,
                Tag = asset.Tag,
                LoadType = asset.LoadType,
                WasDirectPrefab = asset.WasDirectPrefab,
                AddressableKey = asset.AddressableKey,
                ResourcePath = asset.ResourcePath,
                HasShowAnimation = asset.HasShowAnimation,
                HasHideAnimation = asset.HasHideAnimation
            };
        }

        private static MonoScript FindScript(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            return AssetDatabase.FindAssets($"t:MonoScript {typeName}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(script => script != null && script.GetClass()?.Name == typeName);
        }

        private void Generate(ScreenConfigMigrationStep step, string viewNamespace)
        {
            string diskPath = ToDiskPath(step.ContextPath);
            string contextNamespace = NamespaceUtility.GetFullNamespaceForFile(diskPath);

            Directory.CreateDirectory(Path.GetDirectoryName(diskPath));
            File.WriteAllText(diskPath, _template.Render(
                contextNamespace, step.ContextName, step.ViewName, step.MediatorName, viewNamespace, step.Settings));

            // The generator binds the screen's own holder in its context; a migrated screen that has
            // one gets the same line.
            string signalsName = step.ContextName.Replace("Context", "Signals");
            Type signals = TypeCache.GetTypesDerivedFrom<ISignalHolder>().FirstOrDefault(type => type.Name == signalsName);
            if (signals != null)
                CodeGeneratorUtils.BindSignalsInContext(diskPath, signals.Name, signals.Namespace);

            string contextFullName = $"{contextNamespace}.{step.ContextName}";
            string rootPrefab = FindParentRootPrefab(step.ContextPath);

            if (rootPrefab != null && new RootPrefabSubContexts().Add(rootPrefab, contextFullName, step.ContextName))
            {
                Debug.Log(Prefix
                          + $"{step.ContextName} was generated at '{step.ContextPath}' and added to the sub-contexts of '{rootPrefab}'.");
            }
            else
            {
                Debug.LogWarning(Prefix
                                 + $"{step.ContextName} was generated at '{step.ContextPath}', but no Root prefab was found under the parent module's Prefabs folder. "
                                 + $"Select the Root that hosts this screen, press Add Sub Context in its inspector, pick {step.ContextName} and leave Auto Setup ticked.");
            }
        }

        /// <summary>
        /// A screen module sits in its parent's zScreenModules folder, so the parent is two folders
        /// above the screen module, and its Root prefab is the one under the parent's Prefabs
        /// folder carrying a RootBase.
        /// </summary>
        private static string FindParentRootPrefab(string contextAssetPath)
        {
            int scripts = contextAssetPath.IndexOf("/Scripts/", StringComparison.Ordinal);
            if (scripts < 0) return null;

            string screenModule = contextAssetPath.Substring(0, scripts);
            string parentModule = Path.GetDirectoryName(Path.GetDirectoryName(screenModule))?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parentModule)) return null;

            string prefabs = parentModule + "/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabs)) return null;

            return AssetDatabase.FindAssets("t:Prefab", new[] {prefabs})
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<RootBase>() != null);
        }

        private static void OfferToDelete(List<string> generated)
        {
            string list = string.Join("\n", generated);

            // A dialog in a headless run would block forever; the assets stay and the log says so.
            if (Application.isBatchMode)
            {
                Debug.Log(Prefix + $"{generated.Count} screen config asset(s) were turned into contexts and can be deleted:\n{list}");
                return;
            }

            bool delete = EditorUtility.DisplayDialog(
                "FlowIoC - screen configs migrated",
                $"{generated.Count} screen config asset(s) were turned into screen contexts:\n\n{list}\n\n"
                + "The contexts carry everything the assets did. Delete the assets?",
                "Delete", "Keep");

            if (!delete) return;

            foreach (string assetPath in generated)
            {
                string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                AssetDatabase.DeleteAsset(assetPath);

                if (folder != null && AssetDatabase.IsValidFolder(folder)
                                   && Directory.GetFileSystemEntries(ToDiskPath(folder)).All(entry => entry.EndsWith(".meta")))
                    AssetDatabase.DeleteAsset(folder);
            }

            AssetDatabase.SaveAssets();
        }

        private static string ToDiskPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath);
        }
    }
}

#endif
