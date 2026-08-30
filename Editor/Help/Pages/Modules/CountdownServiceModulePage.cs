#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The countdown service module: what it does, how a game calls it, where the time it counts
    /// with comes from, and the button that puts it in the project.
    /// </summary>
    internal class CountdownServiceModulePage : HelpPage
    {
        private const string ModuleFolderName = "CountdownServiceModule";

        private readonly ModuleInstaller _installer =
            new ModuleInstaller(new ProjectRoot().Resolve(), new ModulesSource());

        private readonly HelpAction _install;

        private bool _isInstalled;
        private double _checkedAt = double.NegativeInfinity;

        public CountdownServiceModulePage() : base(null)
        {
            // The label and the enabled state are read every repaint rather than fixed here, so
            // the button turns itself off the moment the module lands in the project.
            _install = new HelpAction(
                () => IsInstalled() ? "Installed" : "Install",
                () => !IsInstalled(),
                Install);
        }

        public override string Title => "Countdown Service";

        public override string Subtitle => "Named timers";

        public override string Icon => "UnityEditor.AnimationWindow";

        public override HelpAction Action => _install;

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Usage", DrawUsage),
            new HelpTab("Time Source", DrawTimeSource)
        };

        /// <summary>
        /// Whether the module is in the project, answered from a cache that goes stale after a
        /// second. The underlying check walks every asmdef under Assets, and the banner asks twice
        /// per repaint - often enough that doing the walk each time would cost real frames. A
        /// second is far below noticing, and installing clears the cache outright.
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
                    "Countdown Service installed",
                    $"The module is now at {ModuleInstaller.TargetFolder}/{ModuleFolderName}.\n\n"
                    + "It is yours to edit from here - the copy in the package is only the one "
                    + "installs are made from.",
                    "OK");

                return;
            }

            EditorUtility.DisplayDialog("Countdown Service", error, "OK");
        }

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Runs named countdowns and calls back once a second while they last. A chest that "
                + "opens in an hour, an energy bar that refills, a round timer - all of them are "
                + "one call and a callback rather than a coroutine of their own.");

            painter.SubHeading("What it gives you");
            painter.Bullet(
                "A countdown is a string id. Several callers may listen to the same one: the first "
                + "sets how long it runs, the rest only add their callbacks to it.");
            painter.Bullet(
                "Ticks arrive as seconds left, or as 0..1 of the whole countdown when that suits "
                + "the bar you are filling better.");
            painter.Bullet(
                "It measures forward as well as down. EvaluateElapsedTime counts up from a moment "
                + "in the past, with or without an end.");
            painter.Bullet(
                "Nothing has to wait for it. A countdown asked for before the module is up is "
                + "held, and starts on its own once the time source answers.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.CountdownService and injects "
                + "ICountdownService directly. That is the one thing the architecture lets a module "
                + "reach across a boundary for.");

            painter.SubHeading("Its signals");
            painter.Paragraph(
                "Incoming.Initialize prepares the time source and starts the clock. The module's own "
                + "context dispatches it on setup, so a game sends it again only to retry a source "
                + "that failed. Outgoing.Ready says whether that worked.");

            painter.SubHeading("Trying it out");
            painter.Paragraph(
                "The module ships with a test module beside it. Run "
                + "Tools > FlowIoC > Modules > Countdown Service > Build Test Scene and press Play: a "
                + "Start and a Stop button, a countdown, and the elapsed time next to it.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.Paragraph(
                "Inject the service and start a countdown. CountDownInstantly starts from now and "
                + "hands back the moment it started from, which is what you save if the countdown "
                + "has to survive a restart.");
            painter.Code(
                "[Inject] private ICountdownService _countdown { get; set; }\n"
                + "\n"
                + "DateTime? startedAt = _countdown.CountDownInstantly(\"chest\", 3600,\n"
                + "    countdownTick: remaining => _view.SetLabel($\"{remaining:0}s\"),\n"
                + "    countdownComplete: () => _signals.Outgoing.ChestReady.Dispatch());");

            painter.Paragraph(
                "Coming back to a countdown that began earlier - a chest the player left running - "
                + "means giving the start time rather than the time left. The service works out "
                + "what is left, and completes the countdown straight away if it already ran out.");
            painter.Code(
                "_countdown.CountDownFrom(\"chest\", 3600, savedStartTime,\n"
                + "    checkActive: isRunning => _view.SetWaiting(!isRunning),\n"
                + "    countdownTick: remaining => _view.SetLabel($\"{remaining:0}s\"),\n"
                + "    countdownComplete: () => _signals.Outgoing.ChestReady.Dispatch());");

            painter.SubHeading("Joining and leaving");
            painter.Paragraph(
                "A second screen showing the same chest adds itself to the countdown already "
                + "running instead of starting one. Remove the callbacks when the screen closes: a "
                + "countdown nothing is listening to any more is dropped with them.");
            painter.Code(
                "_countdown.AddCallbacks(\"chest\", countdownTick: OnTick);\n"
                + "_countdown.RemoveCallbacks(\"chest\", countdownTick: OnTick);");

            painter.SubHeading("Filling a bar");
            painter.Paragraph(
                "Pass isPercentageTick and the tick arrives as 0..1 of the whole countdown, so the "
                + "value goes straight onto a fillAmount with no arithmetic at the call site.");
            painter.Code(
                "_countdown.AddCallbacks(\"chest\", isPercentageTick: true,\n"
                + "    countdownTick: fraction => _view.SetFill(fraction));");

            painter.SubHeading("Counting up");
            painter.Paragraph(
                "EvaluateElapsedTime measures forward from a moment in the past. Leave the duration "
                + "at zero and it runs for as long as the game does - a session timer, or how long "
                + "since the player last collected something.");
            painter.Code(
                "_countdown.EvaluateElapsedTime(\"session\", sessionStart,\n"
                + "    checkActive: null,\n"
                + "    elapsedTimeTick: elapsed => _view.SetPlayed(elapsed));");

            painter.Space();
            painter.Note(
                "Stop(id) ends a countdown early. Its stop callbacks run and its complete callbacks "
                + "do not, because it never completed.");
        }

        private void DrawTimeSource(HelpPainter painter)
        {
            painter.Paragraph(
                "Everything the module counts with comes from one interface. It ships with the "
                + "device clock behind it, which needs no network and is ready the moment it is "
                + "asked - that is what lets the module work as soon as it is installed.");
            painter.Code(
                "public interface ITimeSource\n"
                + "{\n"
                + "    bool IsReady { get; }\n"
                + "    DateTime UtcNow { get; }\n"
                + "    void Prepare(Action<bool> onPrepared);\n"
                + "}");

            painter.Paragraph(
                "A player who moves the device clock moves every countdown with it. A game that "
                + "cares writes its own source - a server time endpoint, a platform service - and "
                + "names it in the module's context. The module lives in the game's own Assets, so "
                + "that line is the game's to change.");
            painter.Code(
                "public override void InjectionBindings()\n"
                + "{\n"
                + "    base.InjectionBindings();\n"
                + "\n"
                + "    InjectionBinder.Bind<ICountdownModel, CountdownModel>();\n"
                + "    InjectionBinder.Bind<ITimeSource, ServerTimeSource>();   // was DeviceTimeSource\n"
                + "\n"
                + "    InjectionBinderCrossContext.Bind<ICountdownService, CountdownService>();\n"
                + "}");

            painter.Paragraph(
                "Prepare is what makes a source that has to fetch something workable. It may answer "
                + "immediately, as the device clock does, or some time later. Until it answers "
                + "IsActive is false, GetTime returns null, and countdowns asked for meanwhile wait "
                + "- their checkActive callback is told false now and true when the clock arrives.");

            painter.Space();
            painter.Note(
                "Read the time through GetTime rather than from DateTime.UtcNow at the call site. "
                + "That is what makes swapping the source change every reading at once instead of "
                + "only the ones that remembered to ask.");
        }
    }
}

#endif