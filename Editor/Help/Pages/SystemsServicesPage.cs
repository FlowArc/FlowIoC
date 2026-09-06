#if UNITY_EDITOR

using System.Collections.Generic;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// The logic side of a module, and the one page that says which of the three kinds a module is.
    /// A Service and a System are close enough to be confused and far enough apart that confusing
    /// them costs you an assembly reference, so they are read together rather than on two pages.
    /// </summary>
    internal class SystemsServicesPage : HelpPage
    {
        public SystemsServicesPage() : base(null)
        {
        }

        public override string Title => "Systems and Services";

        public override string Subtitle => "The logic a module owns";

        public override string Icon => "cs Script Icon";

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("A Service", DrawService),
            new HelpTab("A System", DrawSystem),
            new HelpTab("Splitting", DrawSplitting),
            new HelpTab("Rules", DrawRules)
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Hero(
                "A Service travels. A System belongs to this game.",
                "That one sentence decides which folder the code goes in, whether another module "
                + "may reference it, and what happens to it when the game around it changes.");

            painter.SubHeading("Which of the three a module is, is something you can see");
            painter.Bullet("A Service has files under Scripts/Runtime/Services/.");
            painter.Bullet("A screen module has a context deriving from ScreenSubContext<TView, TMediator>.");
            painter.Bullet("Everything else is a System.");
            painter.Paragraph(
                "There is no fourth question to ask. A module written for the game at hand is a "
                + "System, which is why Role in Create Module starts there.");

            painter.Space();
            painter.SubHeading("What separates them");
            painter.Bullet(
                "A Service depends on nothing. It does not know another Service, and it does not "
                + "know a System or a Screen either. It answers the input it is given: a countdown, "
                + "a parser, a storage wrapper.");
            painter.Bullet(
                "A System is written for this game and may lean on other Systems and Services - "
                + "waiting on a signal they raise, or working from data they publish. What it may "
                + "not do is reference another module's assembly.");
            painter.Bullet(
                "A Service is the one direct crossing FlowIoC allows: reference its assembly, "
                + "inject its interface. Everything else crosses through a Connector.");

            painter.Space();
            painter.Note(
                "A finished Service's assembly gets no later additions. Work that arrives afterwards "
                + "goes somewhere else, or the Service quietly turns into a module of the game it "
                + "happens to sit in - and stops being reusable, which was the whole point.");
        }

        /// <summary>
        /// The pair, the binding that makes it usable from outside, and the two ways in and out
        /// that are the whole of a Service's contract with the rest of the project.
        /// </summary>
        private void DrawService(HelpPainter painter)
        {
            painter.Hero(
                "An interface and an implementation, bound across contexts.",
                "A Service that binds its interface to its own Context is a Service nobody can use.");

            painter.SubHeading("Binding it so it can be reached");
            painter.Code(
                "public override void InjectionBindings()\n"
                + "{\n"
                + "    base.InjectionBindings();\n"
                + "\n"
                + "    InjectionBinder.Bind<ITimeSource, DeviceTimeSource>();\n"
                + "\n"
                + "    // The one type other modules reference directly.\n"
                + "    InjectionBinderCrossContext.Bind<ICounterService, CounterService>();\n"
                + "}",
                "CounterServiceContext.cs");
            painter.Paragraph(
                "InjectionBinder keeps a binding inside the module; InjectionBinderCrossContext "
                + "offers it to everyone. The module that uses the Service references its assembly "
                + "and injects the interface - that reference is the crossing, and it is allowed.");
            painter.Code(
                "[Inject] private ICounterService _counterService { get; set; }",
                "In a Command of another module");

            painter.Space();
            painter.SubHeading("The two ways in");
            painter.Paragraph(
                "A caller either calls the interface, or dispatches one of the Commands the Service "
                + "ships. The second exists for the case where the work has to be a step in a "
                + "sequence with the next step waiting on it.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.CelebrateWin)\n"
                + "    .ToSequence<HapticTriggerCommand>()\n"
                + "    .ToSequence<PlayWinAnimationCommand>();",
                "A Command the Service ships, used in a sequence");
            painter.Paragraph(
                "A generic ToSequence<DispatchSignalCommand> would start the same work, but the "
                + "sequence carries on without waiting for it. A Command of the Service's own is "
                + "what holds the line, and it is what puts the step in the Flow Console.");

            painter.Space();
            painter.SubHeading("The two ways out");
            painter.Bullet(
                "A signal, when what happened may concern the whole game.");
            painter.Bullet(
                "A callback the caller handed in, when the answer is only for whoever asked.");
            painter.Code(
                "bool CountDownFrom(string id, int duration, DateTime startTime,\n"
                + "    Action<bool> checkActive, bool isPercentageTick = false,\n"
                + "    Action<float> counterTick = null, Action counterComplete = null,\n"
                + "    Action counterStop = null, Action<float> elapsedTimeTick = null);",
                "ICounterService - every announcement is an Action the caller passed in");
            painter.Paragraph(
                "Whoever started the counter is who wants the tick, so there is nothing to announce "
                + "to the rest of the game. A Service therefore does not automatically get a signal "
                + "holder: it gets one when something outside it has to be told and no subscription "
                + "already carries that.");

            painter.Space();
            painter.Note(
                "A Service that more than one module needs gets a module of its own, named for what "
                + "it does - CounterModule, not CounterServiceModule - while its Root and Context "
                + "keep the suffix, because that is what the inspector reads to paint them.");
        }

        /// <summary>
        /// The half of the architecture most often written when it did not need to be: a System is
        /// a surface, not a place to put work.
        /// </summary>
        private void DrawSystem(HelpPainter painter)
        {
            painter.Hero(
                "A System needs no System.cs.",
                "The work is followed through the command flow the Context declares. A module can "
                + "be a complete System without a single type named ...System.");

            painter.SubHeading("When one arrives");
            painter.Bullet(
                "To collapse many injections into one. A Command injects IMapSystem once and writes "
                + "_map.Grid.Build(...) instead of injecting eight Models separately.");
            painter.Bullet(
                "To make what is available discoverable. Press the dot and a short tidy list "
                + "appears, rather than everything the module can do at once.");

            painter.Space();
            painter.SubHeading("It holds members, never work");
            painter.Code(
                "internal class MapSystem : IMapSystem, IConstructable\n"
                + "{\n"
                + "    [Inject] public MapLevelSubSystem Level { get; set; }\n"
                + "    [Inject] public MapGridModel      Grid  { get; set; }\n"
                + "    [Inject] public IThemeModel       Theme { get; set; }\n"
                + "\n"
                + "    public void PostConstruct() => LoadCanvasSettings();\n"
                + "}",
                "MapSystem.cs - Scripts/Runtime/Systems");
            painter.Paragraph(
                "The moment a method on a System does something, that work belongs in a Command - "
                + "which is what keeps the step visible in the Flow Console and lets a sequence wait "
                + "for it. PostConstruct assembling the surface is the whole allowance.");

            painter.Space();
            painter.SubHeading("Built out of sub systems");
            painter.Paragraph(
                "Models appear among a System's members where the module's own state lives, but the "
                + "sub system is the unit it is assembled from. When a module has a System, a "
                + "Command reaches that module's Models through it rather than injecting a Model "
                + "directly; a module without a System is the ordinary case, and its Commands inject "
                + "Models as they always have.");

            painter.Space();
            painter.SubHeading("A Model, a sub system, or a module of its own");
            painter.Paragraph(
                "A Model owns the module's state and its data. A sub system computes, and may read "
                + "what other modules publish. MapGridModel owning a dictionary it mutates is a "
                + "Model; MapLevelSubSystem reading two config lists and deriving an index from them "
                + "is a sub system.");
            painter.Bullet(
                "The sub system itself holds its own working data - the config it reads, the values "
                + "it derives.");
            painter.Bullet("A Model holds the module's state.");
            painter.Bullet(
                "A module of its own holds data that is large, or whose scope reaches past this "
                + "module.");
        }

        /// <summary>
        /// Why both split the same way, and the shape that makes the split worth having.
        /// </summary>
        private void DrawSplitting(HelpPainter painter)
        {
            painter.Hero(
                "Two reasons to split, and the second is the common one.",
                "A Service and a System both divide into Services/Sub/ and Systems/Sub/ - because "
                + "the unit grew, or because a chained surface reads better than thirty verbs.");

            painter.SubHeading("Grouped by noun, and a builder");
            painter.Code(
                "public interface IScreenService\n"
                + "{\n"
                + "    LoadSubService  Load  { get; set; }\n"
                + "    CheckSubService Check { get; set; }\n"
                + "    HideSubService  Hide  { get; set; }\n"
                + "\n"
                + "    IScreenBuilderSubService Open<T>(int managerId = 0) where T : IScreenBody;\n"
                + "}",
                "IScreenService - five nouns instead of thirty verbs");
            painter.Code(
                "_screenService.Open<SettingsScreenView>()\n"
                + "    .OpenInLayer(1)\n"
                + "    .SetParameters(id)\n"
                + "    .Show();",
                "Every step returns the builder, and nothing happens until Show()");

            painter.Space();
            painter.Paragraph(
                "The grouping exists for whoever writes the call, not for the file size. Type the "
                + "service, press the dot, pick one, press it again - each step narrows what comes "
                + "next instead of showing everything at once.");

            painter.Space();
            painter.Note(
                "A sub service or a sub system is an implementation detail. It is reached through "
                + "the Service or the System that owns it, and never bound across contexts on its "
                + "own - a caller that injects one has reached past the interface that was meant to "
                + "be the whole surface.");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A Service has files under Services/, a screen module has a ScreenSubContext, everything else is a System.");
            painter.Bullet("A Service depends on nothing - not another Service, not a System, not a Screen.");
            painter.Bullet("A finished Service's assembly gets no later additions.");
            painter.Bullet("A Service is bound with InjectionBinderCrossContext, or no other module can use it.");
            painter.Bullet("A Service is driven by a call to its interface, or by a Command it ships for the case where a sequence has to wait.");
            painter.Bullet("A Service answers with a signal when the game may care, and with a callback the caller handed in when only that caller does.");
            painter.Bullet("A System is specific to this game and never appears in another module's assembly.");
            painter.Bullet("A System needs no System.cs. One arrives when the module wants a surface.");
            painter.Bullet("A System type holds injected members and no method that does work. PostConstruct may assemble it.");
            painter.Bullet("A System is built out of sub systems, with Models among its members where the module's state lives.");
            painter.Bullet("When a module has a System, its Commands reach its Models through it.");
            painter.Bullet("A Model owns the module's state and data; a sub system computes.");
            painter.Bullet("Both split into Services/Sub/ and Systems/Sub/, for size or for a chained surface.");
            painter.Bullet("A sub service or sub system is never bound across contexts.");
            painter.Bullet("Services/ and Systems/ are optional folders, ticked from Role in Create Module.");
        }
    }
}

#endif
