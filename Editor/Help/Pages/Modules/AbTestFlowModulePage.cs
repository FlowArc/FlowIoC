#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.Icons;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The A/B test module: what a test is, how a group's config lands over the game's own, where
    /// a player's status is read from, and the button that puts it in the project.
    /// </summary>
    internal class AbTestFlowModulePage : HelpPage
    {
        private const string ModuleFolderName = "AbTestFlowModule";

        private readonly ModuleInstaller _installer =
            new ModuleInstaller(new ProjectRoot().Resolve(), new ModulesSource());

        private readonly HelpImages _images = new HelpImages();

        private readonly HelpAction _install;

        private bool _isInstalled;
        private double _checkedAt = double.NegativeInfinity;

        public AbTestFlowModulePage() : base(null)
        {
            // The label and the enabled state are read every repaint rather than fixed here, so
            // the button turns itself off the moment the module lands in the project.
            _install = new HelpAction(
                () => IsInstalled() ? "Installed" : "Install",
                () => !IsInstalled(),
                Install);
        }

        public override string Title => "A/B Test";

        public override string Subtitle => "One group per player, decided before the game reads its config";

        public override FlowIcon Icon => FlowIcon.Diagram;

        public override HelpAction Action => _install;

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "Put the Root in the scene and author one test.",
                "The Root ships with its two assets already filed. A test is a few fields on "
                + "CD_AbTests, and the Inspector reports a mistake while you make it."),
            new HelpTab("Usage", DrawUsage,
                "Ask the service, or read the status asset.",
                "A module that branches on the group injects IAbTestFlowService. A module that "
                + "only reports - analytics - reads RD_AbTestStatus when its own SDK is up."),
            new HelpTab("Overrides", DrawOverrides,
                "A variant is written over the original, field by field.",
                "JsonUtility carries primitives, lists and references to other assets across. The "
                + "originals are put back when play mode ends, so the Editor stays clean.")
        };

        /// <summary>
        /// Whether the module is in the project, answered from a cache that goes stale after a
        /// second. The underlying check walks every asmdef under Assets, and the banner asks twice
        /// per repaint - often enough that doing the walk each time would cost real frames.
        /// </summary>
        private bool IsInstalled()
        {
            if (EditorApplication.timeSinceStartup - _checkedAt < 1d)
                return _isInstalled;

            _isInstalled = _installer.IsInstalled(ModuleFolderName);
            _checkedAt = EditorApplication.timeSinceStartup;

            return _isInstalled;
        }

        private void Install()
        {
            // Whatever happened, what the cache holds is now a guess about a project that has
            // changed underneath it.
            _checkedAt = double.NegativeInfinity;

            if (_installer.TryInstall(ModuleFolderName, out string error))
            {
                EditorUtility.DisplayDialog(
                    "A/B Test installed",
                    $"The module is now at {ModuleInstaller.TargetFolder}/{ModuleFolderName}.\n\n"
                    + "It is yours to edit from here - the copy in the package is only the one "
                    + "installs are made from. Drop AbTestFlowServiceRoot into your scene and author "
                    + "a test in CD_AbTests; the Setup tab has the steps.",
                    "OK");

                return;
            }

            EditorUtility.DisplayDialog("A/B Test", error, "OK");
        }

        protected override string BodyHeadline =>
            "A player lands in one group of each test, and that group's config is what the game reads.";

        protected override string BodyTagline =>
            "Harder levels for half the players, a cheaper shop for a third - each is a test with "
            + "groups, and each group is a set of config assets written over the game's own.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "A test is an id, a version, a rollout percentage and its groups. The first group "
                + "is the control and carries nothing: it is the original configuration, not a copy.");
            painter.Bullet(
                "Every other group carries pairs - a config asset another module owns, and the "
                + "variant to write over it. A group's pairs are applied as a set, so a player never "
                + "gets group B's levels next to group A's economy.");
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
                "Incoming.ResolveAbTests decides every active test for this player and writes the "
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
            painter.Bullet("CD_AbTests - the tests, authored by you. In the adapter's own map.");
            painter.Bullet(
                "RD_AbTestStatus - where the player stands, filled at boot. In the Shared Scriptables, "
                + "so any module reads it through ISharedDataModel.GetScriptable<RD_AbTestStatus>() once "
                + "it is ready - nothing is announced, because the decision is made before anyone listens.");

            painter.Space();
            painter.Note(
                "Important: an asset missing from the adapter is reported as an error naming the "
                + "asset and the Root, and the module then decides nothing. Double-click the error "
                + "to reach the Root.");

            painter.SubHeading("2. A test");
            painter.Paragraph(
                "Select Scriptables/CD_AbTests and add a test. The Inspector runs the same "
                + "validation the module does, so an empty id, a single group, an override on the "
                + "control group or a variant of another type is reported while you type it.");
            painter.Image(_images.Get("AbTestsInspector.png"),
                "The test the module ships with: two groups, the control empty, one pair on B.");

            painter.Paragraph(
                "Id is also the PlayerPrefs key. Version is what you raise to restart the test. "
                + "RolloutPercent is how many players are in the test at all; the rest keep the "
                + "original config and are reported as outside. A real test usually moves more than "
                + "one asset, and each group then carries every pair that has to change together:");
            painter.Tree(new HelpTreeNode("LevelDifficulty", "Version 1 · active · 50 % rollout",
                new HelpTreeNode("A", "control - Overrides stays empty"),
                new HelpTreeNode("B", "CD_Level -> CD_Level_B · CD_Currency -> CD_Currency_B"),
                new HelpTreeNode("C", "CD_Level -> CD_Level_C · CD_Currency -> CD_Currency_C")));

            painter.Space();
            painter.Paragraph(
                "Original and Variant are typed ScriptableObject on purpose: you drag another "
                + "module's CD_ asset onto the field, and this module still references no other "
                + "module's assembly. A variant is an asset of the same type as its original - "
                + "make one per group and keep them beside the test, in this module's Scriptables.");

            painter.Space();
            painter.Note(
                "Important: an original left out of one group is a warning, not an error, and it is "
                + "the mistake this shape invites. If group B changes the currency, group C changes "
                + "it too - or group C's players get group B's levels with the control's economy.");

            painter.SubHeading("3. Restarting a test");
            painter.Paragraph(
                "Raise Version. Every player decides again on their next launch, including the ones "
                + "the rollout had left outside - a test is restarted, not amended. Switching "
                + "IsActive off leaves the stored decisions untouched, so switching it back on "
                + "returns every player to the group they had.");
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
            painter.Code(
                "flowioc.abtest.LevelDifficulty = \"1|B\"    // version 1, group B\n"
                + "flowioc.abtest.LevelDifficulty = \"1|-\"    // version 1, outside the test");
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