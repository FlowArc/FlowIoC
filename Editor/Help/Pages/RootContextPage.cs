#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    internal class RootContextPage : HelpPage
    {
        public RootContextPage() : base(Build())
        {
        }

        public override string Title => "Root & Context";

        public override string Icon => "UnityEditor.SceneHierarchyWindow";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A Root is the module's presence in the scene, and it is normally an empty class. "
                + "Dropping it into a scene starts the module; nothing else has to be wired by hand.");
            painter.Paragraph(
                "The Context is where the module declares what it is made of - and nothing else. A "
                + "Context that needs an if is making a decision, and a decision belongs in a Command.");

            painter.SubHeading("What each phase is for");
            painter.Paragraph(
                "The binding phases declare. SignalBindings, InjectionBindings, MediationBindings and "
                + "CommandBindings say what the module is made of, and decide nothing.");
            painter.Paragraph(
                "Setup initialises. It does not run until every Root in the scene has finished binding, "
                + "so this is where a module readies its Models if they need readying - and the only "
                + "phase that may reach across modules, which is what a Connector does there.");
            painter.Paragraph(
                "Launch starts. It runs after every Setup and dispatches the module's first signal; the "
                + "entry point's Launch is what sets the game going.");

            painter.SubHeading("What hangs off a Root");
            painter.Paragraph(
                "A GameObject the module needs in the scene goes under its Root. The Root is the "
                + "module's one presence there, so an EventSystem, an adapter, anything the module "
                + "owns hangs off it rather than sitting loose beside it.");
            painter.Paragraph(
                "A Root otherwise lives and dies with its scene. A module whose work outlives one - "
                + "input, audio, analytics - makes its Root persistent in BeforeCreateContext, which "
                + "runs just before the context is built. The reparenting is not decoration: Unity "
                + "marks only root level objects as do not destroy.");
            painter.Code(
                "protected override void BeforeCreateContext()\n"
                + "{\n"
                + "    transform.SetParent(null);\n"
                + "    DontDestroyOnLoad(gameObject);\n"
                + "}");

            painter.Space();
            painter.Graph(Graph, Stepper);
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
                    "Models, services and systems are bound to their interfaces here.",
                    "public override void InjectionBindings()\n{\n    base.InjectionBindings();\n    InjectionBinderCrossContext.Bind<IPlayerModel, PlayerModel>();\n}"),
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

            return new HelpGraph(nodes, edges, steps);
        }
    }
}

#endif