#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModulePanels;
using Modules.AbTestFlowModule.Data.UnityObjects;
using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.Enums;
using UnityEditor;
using UnityEngine;

namespace Modules.AbTestFlowModule.Editor
{
    /// <summary>
    /// The tests, authored here instead of in the Inspector of an asset the author first has to
    /// find: every CD_AbTests in the project, each test with its id, version, rollout and groups,
    /// each group with its overrides, and the validator's word beside the test it is about rather
    /// than in the Console. Every field is the asset's own serialized property, so undo, dirtying
    /// and saving are Unity's; the panel edits what the Inspector edits and nothing a Model holds.
    ///
    /// Selector is the other panel: this one shapes the tests, that one picks the group this
    /// machine plays. They are used at different moments, so they are two windows.
    /// </summary>
    internal class AbTestEditorPanel : ModulePanel
    {
        private const string MENU_PATH = "Tools/FlowIoC-Modules/AB Test/Editor";

        private const int MENU_PRIORITY = -1080;

        private readonly AbTestAuthoringTools _tools = new AbTestAuthoringTools();

        /// <summary>One SerializedObject per asset, kept while the window lives so a field keeps its focus between repaints.</summary>
        private readonly Dictionary<CD_AbTests, SerializedObject> _serialized = new Dictionary<CD_AbTests, SerializedObject>();

        /// <summary>
        /// A change to a list's shape - a test added, a group removed - waits until the rows are
        /// drawn: the rows are walked by index, and a list that changes under the walk draws a
        /// row that is gone.
        /// </summary>
        private readonly Queue<Action> _pending = new Queue<Action>();

        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        private static void Open() => ModulePanelWindow.Open<AbTestEditorPanel>();

        public override string Title => "AB Test Editor";

        public override string Module => "AbTestFlowModule";

        public override string Subtitle => "Author the tests without hunting for the asset";

        public override FlowRole Role => FlowRole.Service;

        public override string HelpPage => "A/B Test";

        public override void Draw(ModulePanelPainter painter)
        {
            List<CD_AbTests> assets = Assets();

            if (assets.Count == 0)
            {
                painter.Heading("Tests");
                painter.Note("No CD_AbTests asset in the project yet. Create one, then file it on AbTestFlowServiceRoot's adapter under CD_AbTests.");
                painter.Actions(new ModulePanelAction("Create CD_AbTests", CreateAsset));
                return;
            }

            foreach (CD_AbTests asset in assets)
                DrawAsset(painter, asset);
        }

        private void DrawAsset(ModulePanelPainter painter, CD_AbTests asset)
        {
            SerializedObject serialized = Serialized(asset);
            serialized.Update();

            Dictionary<string, List<AbTestValidationVO>> messages = _tools.Messages(asset);

            painter.Heading(asset.name + " - " + AssetDatabase.GetAssetPath(asset));
            painter.Actions(
                new ModulePanelAction("Add test", () => Defer(() => _tools.AddTest(serialized))),
                new ModulePanelAction("Select asset", () => Select(asset)));

            DrawMessages(painter, messages, string.Empty);

            SerializedProperty tests = _tools.Tests(serialized);

            for (int index = 0; index < tests.arraySize; index++)
                DrawTest(painter, serialized, tests, index, messages);

            while (_pending.Count > 0)
                _pending.Dequeue()();

            serialized.ApplyModifiedProperties();
            painter.Space();
        }

        private void DrawTest(ModulePanelPainter painter, SerializedObject serialized, SerializedProperty tests, int index,
            Dictionary<string, List<AbTestValidationVO>> messages)
        {
            SerializedProperty test = tests.GetArrayElementAtIndex(index);
            SerializedProperty id = test.FindPropertyRelative(AbTestAuthoringTools.ID);
            string label = string.IsNullOrEmpty(id.stringValue) ? "(no id)" : id.stringValue;

            painter.Space();
            painter.Heading("Test " + (index + 1) + " - " + label);

            painter.Property(id, "Id");
            painter.Property(test.FindPropertyRelative(AbTestAuthoringTools.VERSION), "Version");
            painter.Property(test.FindPropertyRelative(AbTestAuthoringTools.IS_ACTIVE), "Is active");
            painter.Property(test.FindPropertyRelative(AbTestAuthoringTools.ROLLOUT_PERCENT), "Rollout %");

            painter.Actions(
                new ModulePanelAction("Raise version", () => _tools.RaiseVersion(test)),
                new ModulePanelAction("Add group", () => Defer(() => _tools.AddGroup(test))),
                new ModulePanelAction("Remove test", () => Defer(() => RemoveTest(serialized, tests, index, label)), true, true));

            painter.Note(
                "Raising the version restarts the test: every player decides again at their next "
                + "launch, the ones the rollout left outside included.");

            DrawMessages(painter, messages, id.stringValue);

            SerializedProperty groups = test.FindPropertyRelative(AbTestAuthoringTools.GROUPS);

            for (int groupIndex = 0; groupIndex < groups.arraySize; groupIndex++)
                DrawGroup(painter, groups, groupIndex);
        }

        private void DrawGroup(ModulePanelPainter painter, SerializedProperty groups, int index)
        {
            SerializedProperty group = groups.GetArrayElementAtIndex(index);
            SerializedProperty name = group.FindPropertyRelative(AbTestAuthoringTools.NAME);
            bool isControl = index == 0;

            painter.Property(name, isControl ? "Group " + (index + 1) + " (control)" : "Group " + (index + 1));

            if (isControl)
            {
                painter.Note("The first group is the control: the original configuration, with no overrides.");
                painter.Actions(new ModulePanelAction("Remove group", () => Defer(() => _tools.Remove(groups, index)), true, true));
                return;
            }

            SerializedProperty overrides = group.FindPropertyRelative(AbTestAuthoringTools.OVERRIDES);

            for (int pairIndex = 0; pairIndex < overrides.arraySize; pairIndex++)
            {
                SerializedProperty pair = overrides.GetArrayElementAtIndex(pairIndex);

                painter.Properties(
                    "Override " + (pairIndex + 1) + " - original / variant",
                    pair.FindPropertyRelative(AbTestAuthoringTools.ORIGINAL),
                    pair.FindPropertyRelative(AbTestAuthoringTools.VARIANT));
            }

            var actions = new List<ModulePanelAction>
            {
                new ModulePanelAction("Add override", () => Defer(() => _tools.AddOverride(group))),
                new ModulePanelAction("Remove group", () => Defer(() => _tools.Remove(groups, index)), true, true)
            };

            if (overrides.arraySize > 0)
            {
                int last = overrides.arraySize - 1;
                actions.Add(new ModulePanelAction("Remove last override", () => Defer(() => _tools.Remove(overrides, last)), true, true));
            }

            painter.Actions(actions.ToArray());
        }

        private static void DrawMessages(ModulePanelPainter painter, Dictionary<string, List<AbTestValidationVO>> messages, string owner)
        {
            if (!messages.TryGetValue(owner, out List<AbTestValidationVO> list))
                return;

            foreach (AbTestValidationVO message in list)
            {
                if (message.Severity == AbTestValidationSeverity.Error)
                    painter.Warning("Error: " + message.Message);
                else if (message.Severity == AbTestValidationSeverity.Warning)
                    painter.Warning(message.Message);
                else
                    painter.Note(message.Message);
            }
        }

        private void Defer(Action change) => _pending.Enqueue(change);

        private void RemoveTest(SerializedObject serialized, SerializedProperty tests, int index, string label)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Remove the test",
                "This removes '" + label + "' from " + serialized.targetObject.name + ". Players keep their stored "
                + "assignment in PlayerPrefs until the key is cleared; the module ignores a test it no "
                + "longer has.",
                "Remove",
                "Cancel");

            if (confirmed)
                _tools.Remove(tests, index);
        }

        private static void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create CD_AbTests", "CD_AbTests", "asset", "Where the tests live", "Assets");

            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CD_AbTests>(), path);
            AssetDatabase.SaveAssets();
        }

        private static void Select(CD_AbTests asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private SerializedObject Serialized(CD_AbTests asset)
        {
            if (_serialized.TryGetValue(asset, out SerializedObject serialized) && serialized.targetObject != null)
                return serialized;

            serialized = new SerializedObject(asset);
            _serialized[asset] = serialized;

            return serialized;
        }

        private static List<CD_AbTests> Assets()
        {
            var assets = new List<CD_AbTests>();

            foreach (string guid in AssetDatabase.FindAssets("t:CD_AbTests"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<CD_AbTests>(AssetDatabase.GUIDToAssetPath(guid));

                if (asset != null)
                    assets.Add(asset);
            }

            assets.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

            return assets;
        }
    }
}

#endif
