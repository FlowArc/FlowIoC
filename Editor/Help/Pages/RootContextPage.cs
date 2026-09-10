#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// The pair a module cannot be without, read in four passes: the two of them as a picture,
    /// then the Root as a scene object, then the Context as a list of declarations, then the
    /// rules. The introduction carries no code - the walk under the diagram carries all of it.
    /// </summary>
    internal class RootContextPage : HelpPage
    {
        public RootContextPage() : base(Build())
        {
        }

        public override string Title => "Root & Context";

        public override FlowIcon Icon => FlowIcon.ListNested;

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Root", DrawRoot,
                "A Root takes the colour of whatever it roots.",
                "It decides that from its own name, so the name is not decoration - it is what the "
                + "scene says about the module at a glance."),
            new HelpTab("Context", DrawContext,
                "A Context declares. It never decides.",
                "Three groups of methods, and each answers a different question: what the module is "
                + "made of, what it has to ready, and what it says first."),
            new HelpTab("Rules", DrawRules,
                "The rules, in one list.",
                "What a Root carries into the scene, and what a Context is allowed to declare.")
        };

        protected override string BodyHeadline => "A module joins the game by being in the scene.";

        protected override string BodyTagline =>
            "Nothing reaches in from outside to start it. The Root puts it there, and the Context "
            + "says what it is made of.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Parts(
                new HelpPart("Root",
                    "The module's one presence in the scene, and normally an empty class. Drop it "
                    + "in and the module starts.",
                    "class PlayerRoot : Root<PlayerContext>"),
                new HelpPart("Context",
                    "What the module is made of, declared and nothing else. A Context that needs "
                    + "an if is deciding something, and that belongs in a Command.",
                    "class PlayerContext : Context"));

            painter.SubHeading("From the scene to the first dispatch");
            painter.Paragraph(
                "Eight steps, and the whole of a module's startup. Walk them with the buttons "
                + "under the diagram; the phases they name are on the Context tab.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.Note(
                "Setup does not run until every Root in the scene has finished binding. That "
                + "barrier - not the Initialize Order - is what makes reaching across modules safe.");
        }

        /// <summary>
        /// The Root as a scene object: what its name says, what hangs off it, what it takes to
        /// outlive a scene, and what it may host besides its own context.
        /// </summary>
        private void DrawRoot(HelpPainter painter)
        {
            painter.SubHeading("What a Root is called");
            painter.Paragraph(
                "A module that exists to provide a Service keeps the Service suffix on its Root and "
                + "Context even when the module itself does not. CounterModule holds Modules.Counter, "
                + "and inside it sit CounterServiceRoot and CounterServiceContext beside "
                + "ICounterService. CounterRoot would be drawn as a plain Root instead, and the scene "
                + "would stop saying what kind of module that is.");
            painter.Paragraph(
                "A test module's Root is read the same way: a name ending in TestRoot is drawn in "
                + "the Test grey, so a scene says which Roots exercise a module and which are the "
                + "module. The rule is the Root's alone - a View in a test module is still a View.");
            painter.Code(
                "public class CounterServiceRoot : Root<CounterServiceContext> { }\n"
                + "public class PlayerSystemRoot   : Root<PlayerSystemContext>   { }\n"
                + "\n"
                + "[FlowHeader(FlowRole.Core)]\n"
                + "public class MainRoot           : Root<MainContext>           { }",
                "Service, System and Core - what Create Module asks as Role");
            painter.Paragraph(
                "Core is the one role a name cannot carry. A Core module is part of the project's "
                + "frame rather than of its game - there is one Main, one Screen and one Connector "
                + "in a project - so its name has nothing to disambiguate from and takes no suffix. "
                + "The attribute is what tells the bar, and it wins over every other reading.");

            painter.Separator();
            painter.SubHeading("What hangs off a Root");
            painter.Paragraph(
                "A GameObject the module needs in the scene goes under its Root. The Root is the "
                + "module's one presence there, so an EventSystem, an adapter, anything the module "
                + "owns hangs off it rather than sitting loose beside it.");

            painter.Separator();
            painter.SubHeading("Living past a scene");
            painter.Paragraph(
                "A Root otherwise lives and dies with its scene. A module whose work outlives one - "
                + "input, audio, analytics - makes its Root persistent in BeforeCreateContext, which "
                + "runs just before the context is built. The reparenting is not decoration: Unity "
                + "marks only root level objects as do not destroy, so a Root authored under "
                + "something else has to detach itself before it can survive.");
            painter.Code(
                "protected override void BeforeCreateContext()\n"
                + "{\n"
                + "    transform.SetParent(null);\n"
                + "    DontDestroyOnLoad(gameObject);\n"
                + "}");

            painter.Separator();
            painter.SubHeading("What Add Sub Context offers");
            painter.Paragraph(
                "A Root can host contexts other than its own, and the button under the list offers "
                + "the ones that may honestly go there. A context that some Root declares as its "
                + "Root<T> is built by that Root already, so it is not offered: adding it to a second "
                + "Root would build a second instance of it and run the same bindings twice.");
            painter.Paragraph(
                "A module meant to be hosted on another module's Root says so on its context, and is "
                + "offered again. ExcludeFromContextWindow says the opposite, and wins over it.");
            painter.Code("[AllowAsSubContext]\npublic class CameraContext : Context { }");
            painter.Paragraph(
                "Create Module offers the same attribute as a toggle on a main module that gets a "
                + "Root. It starts unticked, because a module with a Root of its own is the ordinary "
                + "case.");
            painter.Paragraph(
                "What is left is offered with the kind it is - SCREEN, CONNECTOR - and with the Roots "
                + "that already list it, read from the open scenes. Those are sorted to the bottom "
                + "under a heading of their own, and stay clickable: the same screen on two Roots with "
                + "two ManagerIds is a deliberate thing, and the window says so rather than deciding "
                + "for you.");
            painter.Paragraph(
                "Connector sub-contexts are the other half of the rule: they are offered on the "
                + "Connector Root and nowhere else, and every other Root is offered everything but "
                + "those. A context counts as a Connector's when its name says so - "
                + "HeroConnectorSubContext - or when it carries FlowHeader(FlowRole.Connector).");

            painter.Separator();
            painter.SubHeading("An entry points at the context's script");
            painter.Paragraph(
                "A Root's entry holds the context's own script asset, not just its name. That is what "
                + "makes the wiring something the project can see: renaming the class inside its file "
                + "or moving the file keeps working, and deleting the module the context lives in "
                + "leaves a reference the Root reports instead of a name that quietly stops resolving.");
            painter.Paragraph(
                "The name is still there and is what the game reads at runtime, because MonoScript is "
                + "an Editor type and a build has none. It is rewritten from the script whenever the "
                + "inspector or a generator touches the entry, so the two never drift apart.");
            painter.Paragraph(
                "An entry with no script says which of two things it is. Where the context still "
                + "exists, Resolve links it in one press - that is an entry authored before this, or "
                + "one somebody cleared. Where nothing compiles to the name, the module is gone or "
                + "renamed and there is nothing to press: either the entry goes, or the context comes "
                + "back.");

            painter.Separator();
            painter.SubHeading("Deleting the module a context lives in");
            painter.Paragraph(
                "Delete Module takes the module's sub-contexts out of the Roots that list them, and "
                + "asks first. Remove them from every Root, go through them one at a time, or remove "
                + "none, see where they are and keep the module. The question comes before anything "
                + "is deleted, so cancelling leaves the module whole - its assemblies, its settings "
                + "files and its folder are all still there.");
            painter.PageLink("Delete Module", "What each answer does");
        }

        /// <summary>
        /// The Context as a list of declarations, and the three things each of its phases is for.
        /// </summary>
        private void DrawContext(HelpPainter painter)
        {
            painter.SubHeading("The binding phases declare");
            painter.Paragraph(
                "SignalBindings, InjectionBindings, MediationBindings and CommandBindings say what "
                + "the module is made of, and decide nothing. They run in that order, because each "
                + "refers to what the one before it bound.");
            painter.Code(
                "public override void SignalBindings()\n"
                + "{\n"
                + "    base.SignalBindings();\n"
                + "    _signals = InjectionBinderCrossContext.Bind<PlayerSignals>();\n"
                + "}\n"
                + "\n"
                + "public override void InjectionBindings()\n"
                + "{\n"
                + "    base.InjectionBindings();\n"
                + "    InjectionBinder.Bind<IPlayerModel, PlayerModel>();\n"
                + "}\n"
                + "\n"
                + "public override void MediationBindings()\n"
                + "{\n"
                + "    base.MediationBindings();\n"
                + "    MediationBinder.Bind<HudView>().To<HudMediator>();\n"
                + "}\n"
                + "\n"
                + "public override void CommandBindings()\n"
                + "{\n"
                + "    base.CommandBindings();\n"
                + "\n"
                + "    CommandBinder.Bind(_signals.Incoming.AddCurrency)\n"
                + "        .ToSequence<AddCurrencyCommand>()\n"
                + "        .ToSequence<SavePlayerCommand>();\n"
                + "}",
                "PlayerContext.cs");

            painter.Separator();
            painter.SubHeading("Two binders, and the one question that picks between them");
            painter.Paragraph(
                "Every Context makes an InjectionBinder of its own the moment it starts. "
                + "InjectionBinderCrossContext is not its own: RootsManager makes one for the whole "
                + "scene and hands that same instance to every Context in it. The two have the same "
                + "methods, and differ only in who can see what they hold.");
            painter.Bullet(
                "InjectionBinder - this Context. Models, sub-services, and whichever implementation "
                + "the module picked for itself.");
            painter.Bullet(
                "InjectionBinderCrossContext - every Context in the scene. The signal holder, and "
                + "the Service interface other modules inject.");
            painter.Paragraph(
                "Bind across when something outside this Context has to see the object, and locally "
                + "when it must not. A Model is local: Create Model writes InjectionBinder.Bind, and "
                + "the only way into a Model is a Command of its own module anyway.");
            painter.Code(
                "public override void InjectionBindings()\n"
                + "{\n"
                + "    base.InjectionBindings();\n"
                + "\n"
                + "    InjectionBinder.Bind<ICounterModel, CounterModel>();\n"
                + "    InjectionBinder.Bind<ITimeSource, DeviceTimeSource>();\n"
                + "\n"
                + "    // The one type other modules reference directly, which is what makes this a Service.\n"
                + "    InjectionBinderCrossContext.Bind<ICounterService, CounterService>();\n"
                + "}",
                "CounterServiceContext.cs - the module that ships with the package");
            painter.Paragraph(
                "The signal holder is the other cross-context binding, and for the same reason: a "
                + "Connector has to reach it. Bind it locally and the Connector's GetInstance finds "
                + "nothing.");

            painter.Separator();
            painter.SubHeading("Which one an [Inject] finds");
            painter.Paragraph(
                "The injector asks the local binders first and the shared one last, so a local "
                + "binding of a type hides a cross-context binding of the same type.");
            painter.Code(
                "// A Context asks, in this order:\n"
                + "//   1. its own InjectionBinder\n"
                + "//   2. the InjectionBinder of each of its sub-contexts\n"
                + "//   3. InjectionBinderCrossContext\n"
                + "//\n"
                + "// A sub-context asks, in this order:\n"
                + "//   1. its own InjectionBinder\n"
                + "//   2. InjectionBinderCrossContext");
            painter.Note(
                "Important: the list is not symmetrical. A Context reaches into its sub-contexts, "
                + "and a sub-context does not reach back. A screen sub-context that injects its "
                + "parent module's Model gets null unless that Model was bound across.");
            painter.Paragraph(
                "The two providers are there without being bound at all, because the Context binds "
                + "them across for you.");
            painter.Code(
                "[Inject] private ICoroutineProvider _coroutineProvider { get; set; }\n"
                + "[Inject] private IUpdateProvider    _updateProvider    { get; set; }");

            painter.Separator();
            painter.SubHeading("What DestroyContext takes back");
            painter.Paragraph(
                "Tearing a Context down empties its own binder and takes out of the shared one "
                + "everything this Context bound there - its signal holder, its Service - so a "
                + "module's public surface lives exactly as long as its Root. A scene's module goes "
                + "with the scene and is bound fresh when the scene comes back; a persistent Root's "
                + "Service lives for the run, because that Root is never torn down. What was handed "
                + "in with BindInstance, the two providers say, belongs to the run and stays.");
            painter.Paragraph(
                "The holder goes last, after the Context's own binder, so a Deconstruct that "
                + "dispatches still has a holder to dispatch through.");

            painter.Separator();
            painter.SubHeading("Setup initialises");
            painter.Paragraph(
                "Setup does not run until every Root in the scene has finished binding, so this is "
                + "where a module readies its Models if they need readying - and the only phase that "
                + "may reach across modules, which is what a Connector does there.");

            painter.Space();
            painter.SubHeading("Launch starts");
            painter.Paragraph(
                "Launch runs after every Setup and dispatches the module's first signal. The entry "
                + "point's Launch is what sets the game going.");
            painter.Code(
                "public override void Launch()\n"
                + "{\n"
                + "    base.Launch();\n"
                + "    _signals.Incoming.InitializePlayer.Dispatch();\n"
                + "}");

            painter.Space();
            painter.Note(
                "A module that has to put data in place before anything reads it takes Initialize "
                + "Order -100 and does the work in PostConstruct, which runs during the binding pass. "
                + "Setup would already be a frame too late, and a Command later still.");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A Context declares bindings and nothing else. If it needs an if, that decision belongs in a Command.");
            painter.Bullet("A Root is normally an empty class. What the module does lives in the Context it names.");
            painter.Bullet("A Root keeps the suffix of what it roots: CounterServiceRoot, PlayerSystemRoot, PlayerTestRoot.");
            painter.Bullet("A GameObject the module needs in the scene is parented to the module's Root.");
            painter.Bullet("A Root that must outlive its scene detaches itself in BeforeCreateContext before DontDestroyOnLoad.");
            painter.Bullet("Setup is the only phase that may reach across modules. The binding phases declare; Launch dispatches.");
            painter.Bullet("InjectionBinder is this Context's own. InjectionBinderCrossContext is the scene's, and every Context shares it.");
            painter.Bullet(
                "Bind across only what something outside the Context must see: the signal holder, and a Service interface. A Model is local.");
            painter.Bullet("A Context reaches into its sub-contexts' bindings. A sub-context does not reach back into its parent's.");
            painter.Bullet(
                "A Context takes back what it bound across. A module's holder and Service live exactly as long as its Root; what BindInstance handed in lives for the run.");
            painter.Bullet("Initialize Order runs from -100 to 100. Services take the negative band, the game's modules 0 to 97.");
        }

        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("scene", "Scene", "the module is present", 0, 0),
                new HelpGraphNode("root", "PlayerRoot", "Root<PlayerContext>", 0, 1),
                new HelpGraphNode("context", "PlayerContext", "declares the bindings", 0, 2),
                new HelpGraphNode("signals", "SignalBindings", "the module's signal holder", 1, 0),
                new HelpGraphNode("injection", "InjectionBindings", "models, services, systems", 1, 1),
                new HelpGraphNode("mediation", "MediationBindings", "view to mediator", 1, 2),
                new HelpGraphNode("commands", "CommandBindings", "signal to command", 2, 1),
                new HelpGraphNode("launch", "Launch", "the first dispatch", 2, 2)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("scene", "root", "holds"),
                new HelpGraphEdge("root", "context", "starts"),
                new HelpGraphEdge("context", "signals", string.Empty),
                new HelpGraphEdge("signals", "injection", string.Empty),
                new HelpGraphEdge("injection", "mediation", string.Empty),
                new HelpGraphEdge("mediation", "commands", string.Empty),
                new HelpGraphEdge("commands", "launch", string.Empty)
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("scene",
                    "A module joins the game by being in the scene. Nothing reaches in from outside to start it.",
                    "// Drop PlayerRoot onto a GameObject in the scene."),
                new HelpGraphStep("root",
                    "A Root is the module's presence in the scene, normally an empty class.",
                    "public class PlayerRoot : Root<PlayerContext> { }"),
                new HelpGraphStep("context",
                    "A Context declares bindings and nothing else.",
                    "public class PlayerContext : Context\n{\n    private PlayerSignals _signals;\n}"),
                new HelpGraphStep("signals",
                    "The signal holder is bound first, because the other bindings refer to it.",
                    "public override void SignalBindings()\n{\n    base.SignalBindings();\n    _signals = InjectionBinderCrossContext.Bind<PlayerSignals>();\n}"),
                new HelpGraphStep("injection",
                    "Models, services and systems are bound to their interfaces here - locally when the module keeps them, across when another module must see them.",
                    "public override void InjectionBindings()\n{\n    base.InjectionBindings();\n\n    InjectionBinder.Bind<IPlayerModel, PlayerModel>();\n    InjectionBinderCrossContext.Bind<IPlayerService, PlayerService>();\n}"),
                new HelpGraphStep("mediation",
                    "Each View is paired with the one Mediator that drives it.",
                    "public override void MediationBindings()\n{\n    base.MediationBindings();\n    MediationBinder.Bind<HudView>().To<HudMediator>();\n}"),
                new HelpGraphStep("commands",
                    "An incoming signal is bound to the command - or the sequence of commands - that answers it.",
                    "public override void CommandBindings()\n{\n    base.CommandBindings();\n\n    CommandBinder.Bind(_signals.Incoming.AddCurrency)\n        .ToSequence<AddCurrencyCommand>()\n        .ToSequence<SavePlayerCommand>();\n}"),
                new HelpGraphStep("launch",
                    "Launch runs once everything is bound. It dispatches the module's first signal.",
                    "public override void Launch()\n{\n    base.Launch();\n    _signals.Incoming.InitializePlayer.Dispatch();\n}")
            };

            return new HelpGraph(nodes, edges, steps, 1.4f);
        }
    }
}

#endif