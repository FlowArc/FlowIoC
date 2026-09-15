#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The A/B test module: what a test is, how a group's config lands over the game's own, where
    /// a player's status is read from, and the button that puts it in the project.
    /// </summary>
    internal class AbTestFlowModulePage : ModulePage
    {
        public override string ModuleFolderName => "AbTestFlowModule";

        public override string Title => "A/B Test";

        public override string Subtitle => "One group per player, decided before the game reads its config";

        public override FlowIcon Icon => FlowIcon.Diagram;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "Put the Root in the scene and author one test.",
                "The Root ships with its two assets already filed. A test is a few fields and a matrix "
                + "in the AB Test Editor, which reports a mistake beside the cell while you make it."),
            new HelpTab("Usage", DrawUsage,
                "Ask the service, or read the status asset.",
                "A module that branches on the group injects IAbTestFlowService. A module that "
                + "only reports - analytics - reads RD_AbTestStatus when its own SDK is up."),
            new HelpTab("Overrides", DrawOverrides,
                "A variant is written over the original, field by field.",
                "JsonUtility carries primitives, lists and references to other assets across. The "
                + "originals are put back when play mode ends, so the Editor stays clean.")
        };

        public override string InstalledHint =>
            "Drop AbTestFlowServiceRoot into your scene and author a test in CD_AbTests; the Setup "
            + "tab has the steps.";

        public override string BodyHeadline =>
            "A player lands in one group of the active test, and that group's config is what the game reads.";

        public override string BodyTagline =>
            "Harder levels for half the players, a cheaper shop for a third - each is a test with "
            + "groups, and each group is a set of config assets written over the game's own.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "A test is an id, a version, the share of players who enter it and its groups, laid "
                + "out as a matrix: a row per config asset the test changes, a column per group. The "
                + "first column is the control and holds the game's own assets - the originals "
                + "themselves, not copies.");
            painter.Bullet(
                "Every other column holds, row by row, the asset that replaces the original for that "
                + "group. A column is applied as a set, so a player never gets group B's levels next "
                + "to group A's economy - and the shape keeps every group the length of the control.");
            painter.Bullet(
                "One test runs at a time: CD_AbTests names it, and the rest wait their turn without "
                + "losing what their players were assigned.");
            painter.Bullet(
                "The decision is made once, during boot, and kept in PlayerPrefs. The same player "
                + "lands in the same group on every launch until the test's version is raised.");
            painter.Bullet(
                "Where the player stands is published in RD_AbTestStatus, an asset in the module's "
                + "Shared assembly, and answered by IAbTestFlowService from code.");

            painter.Space();
            painter.Note(
                "It is a Service, so a module that branches on the group references "
                + "Modules.AbTestFlow and injects IAbTestFlowService directly. A module that only "
                + "reads the status references Modules.AbTestFlow.Shared and nothing else.");

            painter.SubHeading("When it decides");
            painter.Paragraph(
                "The overrides land during the binding pass, from the service's PostConstruct, "
                + "because that is the only moment early enough: a module reads its config in its "
                + "own PostConstruct, and Setup would be a frame too late. That is also why nothing "
                + "is announced - no other module is listening yet, so the status is data to read "
                + "rather than an event to catch.");

            painter.Space();
            painter.Note(
                "Important: AbTestFlowServiceRoot ships at Initialize Order -90. It has to stay "
                + "below every module whose config it overrides, or that module reads the original "
                + "before the variant lands - and nothing is logged, because nothing went wrong.");

            painter.SubHeading("Its signal");
            painter.Paragraph(
                "Incoming.ResolveAbTests decides the active test for this player and writes the "
                + "overrides. The service dispatches it at boot; a game dispatches it again only to "
                + "decide again, which changes nothing while the version stands. There is no "
                + "Outgoing: the result is in RD_AbTestStatus.");

            painter.SubHeading("Trying it out");
            painter.Paragraph(
                "The module ships with a test module beside it, and the scene it runs in arrives "
                + "with it. Open AbTestFlowTestScene under the test module's Scenes folder and press "
                + "Play: the group the player landed in, the probe config as it reads after the "
                + "override, and three buttons - Clear forgets the stored decision, Raise version "
                + "restarts the test, Re-roll decides again without leaving play mode.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("1. The Root");
            painter.Paragraph(
                "Drop Prefabs/AbTestFlowServiceRoot into the scene. It detaches itself and survives "
                + "scene loads, so a scene change neither decides again nor writes an override "
                + "twice. Its RootAdapter already carries the two assets the module reads, filed "
                + "under their type names:");
            painter.Table(new[] {"Asset", "What it holds", "Where it is filed"},
                new[] {"CD_AbTests", "The tests, authored by you.", "In the adapter's own map."},
                new[]
                {
                    "RD_AbTestStatus", "Where the player stands, filled at boot.",
                    "In the Shared Scriptables, so any module reads it through "
                    + "ISharedDataModel.GetScriptable<RD_AbTestStatus>() once it is ready - nothing is "
                    + "announced, because the decision is made before anyone listens."
                });

            painter.Space();
            painter.Note(
                "Important: an asset missing from the adapter is reported as an error naming the "
                + "asset and the Root, and the module then decides nothing. Double-click the error "
                + "to reach the Root.");

            painter.SubHeading("2. A test");
            painter.Paragraph(
                "Open Tools > FlowIoC-Modules > AB Test > Editor. The tests of every CD_AbTests in the "
                + "project are listed down the left, the active one marked, so there is no asset to "
                + "hunt for; the + on the list adds one, and the clicked one opens on the right. A new "
                + "test comes with every player in it, the control group and one variant, and is not "
                + "active until you press Activate on its heading. Give it an id, name the groups, add "
                + "a row per asset the test changes and fill the matrix: the game's own asset in the "
                + "control column, the replacement for each group beside it. The validator's word - an "
                + "empty slot, a name used twice, a replacement of another type - sits under the row it "
                + "is about, the slot washed red. Every field is the asset's own, so undo and save are "
                + "Unity's, and the Inspector shows the same test if you would rather edit it there.");

            painter.Paragraph(
                "Id is also the PlayerPrefs key. Version is what you raise to restart the test. Test "
                + "users / all users is the share of players who enter the test at all - 80 puts 80 of "
                + "every 100 into one of the groups, split evenly - and the rest keep the original "
                + "config and are reported as outside. A real test usually moves more than one asset, "
                + "and each row of the matrix then names one:");
            painter.Table(new[] {"", "A (control)", "B", "C"},
                new[] {"Asset 1", "CD_Level", "CD_Level_B", "CD_Level_C"},
                new[] {"Asset 2", "CD_Currency", "CD_Currency_B", "CD_Currency_C"});

            painter.Space();
            painter.Paragraph(
                "Every cell is typed ScriptableObject on purpose: you drag another module's CD_ asset "
                + "onto it, and this module still references no other module's assembly. A replacement "
                + "is an asset of the same type as the control's - make one per group and keep them "
                + "beside the test, in this module's Scriptables.");

            painter.Space();
            painter.Note(
                "Important: the shape keeps every column the length of the control's, so a group "
                + "cannot forget an asset - but it can leave a cell empty, and an empty cell is an "
                + "error at boot that skips that row alone. Fill every cell before the test goes out.");

            painter.SubHeading("3. Restarting a test");
            painter.Paragraph(
                "Raise Version. Every player decides again on their next launch, including the ones "
                + "left outside the test - a test is restarted, not amended. Activating another test "
                + "leaves the stored decisions untouched, so activating this one again returns every "
                + "player to the group they had.");
            painter.Space();

            painter.SubHeading("4. Seeing a variant on this machine");
            painter.Paragraph(
                "Tools > FlowIoC-Modules > AB Test > Selector lists every test with the group this "
                + "machine's player is in. Force a group before pressing Play and the run reads it as "
                + "the player's own - the panel writes the same PlayerPrefs key a roll writes, under "
                + "the test's current version - or Reset the assignment and the next run rolls "
                + "again. Nothing changes in a running game: the module reads its assignments once, "
                + "at boot.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("Branching on the group");
            painter.Code(
                "[Inject] private IAbTestFlowService _abTests { get; set; }\n"
                + "\n"
                + "if (_abTests.IsInGroup(\"LevelDifficulty\", \"B\"))\n"
                + "    _view.ShowHardModeBadge();\n"
                + "\n"
                + "string group = _abTests.GetGroup(\"LevelDifficulty\");   // null when outside the test");

            painter.Paragraph(
                "Most modules never need this. A test that only changes config - which is what the "
                + "overrides are for - is invisible to the module reading that config: it reads "
                + "CD_Level as it always did and gets group B's numbers.");

            painter.SubHeading("Reporting the group");
            painter.Paragraph(
                "An analytics module comes up asynchronously, after its SDK. When it is ready it "
                + "reads the status asset - filed on its own Root's adapter, or reached however the "
                + "module reaches its assets - and attributes the session. Referencing "
                + "Modules.AbTestFlow.Shared is enough for that.");
            painter.Code(
                "foreach (AbTestStatusRVO status in _status.Tests)\n"
                + "{\n"
                + "    if (status.IsInTest)\n"
                + "        _analytics.SetUserProperty(\"abtest_\" + status.AbTestId, status.Group);\n"
                + "}");

            painter.SubHeading("What is stored");
            painter.Paragraph(
                "One PlayerPrefs entry per test, under the prefix AbTestConstants.PrefsPrefix:");
            painter.Table(new[] {"Entry", "Value", "Meaning"},
                new[] {"flowioc.abtest.LevelDifficulty", "1|B", "version 1, group B"},
                new[] {"flowioc.abtest.LevelDifficulty", "1|-", "version 1, outside the test"});
            painter.Paragraph(
                "A tester who wants a particular group writes that entry and relaunches; the "
                + "decision is read at boot, so a change takes effect on the next launch. A stored "
                + "group the config no longer has is decided again rather than kept.");
        }

        private void DrawOverrides(HelpPainter painter)
        {
            painter.Paragraph(
                "An override is one JsonUtility round trip: the variant is serialised and written "
                + "over the original. That carries primitives, strings, enums and nested lists - "
                + "copied, not shared - and references to other assets, so a variant that swaps a "
                + "prefab, a sprite or an audio clip is an ordinary test.");
            painter.Code(
                "JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(variant), original);");

            painter.Space();
            painter.Note(
                "Important: a field marked [SerializeReference] is not copied - JsonUtility skips "
                + "it without a word. Validation warns about a variant whose type carries one; the "
                + "copy itself leaves that field as the original had it.");

            painter.SubHeading("What the Editor keeps");
            painter.Paragraph(
                "In a build a ScriptableObject's runtime changes die with the process. In the "
                + "Editor they survive leaving play mode and would land in source control as a diff "
                + "on an asset nobody edited. So the module captures every original before the "
                + "first override and puts it back when play mode ends - the same answer the save "
                + "module gives for the assets it restores.");

            painter.Space();
            painter.Note(
                "Important: that restore runs from the Root's OnApplicationQuit. A play session "
                + "that ends by the Editor crashing, or a Root that is destroyed before quit, leaves "
                + "the variant's values in the original - revert the asset in source control.");
        }
    }
}

#endif