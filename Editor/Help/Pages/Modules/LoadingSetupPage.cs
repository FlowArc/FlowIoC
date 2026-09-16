#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The loading module: what a set is, how a Command reports into one, the two fan-out points
    /// of the shipped boot, and the three setup mistakes that show up as nothing on screen. The
    /// one module of the setup set that carries a version: it is a service, and its improvements
    /// reach a game through the Module Library like any other service's.
    /// </summary>
    internal class LoadingSetupPage : ModulePage
    {
        private readonly HelpImages _images = new HelpImages();

        public override string Title => "Loading";

        public override string Subtitle => "Sets, steps and the bar that shows them";

        public override FlowIcon Icon => FlowIcon.Stopwatch;

        public override string ModuleFolderName => "LoadingModule";

        public override bool InSetupSet => true;

        public override string BodyHeadline => "It shows, waits and times. It loads nothing.";

        public override string BodyTagline =>
            "The pool service fills its groups, the screen service preloads its screens, a game "
            + "module fetches its data - and the Command doing each of those reports its step to "
            + "ILoadingService. The service draws the bar, ends the set when every step it lists "
            + "has ended, and says so when a set goes quiet.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "A set: steps with weights and messages, declared in CD_LoadingSets, shown as one bar. "
                + "Fullscreen for the boot, Overlay for a feature waiting on its own data, Silent for "
                + "work the player never sees.");
            painter.Bullet(
                "Parallel by nature. A set accepts its steps in any order and any number at once; the "
                + "order of the work is the caller's Context, not the set's.");
            painter.Bullet(
                "A second bar. A step may stand for another set, and while that set runs the "
                + "fullscreen screen draws it under the first bar - a download with its bytes and "
                + "speed, say.");
            painter.Bullet(
                "A set that goes quiet is reported. Nothing hangs silently: after StallWarningSeconds "
                + "with no report, the console names the steps not started and the steps still running.");
            painter.Bullet(
                "Every step is timed on the module's channel, so the Flow Console reads "
                + "\"Boot completed in 4.20 s - Screens 1.12, Pools skipped\" without a stopwatch anywhere.");

            painter.Image(_images.Get("LoadingScreen.png"),
                "LoadingTestScene: the Boot set's bar over the download set it waits for.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.Loading and injects "
                + "ILoadingService directly. It offers no \"is X loaded\" query on purpose: every "
                + "module knows its own readiness and announces it, and a readiness registry would "
                + "let one module depend on another's load without a Connector.");

            painter.SubHeading("The shipped boot");
            painter.Paragraph(
                "MainContext binds the boot as one chain: load the loading screens into the pool, "
                + "begin the Boot set, announce BootStarted for what may run beside the boot, "
                + "preload the screens and fill the pools in parallel, wait for the set, announce "
                + "Started. The loading screen opens on the set's own signal, through "
                + "LoadingConnectorSubContext, and closes when the set ends. It goes into the pool "
                + "first because nothing is up to show its own load on - and a pooled open is on "
                + "stage the same frame, so every load after it is drawn on the bar.");
            painter.Code(
                "CommandBinder.Bind(_internalSignals.Launch)\n"
                + "    .ToSequence<IScreenService.Commands.LoadByTag>(LoadingConstants.SCREEN_TAG)\n"
                + "    .ToSequence<ILoadingService.Commands.Begin>(MainConstants.BOOT_SET)\n"
                + "    .ToSequence<SignalDispatchCommand>(_mainSignals.Outgoing.BootStarted)\n"
                + "    .ToParallel<PreloadScreensCommand>()\n"
                + "    .ToSequence<FillPoolsCommand>()\n"
                + "    .ToSequence<ILoadingService.Commands.Await>(MainConstants.BOOT_SET)\n"
                + "    .ToSequence<SignalDispatchCommand>(_mainSignals.Outgoing.Started);");
            painter.Paragraph(
                "BootStarted and Started are the two fan-out points. What may run beside the boot "
                + "without slowing it - an SDK initialising, a profile fetch - hangs off BootStarted "
                + "and reports into whichever set the asset puts it in. Heavy preloads that would "
                + "share bandwidth with the critical path hang off Started, after the player is in.");
            painter.Paragraph(
                "Content the stores deliver - a fast-follow pack that has not landed on an early open "
                + "- is the Asset Delivery module's step, IAssetDeliveryService.Commands.EnsurePromised, "
                + "bound right after Begin; its page has the line and the Content row the set needs.");
            painter.PageLink("Asset Delivery");

            painter.SubHeading("The screens come from Resources, the art from Addressables");
            painter.Paragraph(
                "Both loading screens declare ScreenLoadCVO.Resource and keep their prefabs in their "
                + "modules' Resources folders, so the bar is on stage before Addressables has "
                + "initialised - seconds on a remote catalogue over a bad connection, with nothing "
                + "to show otherwise. What a game changes between releases stays addressable: "
                + "T_LoadingBackground in LoadingScreenModule/Art is the splash behind the bar, "
                + "bundled on the prefab as the fallback and loaded again through IAssetService from "
                + "the screen context's Launch. Once the load lands the addressable copy replaces the "
                + "bundled one; on this run's first boot that may be after the bar has gone, and the "
                + "cache answers from the next boot on.");
            painter.Note(
                "Important: replace the asset and keep its address. The screen asks for "
                + "T_LoadingBackground by name; the installer registered the file in Art under that "
                + "name in the Local_Screen-Loading group, and a new file dropped in beside it is "
                + "not addressable until it is. A load that brings nothing is an error from the asset "
                + "service and leaves the bundled art on stage.");

            painter.SubHeading("Reporting a step");
            painter.Code(
                "[Inject] private ILoadingService _loadingService { get; set; }\n"
                + "\n"
                + "ILoadingStep step = _loadingService.Report(\"Pools\");\n"
                + "step.Start();\n"
                + "await _poolService.InitializeGroupAsync(\"Match\");\n"
                + "step.Progress(0.5f);\n"
                + "step.Detail(\"Match\");\n"
                + "step.Complete();");
            painter.Paragraph(
                "The Command names only its step; the asset says which set the step belongs to, and "
                + "so whether the boot waits for it. Start and Complete are the pair; Skip and Fail "
                + "are the other two ends. A step that has nothing to do calls Skip and still counts "
                + "its weight, so the bar reaches the end. A Complete, Skip or Fail on a step that "
                + "never started is accepted - instantaneous work need not write two lines.");

            painter.SubHeading("Beginning and waiting");
            painter.Paragraph(
                "The chain that owns the moment binds the two shipped Commands: ILoadingService.Commands.Begin "
                + "opens the presentation, ILoadingService.Commands.Await holds the chain until the set has "
                + "ended - its own steps are already waited on by the group; the wait is for the "
                + "steps other modules report on their own. A failed set stops the chain there.");
            painter.Code(
                ".ToSequence<ILoadingService.Commands.Begin>(\"EnterMatch\")\n"
                + "...\n"
                + ".ToSequence<ILoadingService.Commands.Await>(\"EnterMatch\")");
            painter.Paragraph(
                "A feature that waits for its own data waits in its own module: the first Command of "
                + "the clan panel's open flow retains until its Model is ready, and begins an Overlay "
                + "set for the spinner meanwhile. Nothing asks the loading service whether clan data "
                + "is in - the clan module knows, and tells its neighbours with an Outgoing.");

            painter.SubHeading("Sets");
            painter.Paragraph("What a CD_LoadingSets asset might hold - three sets, and the steps of each:");
            painter.Table(new[] {"Set", "Step", "Weight", "Label", "Notes"},
                new[] {"<b>Boot</b>", "", "", "", "Fullscreen, StallWarningSeconds 10"},
                new[] {"", "Screens", "2", "Preparing screens", ""},
                new[] {"", "Pools", "1", "Warming up", ""},
                new[] {"", "Assets", "3", "Fetching assets", "ChildSet: Download"},
                new[] {"<b>Download</b>", "", "", "", "Silent"},
                new[] {"", "Catalog", "1", "", ""},
                new[] {"", "Bundles", "4", "Downloading", ""},
                new[] {"<b>PostBoot</b>", "", "", "", "Silent"},
                new[] {"", "Ads", "1", "", ""},
                new[] {"", "ClanData", "1", "", ""});
            painter.Paragraph(
                "Progress is the weighted sum of the steps: an ended step counts its full weight, a "
                + "running one what it last reported, a pending one nothing. A step with a ChildSet "
                + "takes that set's progress and completes when it completes - and the fullscreen "
                + "screen draws the child on the second bar while it runs.");
            painter.Paragraph(
                "A Silent set needs no Begin: its first report begins it, and the last one ends it. "
                + "Several sets may be alive at once - a visible Boot, a silent PostBoot, and later "
                + "an EnterMatch before every match - and a set that ended may be begun again.");
            painter.Note(
                "Whether the boot waits for a step is where the step sits, not how it is written. "
                + "Move ClanData from Boot to PostBoot and the main screen opens without it; the "
                + "Command that reports ClanData does not change.");

            painter.SubHeading("Retry");
            painter.Paragraph(
                "The fullscreen screen shows the failed step and a Retry button. Its RetryClicked "
                + "reaches MainSignals.Incoming.RetryBoot through the Connector, and the boot runs "
                + "again from the top. A game that re-runs only the failed step reports it again: the "
                + "set reopens, the steps that succeeded keep their end, and it completes on its own.");

            painter.SubHeading("Setup that fails quietly");
            painter.Note(
                "Important: a step key that no set lists is an error at the Command that reported "
                + "it. Every step a Command names has to be in exactly one set of CD_LoadingSets on "
                + "LoadingServiceRoot's adapter.");
            painter.Note(
                "Important: a Fullscreen or Overlay set that nobody called Begin on begins at its "
                + "first report, with a warning. Begin it first, from the chain that owns the moment, "
                + "so its screen is up before the first step starts.");
            painter.Note(
                "Important: a step whose Root is not in the scene never reports, and the only thing "
                + "that says so is the stall warning. Read the step names in it - a step the asset "
                + "lists that nothing reports is either a missing Root or a line to delete.");

            painter.SubHeading("Trying it out");
            painter.Paragraph(
                "LoadingTestScene under the test module's Scenes folder walks every state once: "
                + "parallel steps, the second bar, a step that fails and the retry that succeeds, "
                + "and a silent set running beside it all.");
        }
    }
}

#endif
