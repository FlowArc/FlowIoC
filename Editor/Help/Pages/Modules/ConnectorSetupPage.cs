#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>Where the modules meet: one sub-context per crossing, listed on ConnectorRoot.</summary>
    internal class ConnectorSetupPage : ModulePage
    {
        private readonly HelpImages _images = new HelpImages();

        public override string Title => "Connector Module";

        public override string Subtitle => "Where the modules meet";

        public override FlowIcon Icon => FlowIcon.Link;

        public override string ModuleFolderName => "ConnectorModule";

        public override bool InSetupSet => true;

        public override string BodyHeadline => "One sub-context per crossing.";

        public override string BodyTagline =>
            "ConnectorModule wires MainModule to the main screen, the main screen to the gameplay "
            + "screen, and LoadingConnectorSubContext joins the two loading presentations to the service.";

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("The crossings", DrawCrossings,
                "Two sub-contexts ship, and every wire in them is one line.",
                "A crossing is one module's announcement joined to another module's order. Read the "
                + "two files and the whole shape of the shipped game is on the page: who tells whom what, "
                + "and nothing else.")
        };

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it owns");
            painter.Bullet(
                "ConnectorRoot with an empty ConnectorContext: the Root binds nothing of its own. What "
                + "it carries is the list of sub-contexts on its inspector, one per counterpart module, "
                + "and Add Sub Context is how a new crossing is put there.");
            painter.Bullet(
                "MainConnectorSubContext, joining MainModule and its screens: the boot's Started opens "
                + "the main screen, the main screen's Play opens the gameplay screen, and "
                + "the loading screen's retry runs the boot again.");
            painter.Bullet(
                "LoadingConnectorSubContext, joining the loading service to its two presentations: "
                + "Began, SetChanged, SetCompleted and SetFailed reach the fullscreen screen and the "
                + "overlay, each applying what concerns the set it shows.");
            painter.Bullet(
                "The longest reference list in the project, on purpose: every module's Signals "
                + "assembly, and the Shared assemblies their payloads need. The Connector is the one place "
                + "allowed to know the game's shape.");

            painter.Image(_images.Get("ConnectorRootInspector.png"),
                "ConnectorRoot: the two shipped sub-contexts, and the button that adds the next crossing.");

            painter.SubHeading("In the scene");
            painter.Table(new[] {"Root", "Initialize Order", "Role"},
                new[]
                {
                    "ConnectorRoot", "98",
                    "Connector - read off the name. After every module it wires, so a sub-context's Setup "
                    + "finds every holder it asks for already bound; before ScreenRoot and MainRoot, so the "
                    + "wires are in place when the first signal is dispatched."
                });
            painter.PageLink("Ordering Roots", "Read: Ordering Roots - the bands and why 98");
            painter.PageLink("Connectors", "Read: Connectors - the rules, and writing one");

            painter.Note(
                "This module is the game's from the day it lands. It carries no version and is never "
                + "updated by the package.");
        }

        private void DrawCrossings(HelpPainter painter)
        {
            painter.SubHeading("MainConnectorSubContext");
            painter.Paragraph(
                "The holders are fetched in Setup - never bound, the owning module binds its own - and "
                + "the wiring is split by direction into IncomingSignals and OutgoingSignals, undone in "
                + "DestroyContext. Started is Main's announcement and OpenMainScreen the screen's order; "
                + "the crossing is the one line that joins them.");
            painter.Code(
                "public override void Setup()\n"
                + "{\n"
                + "    base.Setup();\n"
                + "\n"
                + "    _mainSignals           = InjectionBinderCrossContext.GetInstance<MainSignals>();\n"
                + "    _mainScreenSignals     = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();\n"
                + "    _gameplayScreenSignals = InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>();\n"
                + "    _loadingScreenSignals  = InjectionBinderCrossContext.GetInstance<LoadingScreenSignals>();\n"
                + "\n"
                + "    IncomingSignals();\n"
                + "    OutgoingSignals();\n"
                + "}\n"
                + "\n"
                + "private void IncomingSignals()\n"
                + "{\n"
                + "    _mainSignals.Outgoing.Started.Connect(_mainScreenSignals.Incoming.OpenMainScreen);\n"
                + "\n"
                + "    // The loading screen's retry button asks Main to boot again.\n"
                + "    _loadingScreenSignals.Outgoing.RetryClicked.Connect(_ => _mainSignals.Incoming.RetryBoot.Dispatch());\n"
                + "}\n"
                + "\n"
                + "private void OutgoingSignals()\n"
                + "{\n"
                + "    _mainScreenSignals.Outgoing.PlayClicked.Connect(_gameplayScreenSignals.Incoming.OpenGameplayScreen);\n"
                + "}",
                "ConnectorModule/Scripts/Runtime/RootsContexts/MainConnectorSubContext.cs");
            painter.Paragraph(
                "PlayClicked and OpenGameplayScreen carry nothing, so Connect joins them as they are. "
                + "RetryClicked carries the set it names and RetryBoot takes nothing, so that wire is a "
                + "lambda that dispatches. A payload that had to change shape between the two would be "
                + "adapted on the same line.");

            painter.SubHeading("LoadingConnectorSubContext");
            painter.Paragraph(
                "One service, two screens. Each Began reaches its own presentation - FullscreenBegan "
                + "opens the loading screen, OverlayBegan the overlay - and the set's changes, completion "
                + "and failure reach both. The wires are connected under one group name, so "
                + "DestroyContext takes them all back with a single DisconnectGroup.");
            painter.Code(
                "_loadingSignals.Outgoing.FullscreenBegan.Connect(_loadingScreenSignals.Incoming.Open, GROUP);\n"
                + "_loadingSignals.Outgoing.SetChanged.Connect(_loadingScreenSignals.Incoming.Apply, GROUP);\n"
                + "_loadingSignals.Outgoing.SetCompleted.Connect(_loadingScreenSignals.Incoming.Close, GROUP);\n"
                + "_loadingSignals.Outgoing.SetFailed.Connect(_loadingScreenSignals.Incoming.ShowFailed, GROUP);\n"
                + "\n"
                + "_loadingSignals.Outgoing.OverlayBegan.Connect(_loadingOverlayScreenSignals.Incoming.Open, GROUP);\n"
                + "_loadingSignals.Outgoing.SetChanged.Connect(_loadingOverlayScreenSignals.Incoming.Apply, GROUP);\n"
                + "_loadingSignals.Outgoing.SetCompleted.Connect(_loadingOverlayScreenSignals.Incoming.Close, GROUP);\n"
                + "_loadingSignals.Outgoing.SetFailed.Connect(_loadingOverlayScreenSignals.Incoming.Close, (set, step) => set, GROUP);",
                "LoadingConnectorSubContext - IncomingSignals");

            painter.SubHeading("Adding a crossing");
            painter.Bullet(
                "A new module that has to hear another module gets a sub-context named after the one "
                + "counterpart - HeroConnectorSubContext - never after the pair, listed on ConnectorRoot "
                + "with Add Sub Context.");
            painter.Bullet(
                "The Connector's asmdef references the counterpart's Signals assembly and whatever "
                + "Shared assembly its payloads live in, or Connect cannot infer its type and the "
                + "compiler reports CS0012.");
            painter.Bullet(
                "It translates and never decides: a list of consequences hung off one announcement "
                + "belongs in the Context that owns the decision, as a sequence.");
            painter.PageLink("Connectors", "Read: Connectors - wiring, adapting payloads, the exceptions");
            painter.PageLink("Signals", "Read: Signals - Incoming, Outgoing and the holder");
        }
    }
}

#endif