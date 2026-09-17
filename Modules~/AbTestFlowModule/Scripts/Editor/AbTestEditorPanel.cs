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
    /// find. The tests of every CD_AbTests in the project are listed down the left, the active one
    /// marked, a "+" on the heading to add one; the one clicked is open on the right with its id, version,
    /// share of players and groups, the groups as a matrix: a row per asset the test changes, a
    /// column per group, the control's column being the game's own assets. Activate makes the open
    /// test the one that runs, Delete removes it. The validator's word sits beside the test it is
    /// about rather than in the Console. Every field is the asset's own serialized property, so
    /// undo, dirtying and saving are Unity's; the panel edits what the Inspector edits and nothing
    /// a Model holds. Which test is open is this developer's, kept in SessionState.
    ///
    /// Selector is the other panel: this one shapes the tests, that one picks the group this
    /// machine plays. They are used at different moments, so they are two windows.
    /// </summary>
    internal class AbTestEditorPanel : ModulePanel
    {
        private const string MENU_PATH = "Tools/FlowIoC-Modules/AB Test/Editor";

        private const int MENU_PRIORITY = -1080;

        private const string LIST_TITLE = "A/B Test List";
        private const string ACTIVE_MARKER = "active";
        private const string SHOWN_KEY = "FlowIoC.AbTestEditor.Shown.";

        private readonly AbTestAuthoringTools _tools = new AbTestAuthoringTools();

        /// <summary>One SerializedObject per asset, kept while the window lives so a field keeps its focus between repaints.</summary>
        private readonly Dictionary<CD_AbTests, SerializedObject> _serialized = new Dictionary<CD_AbTests, SerializedObject>();

        /// <summary>
        /// A change to a list's shape - a test added, a group removed - waits until the rows are
        /// drawn: the rows are walked by index, and a list that changes under the walk draws a
        /// row that is gone. The sidebar is drawn first, so what it asks for lands after the rows.
        /// </summary>
        private readonly Queue<Action> _pending = new Queue<Action>();

        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        private static void Open() => ModulePanelWindow.Open<AbTestEditorPanel>();

        public override string Title => "AB Test Editor";

        public override string Module => "AbTestFlowModule";

        public override string Subtitle => "Author the tests without hunting for the asset";

        public override FlowRole Role => FlowRole.Service;

        public override string HelpPage => "A/B Test Module";

        public override bool HasSidebar => true;

        /// <summary>
        /// Select asset on the bar, beside the help icon, while the project has the one CD_AbTests
        /// it normally has; with several, each keeps its own button under its tests instead.
        /// </summary>
        public override ModulePanelAction? BarAction
        {
            get
            {
                List<CD_AbTests> assets = Assets();

                return assets.Count == 1 ? SelectAction(assets[0]) : null;
            }
        }

        public override void DrawSidebar(ModulePanelSidebarPainter sidebar)
        {
            List<CD_AbTests> assets = Assets();

            // The "+" that adds a test sits on the list's heading while the project has the one asset
            // it normally has; with several, each asset gets a heading of its own to carry it.
            if (assets.Count == 1)
                sidebar.Heading(LIST_TITLE, AddAction(assets[0]));
            else
                sidebar.Heading(LIST_TITLE);

            foreach (CD_AbTests asset in assets)
            {
                List<AbTestCVO> tests = asset.Tests;
                int shown = Shown(asset, tests, asset.ActiveTestId);

                if (assets.Count > 1)
                    sidebar.Heading(asset.name, AddAction(asset));

                for (int index = 0; index < tests.Count; index++)
                {
                    int picked = index;
                    bool isActive = IsActive(tests[index].Id, asset.ActiveTestId);

                    sidebar.Item(Label(tests[index].Id), index == shown, isActive ? ACTIVE_MARKER : null,
                        () => Show(asset, picked));
                }

                if (assets.Count > 1)
                    sidebar.Action(SelectAction(asset));

                sidebar.Space();
            }
        }

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
            SerializedProperty tests = _tools.Tests(serialized);
            string activeId = _tools.ActiveTestId(serialized).stringValue;
            int shown = Shown(asset, asset.Tests, activeId);

            if (shown < 0)
            {
                painter.Heading(asset.name);
                painter.Note("No test yet. The + on the left adds one.");
            }
            else
            {
                DrawTest(painter, serialized, asset, tests, shown, messages, activeId);
            }

            if (messages.TryGetValue(string.Empty, out List<AbTestValidationVO> ofAsset))
                DrawMessages(painter, ofAsset, message => true);

            while (_pending.Count > 0)
                _pending.Dequeue()();

            serialized.ApplyModifiedProperties();
            painter.Space();
        }

        private void DrawTest(ModulePanelPainter painter, SerializedObject serialized, CD_AbTests asset,
            SerializedProperty tests, int index, Dictionary<string, List<AbTestValidationVO>> messages, string activeId)
        {
            SerializedProperty test = tests.GetArrayElementAtIndex(index);
            SerializedProperty id = test.FindPropertyRelative(AbTestAuthoringTools.ID);
            bool isActive = IsActive(id.stringValue, activeId);

            painter.Heading(
                Label(id.stringValue) + (isActive ? " - active" : string.Empty),
                new ModulePanelAction(isActive ? "Deactivate" : "Activate",
                    () => _tools.Activate(serialized, isActive ? string.Empty : id.stringValue),
                    isActive ? ModulePanelActionKind.Caution : ModulePanelActionKind.Confirm,
                    isActive || !string.IsNullOrEmpty(id.stringValue)),
                new ModulePanelAction("-", () => Defer(() => DeleteTest(serialized, asset, tests, index)), true, true));

            // The active test is named by id, so renaming the active test carries the name over -
            // otherwise every keystroke would leave the asset naming a test that no longer exists.
            string before = id.stringValue;
            painter.PropertyWithAside(
                id, "Id", test.FindPropertyRelative(AbTestAuthoringTools.VERSION), "Version",
                "The id names the test in PlayerPrefs, in RD_AbTestStatus and in the log. One test runs at a "
                + "time: Activate on the heading makes this the one, Deactivate leaves none, and the - deletes "
                + "it. Switching leaves every stored decision in PlayerPrefs as it is, so a test switched back "
                + "on carries on where it was; raising the version restarts it, every player deciding again at "
                + "their next launch, the ones left outside the test included.");

            if (id.stringValue != before && before == activeId)
                _tools.Activate(serialized, id.stringValue);

            painter.Property(
                test.FindPropertyRelative(AbTestAuthoringTools.TEST_USER_PERCENT), "Test users / all users",
                "In percent: 80 puts 80 of every 100 players into the test, split evenly over its groups, "
                + "and leaves the other 20 playing the original configuration outside it.");

            List<AbTestValidationVO> own = messages.TryGetValue(id.stringValue, out List<AbTestValidationVO> list)
                ? list
                : new List<AbTestValidationVO>();

            DrawMessages(painter, own, message => message.Scope == AbTestValidationScope.Test);
            DrawMatrix(painter, test, own);
        }

        /// <summary>
        /// The groups as a matrix: the group names across the top, then a row per asset the
        /// control group lists, with the control's asset - the game's own - in the first column and
        /// the asset each other group replaces it with beside it. The lists are aligned first, so a
        /// list hand-edited to another length gets an empty cell rather than no cell.
        /// </summary>
        private void DrawMatrix(ModulePanelPainter painter, SerializedProperty test, List<AbTestValidationVO> messages)
        {
            _tools.AlignAssets(test);

            SerializedProperty groups = test.FindPropertyRelative(AbTestAuthoringTools.GROUPS);
            int columns = groups.arraySize;
            int rows = _tools.Rows(test);

            if (columns == 0)
                return;

            var names = new SerializedProperty[columns];
            var assets = new SerializedProperty[columns];

            for (var column = 0; column < columns; column++)
            {
                SerializedProperty group = groups.GetArrayElementAtIndex(column);
                names[column] = group.FindPropertyRelative(AbTestAuthoringTools.NAME);
                assets[column] = group.FindPropertyRelative(AbTestAuthoringTools.ASSETS);
            }

            painter.Properties(
                "Groups",
                "The first group is the control, and its column is the game's own assets - the ones this "
                + "test changes. Every other column is the asset that replaces it for that group, row by row. "
                + "The + adds a group, the - takes the last one away while more than the two a test needs are "
                + "there; the - on an asset row takes that row out of every group.",
                new ModulePanelAction?[]
                {
                    ModulePanelAction.Add("+", () => Defer(() => _tools.AddGroup(test))),
                    new ModulePanelAction("-", () => Defer(() => _tools.Remove(groups, columns - 1)), ModulePanelActionKind.Remove, columns > 2)
                },
                Flagged(messages, columns, message => message.Scope == AbTestValidationScope.Group),
                names);

            DrawMessages(painter, messages, message => message.Scope == AbTestValidationScope.Group);

            for (var row = 0; row < rows; row++)
            {
                int current = row;
                var cells = new SerializedProperty[columns];

                for (var column = 0; column < columns; column++)
                    cells[column] = assets[column].GetArrayElementAtIndex(row);

                // The "-" takes this row out of every group; the blank before it keeps the cells in
                // line with the group names above, which carry two squares. A cell the validator
                // names is flagged, and what it says about the row sits right under it.
                painter.Properties(
                    "Asset " + (row + 1), null,
                    new ModulePanelAction?[]
                    {
                        null,
                        new ModulePanelAction("-", () => Defer(() => _tools.RemoveAsset(test, current)), ModulePanelActionKind.Remove)
                    },
                    Flagged(messages, columns, message => message.Scope == AbTestValidationScope.Cell && message.Row == current),
                    cells);

                DrawMessages(painter, messages, message => message.Scope == AbTestValidationScope.Cell && message.Row == current);
            }

            // The row button sits under the row labels, in line with "Asset 1", where a reader looks
            // for what is done to the rows; the column buttons are on the Groups row above.
            painter.Actions(new ModulePanelAction("Add asset", () => Defer(() => _tools.AddAsset(test)), ModulePanelActionKind.Confirm));
            DrawMessages(painter, messages, message => message.Scope == AbTestValidationScope.Rows);
        }

        /// <summary>The messages that pass the filter, each in the row its severity wears - red, amber, or a note.</summary>
        private static void DrawMessages(ModulePanelPainter painter, List<AbTestValidationVO> messages,
            Func<AbTestValidationVO, bool> about)
        {
            foreach (AbTestValidationVO message in messages)
            {
                if (!about(message))
                    continue;

                if (message.Severity == AbTestValidationSeverity.Error)
                    painter.Error(message.Message);
                else if (message.Severity == AbTestValidationSeverity.Warning)
                    painter.Warning(message.Message);
                else
                    painter.Note(message.Message);
            }
        }

        /// <summary>One flag per column: the column an error that passes the filter names.</summary>
        private static bool[] Flagged(List<AbTestValidationVO> messages, int columns, Func<AbTestValidationVO, bool> about)
        {
            var flagged = new bool[columns];

            foreach (AbTestValidationVO message in messages)
            {
                if (about(message) && message.Severity == AbTestValidationSeverity.Error
                    && message.Group >= 0 && message.Group < columns)
                    flagged[message.Group] = true;
            }

            return flagged;
        }

        private void Defer(Action change) => _pending.Enqueue(change);

        private static string Label(string id) => string.IsNullOrEmpty(id) ? "(no id)" : id;

        private static bool IsActive(string id, string activeId) => !string.IsNullOrEmpty(id) && id == activeId;

        private static ModulePanelAction SelectAction(CD_AbTests asset) =>
            new ModulePanelAction("Select asset", () => Select(asset));

        private ModulePanelAction AddAction(CD_AbTests asset) =>
            ModulePanelAction.Add("+", () => Defer(() => AddTest(Serialized(asset), asset)));

        /// <summary>
        /// The test drawn open: the one this developer clicked last, while it still exists, else
        /// the active one, else the first. Kept in SessionState, so it survives a domain reload and
        /// resets with the Editor - a developer's own state, never the asset's.
        /// </summary>
        private static int Shown(CD_AbTests asset, List<AbTestCVO> tests, string activeId)
        {
            int shown = SessionState.GetInt(Key(asset), -1);

            if (shown >= 0 && shown < tests.Count)
                return shown;

            for (int index = 0; index < tests.Count; index++)
            {
                if (IsActive(tests[index].Id, activeId))
                    return index;
            }

            return tests.Count > 0 ? 0 : -1;
        }

        private static void Show(CD_AbTests asset, int index) => SessionState.SetInt(Key(asset), index);

        private static string Key(CD_AbTests asset) =>
            SHOWN_KEY + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));

        /// <summary>A new test opens at once, because naming it is the next thing the author does.</summary>
        private void AddTest(SerializedObject serialized, CD_AbTests asset)
        {
            _tools.AddTest(serialized);
            Show(asset, _tools.Tests(serialized).arraySize - 1);
        }

        private void DeleteTest(SerializedObject serialized, CD_AbTests asset, SerializedProperty tests, int index)
        {
            string id = tests.GetArrayElementAtIndex(index).FindPropertyRelative(AbTestAuthoringTools.ID).stringValue;

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete the test",
                "This deletes '" + Label(id) + "' from " + asset.name + ". Players keep their stored "
                + "assignment in PlayerPrefs until the key is cleared; the module ignores a test it no "
                + "longer has.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            if (IsActive(id, _tools.ActiveTestId(serialized).stringValue))
                _tools.Activate(serialized, string.Empty);

            _tools.Remove(tests, index);
            SessionState.EraseInt(Key(asset));
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