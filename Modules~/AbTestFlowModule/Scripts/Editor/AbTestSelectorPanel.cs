#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModulePanels;
using Modules.AbTestFlowModule.Data.UnityObjects;
using Modules.AbTestFlowModule.Data.ValueObjects;
using UnityEditor;

namespace Modules.AbTestFlowModule.Editor
{
    /// <summary>
    /// Where this machine's player stands in every test, and the two things a developer does
    /// about it between runs: put the player in a group to see that variant, or forget the
    /// assignment so the next run rolls again. It writes only the prefs the module reads at boot,
    /// in the module's own shape; the Model is never touched.
    ///
    /// The tests come from the CD_AbTests assets in the project - every one, not only the one a
    /// scene's Root carries, because the question is which group this machine is in for a test,
    /// and that is answered by the test's id whichever asset declares it.
    /// </summary>
    internal class AbTestSelectorPanel : ModulePanel
    {
        /// <summary>
        /// "AB Test" rather than the module's "A/B Test": a slash in a menu path opens a submenu.
        /// The priority is the one every module panel uses, which keeps FlowIoC-Modules second
        /// under Tools.
        /// </summary>
        private const string MENU_PATH = "Tools/FlowIoC-Modules/AB Test/Selector";

        private const int MENU_PRIORITY = -1080;

        private const string PLAYING =
            "The game is running and has already read its assignments; a change here is read at "
            + "the next run. Leave play mode first.";

        private readonly AbTestPrefsTools _prefs = new AbTestPrefsTools();

        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        private static void Open() => ModulePanelWindow.Open<AbTestSelectorPanel>();

        public override string Title => "AB Test Selector";

        public override string Module => "AbTestFlowModule";

        public override string Subtitle => "Pick the group this machine plays, before pressing Play";

        public override FlowRole Role => FlowRole.Service;

        public override string HelpPage => "A/B Test";

        public override void Draw(ModulePanelPainter painter)
        {
            bool playing = EditorApplication.isPlaying;

            if (playing)
                painter.Warning(PLAYING);

            List<CD_AbTests> assets = Assets();

            if (assets.Count == 0)
            {
                painter.Heading("Tests");
                painter.Note("No CD_AbTests asset in the project. Author one - Create > FlowIoC > AbTestFlowModule > Data > CD_AbTests - and file it on AbTestFlowServiceRoot's adapter.");
                return;
            }

            foreach (CD_AbTests asset in assets)
            {
                painter.Heading(asset.name + " - " + AssetDatabase.GetAssetPath(asset));

                if (asset.Tests.Count == 0)
                    painter.Note("No tests declared.");

                foreach (AbTestCVO test in asset.Tests)
                    DrawTest(painter, test, playing);

                painter.Space();
            }

            painter.Note(
                "Force writes the assignment the way a roll would, under the test's current version, "
                + "so the next run reads it as the player's own. Reset forgets it and the next run "
                + "rolls the test again. Neither touches a running game.");
        }

        private void DrawTest(ModulePanelPainter painter, AbTestCVO test, bool playing)
        {
            AbTestPrefsTools.StoredAssignment stored = _prefs.Read(test);

            painter.Field(test.Id + " v" + test.Version, Describe(test, stored));

            var actions = new List<ModulePanelAction>();

            foreach (AbTestGroupCVO group in test.Groups)
            {
                string name = group.Name;
                bool isCurrent = stored.Standing == AbTestPrefsTools.Standing.InGroup && stored.Group == name;

                actions.Add(new ModulePanelAction("Force " + name, () => _prefs.Force(test, name), !playing && !isCurrent));
            }

            actions.Add(new ModulePanelAction(
                "Force outside",
                () => _prefs.ForceOutside(test),
                !playing && stored.Standing != AbTestPrefsTools.Standing.OutOfTest));

            actions.Add(new ModulePanelAction(
                "Reset",
                () => _prefs.Reset(test),
                !playing && stored.Standing != AbTestPrefsTools.Standing.NotAssigned,
                true));

            painter.Actions(actions.ToArray());
        }

        private static string Describe(AbTestCVO test, AbTestPrefsTools.StoredAssignment stored)
        {
            string activity = test.IsActive ? "active, " + test.RolloutPercent + "% rollout" : "inactive";

            switch (stored.Standing)
            {
                case AbTestPrefsTools.Standing.InGroup:
                    return "in group '" + stored.Group + "' - " + activity;
                case AbTestPrefsTools.Standing.OutOfTest:
                    return "outside the test - " + activity;
                case AbTestPrefsTools.Standing.Stale:
                    return "assigned under v" + stored.Version + ", rolled again at the next run - " + activity;
                case AbTestPrefsTools.Standing.UnknownGroup:
                    return "in group '" + stored.Group + "', which the config no longer has - rolled again at the next run - " + activity;
                default:
                    return "not assigned yet - " + activity;
            }
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
