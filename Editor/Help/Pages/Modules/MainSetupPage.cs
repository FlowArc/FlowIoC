#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>The module the game starts from: MainScene, the boot, and the main screen.</summary>
    internal class MainSetupPage : ModulePage
    {
        private readonly HelpImages _images = new HelpImages();

        public override string Title => "Main Module";

        public override string Subtitle => "Launches the game, owns MainScene";

        public override FlowIcon Icon => FlowIcon.Bolt;

        public override string ModuleFolderName => "MainModule";

        public override bool InSetupSet => true;

        public override string BodyHeadline => "The module the game starts from.";

        public override string BodyTagline =>
            "MainContext begins the Boot set and opens the main screen when it completes; MainScene "
            + "carries the package's own ScreenServiceRoot, PoolServiceRoot and AssetServiceRoot beside it.";

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("The boot", DrawBoot,
                "One chain in MainContext, read top to bottom.",
                "Everything the game does before the player is in is a step bound to one internal "
                + "signal, so what the boot does is read from the Context and never from a Command.")
        };

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it owns");
            painter.Bullet(
                "MainScene, the first scene in the build list, with the Roots of the whole set "
                + "instantiated in it under three separators: the package's services, the game's "
                + "systems, and the core - ConnectorRoot, ScreenRoot and MainRoot last.");
            painter.Bullet(
                "MainScreenModule under zScreenModules: the main screen, where Easy, Medium or Hard "
                + "is picked and carried to the gameplay screen as a signal parameter.");
            painter.Bullet(
                "MainContext: the Boot set is begun here, PreloadScreensCommand and FillPoolsCommand "
                + "report into it, and the main screen opens when the set completes. MainSignals "
                + "announces BootStarted and Started, and accepts RetryBoot.");

            painter.Image(_images.Get("MainSceneHierarchy.png"),
                "MainScene: the Roots in the order their numbers put them, top to bottom.");

            painter.SubHeading("In the scene");
            painter.Table(new[] {"Root", "Initialize Order", "Role"},
                new[]
                {
                    "MainRoot", "100",
                    "Core - the frame, declared by [FlowHeader(FlowRole.Core)] on the Root. The highest number "
                    + "in the scene: its Launch dispatches the first signal, and by then every service, "
                    + "every system, the Connector and the screen layers have bound, set up and launched."
                });
            painter.PageLink("Ordering Roots", "Read: Ordering Roots - the bands and why 100");
            painter.PageLink("Root & Context", "Read: Root & Context - the phases a Root runs");

            painter.Note(
                "This module is the game's from the day it lands - rename it, gut it, rewrite the "
                + "flow. It carries no version and is never updated by the package.");
        }

        private void DrawBoot(HelpPainter painter)
        {
            painter.SubHeading("The chain");
            painter.Code(
                "CommandBinder.Bind(_internalSignals.Launch)\n"
                + "    .ToSequence<IScreenService.Commands.LoadByTag>(LoadingConstants.SCREEN_TAG)\n"
                + "    .ToSequence<ILoadingService.Commands.Begin>(MainConstants.BOOT_SET)\n"
                + "    .ToSequence<SignalDispatchCommand>(_mainSignals.Outgoing.BootStarted)\n"
                + "    .ToParallel<PreloadScreensCommand>()\n"
                + "    .ToSequence<FillPoolsCommand>()\n"
                + "    .ToSequence<ILoadingService.Commands.Await>(MainConstants.BOOT_SET)\n"
                + "    .ToSequence<SignalDispatchCommand>(_mainSignals.Outgoing.Started);\n"
                + "\n"
                + "// A retry from the loading screen runs the boot again.\n"
                + "CommandBinder.Bind(_mainSignals.Incoming.RetryBoot).ToSequence<SignalDispatchCommand>(_internalSignals.Launch);",
                "MainModule/Scripts/Runtime/RootsContexts/MainContext.cs - CommandBindings");

            painter.SubHeading("Step by step");
            painter.Bullet(
                "Launch dispatches the internal Launch signal - MainRoot is the last Root, so every "
                + "service the steps inject is already bound.");
            painter.Bullet(
                "LoadByTag puts the loading screens into the pool first. Nothing is on stage yet to "
                + "show that load on, and Begin needs the loading screen ready the frame it opens.");
            painter.Bullet(
                "Begin opens the Boot set: the loading screen comes up from the pool, and every step "
                + "reported from here on is drawn on its bar.");
            painter.Bullet(
                "BootStarted is announced for whatever may run beside the boot without slowing it - a "
                + "module that fetches its own data on that signal reports its step into the same set.");
            painter.Bullet(
                "PreloadScreensCommand and FillPoolsCommand run in parallel: every registered screen is "
                + "loaded, every pool group filled, each reporting its step and its progress.");
            painter.Bullet(
                "Await holds the chain until every step the set lists has ended - the two above and any "
                + "that BootStarted set going - then Started is announced.");
            painter.Bullet(
                "Started reaches the main screen through ConnectorModule: MainConnectorSubContext joins "
                + "it to MainScreenSignals.Incoming.OpenMainScreen, and the main screen opens.");

            painter.Note(
                "Two steps that are Commands of other modules - LoadByTag, Begin, Await - are bound "
                + "here rather than dispatched to: a Service ships its steps for other modules to bind, "
                + "so the boot reads as one sequence in one Context.");
            painter.PageLink("Loading Module", "Read: Loading Module - sets, steps and the bar");
            painter.PageLink("Connector Module", "Read: Connector Module - where Started is wired");
            painter.PageLink("Controllers", "Read: Controllers - sequences and parallel steps");
        }
    }
}

#endif