#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// The one place two modules meet, read in four passes: the crossing as a picture, then how
    /// the wiring is written, then the three cases that need no Connector at all, then the rules.
    /// </summary>
    internal class ConnectorsPage : HelpPage
    {
        public ConnectorsPage() : base(Build())
        {
        }

        public override string Title => "Connectors";

        public override string Icon => "Linked";

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Wiring", DrawWiring),
            new HelpTab("Exceptions", DrawExceptions),
            new HelpTab("Rules", DrawRules)
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Hero(
                "A module never reaches into another module.",
                "No type from Modules.A appears in Modules.B. The only crossing point is a "
                + "Connector, and neither module learns that the other exists.");

            painter.Parts(
                new HelpPart("Setup",
                    "Where a Connector does its work. Every Root has finished binding by then, so "
                    + "both signal holders already exist and are asked for, never bound.",
                    "InjectionBinderCrossContext.GetInstance<HeroSignals>()"),
                new HelpPart("Connect",
                    "What joins them: one module's Outgoing to another's Incoming. It also takes a "
                    + "plain delegate, and a converter when the payloads differ.",
                    "outgoing.Connect(incoming)"));

            painter.SubHeading("One announcement, one delivery");
            painter.Paragraph(
                "Three boxes, and the whole of a crossing. Walk them with the buttons under the "
                + "diagram; the code is on the Wiring tab.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.Note(
                "A Connector gets signal holders, it never binds them. Bind would hand it a holder "
                + "of its own the moment the owning module is missing from the scene: nothing would "
                + "fail, and nothing would ever arrive either.");
        }

        /// <summary>
        /// How the wiring is written: where the sub-context is listed, the two shapes Connect
        /// takes, and what a failed GetInstance is actually telling you.
        /// </summary>
        private void DrawWiring(HelpPainter painter)
        {
            painter.Hero(
                "A Connector is a Context that binds nothing.",
                "It declares no models, no commands and no signals. It asks for two holders that "
                + "already exist and joins them.");

            painter.SubHeading("Writing one");
            painter.Code(
                "public class HeroConnectorSubContext : Context\n"
                + "{\n"
                + "    private HeroSignals          _heroSignals;\n"
                + "    private PlayerProfileSignals _playerProfileSignals;\n"
                + "\n"
                + "    public override void Setup()\n"
                + "    {\n"
                + "        _heroSignals          = InjectionBinderCrossContext.GetInstance<HeroSignals>();\n"
                + "        _playerProfileSignals = InjectionBinderCrossContext.GetInstance<PlayerProfileSignals>();\n"
                + "\n"
                + "        _heroSignals.Outgoing.DecreaseCurrency\n"
                + "            .Connect(_playerProfileSignals.Incoming.DecreaseCurrency);\n"
                + "    }\n"
                + "}",
                "HeroConnectorSubContext.cs");
            painter.Paragraph(
                "It reaches each module through that module's Signals assembly - Modules.Hero.Signals "
                + "and never Modules.Hero - which is what keeps one module's assembly out of "
                + "another's. Signals sit in an assembly of their own so that a System reading a "
                + "neighbour's Shared data does not get that neighbour's signals with it.");
            painter.Note(
                "A Connector still references the Shared assemblies of whatever its signals carry. "
                + "Connect<T> has to infer T, so connecting two Signal<DifficultyType> needs "
                + "Modules.Gameplay.Shared even though the Connector never touches the value - "
                + "without it the compiler reports CS0012.");

            painter.Space();
            painter.SubHeading("Where it is listed");
            painter.Paragraph(
                "On the Connector Root, and nowhere else. Add Sub Context offers connector "
                + "sub-contexts on that Root alone, and offers every other Root everything but "
                + "those, so the wiring between two modules cannot be scattered across the scene by "
                + "accident. A context counts as a Connector's when its name says so - "
                + "HeroConnectorSubContext - or when it carries FlowHeader(FlowRole.Connector).");

            painter.Space();
            painter.SubHeading("Adapting between two payloads");
            painter.Paragraph(
                "Connect also takes a plain delegate, and can adapt between signals whose parameter "
                + "types differ by taking a converter as its second argument.");
            painter.Code(
                "_heroSignals.Outgoing.HeroDied\n"
                + "    .Connect(_analyticsSignals.Incoming.TrackEvent, hero => hero.Id);\n"
                + "\n"
                + "_heroSignals.Outgoing.HeroDied\n"
                + "    .Connect(OnHeroDied);");

            painter.Space();
            painter.SubHeading("A Connector translates, it does not decide");
            painter.Paragraph(
                "An Outgoing signal announces what happened; an Incoming signal orders something "
                + "done. Joining one to the other is a crossing, and that is the whole job.");
            painter.Paragraph(
                "A list of consequences hung off one announcement is not a crossing, it is a flow. "
                + "Bind GameOver to OpenGameEndScreen and, beside it, to ResetPlayerData, and both "
                + "exist only in the wiring - it reads as though the Connector decided that ending "
                + "a game resets the player's data. Nobody decided it; the two lines just happen to "
                + "sit together.");
            painter.Paragraph(
                "GameOver is an announcement, but what follows it - reset the data, close the "
                + "panel, disable input - is a decision, and it belongs where the deciding happens: "
                + "dispatched from the Command that made it, or as sequence steps under that "
                + "Command in its own module's Context.");
            painter.Note(
                "About to write a second Connect from the same signal? Ask whether those two things "
                + "are one consequence of one decision. If they are, they belong in that module's "
                + "sequence, and the Connector carries one line to it.");

            painter.Space();
            painter.Note(
                "Failing to get a holder is the report that the module's Root is not in the scene. "
                + "Put the Root back rather than binding around it.");
        }

        /// <summary>
        /// The three cases a Connector is not needed for. They are the whole list: anything else
        /// that crosses is a mistake.
        /// </summary>
        private void DrawExceptions(HelpPainter painter)
        {
            painter.Hero(
                "Three things cross without a Connector.",
                "This is the whole list. Anything else that names a type from another module is a "
                + "mistake, whatever the compiler says about it.");

            painter.SubHeading("A Service crosses directly");
            painter.Paragraph(
                "Reference the Service module's assembly and inject its interface. Being usable "
                + "this way is the point of a Service: it is self-contained, it depends on nothing "
                + "outside itself, and it answers the input it is given.");
            painter.Code(
                "[Inject] private ICounterService _counterService { get; set; }",
                "In a Command of any module that references Modules.Counter");

            painter.Space();
            painter.SubHeading("A sub-module reaches the module it lives in");
            painter.Paragraph(
                "A screen or sub module may use its parent's types. The direction is one way: a "
                + "module never knows what sits in its own zScreenModules or zSubModules.");

            painter.Space();
            painter.SubHeading("A test module reaches anything");
            painter.Paragraph(
                "Everything under zTestModules is test code, so it may reference any module in the "
                + "project. In exchange, every script in it is wrapped in #if UNITY_EDITOR.");

            painter.Space();
            painter.Note(
                "If two modules need the same data and neither owns it, that data belongs in a "
                + "module of its own - the way a shared Service does.");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A module never reaches into another module. The only crossing point is a Connector.");
            painter.Bullet("A Connector translates, it does not decide. One announcement joined to one order is a crossing.");
            painter.Bullet("A list of consequences hung off one announcement is a flow, and a flow belongs in the Context that owns it.");
            painter.Bullet("A Connector gets signal holders with GetInstance in Setup. It never binds them.");
            painter.Bullet(
                "A Connector disconnects in DestroyContext what it connected in Setup, so a scene that comes back does not wire the same crossing twice onto a holder that outlived it.");
            painter.Bullet("A Connector sub-context is listed on the Connector Root and nowhere else.");
            painter.Bullet("A Connector reaches a module through Modules.Hero.Signals, never through Modules.Hero or Modules.Hero.Shared.");
            painter.Bullet("Systems are never added to one another's assemblies. Two Systems talk through signals wired in a Connector.");
            painter.Bullet("The three exceptions are a Service, a sub-module reaching its parent, and a test module.");
        }

        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("hero", "HeroModule", "Outgoing.DecreaseCurrency", 0, 0),
                new HelpGraphNode("connector", "HeroConnectorSubContext", "the only crossing point", 0, 1),
                new HelpGraphNode("profile", "PlayerProfileModule", "Incoming.DecreaseCurrency", 0, 2)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("hero", "connector", "announces"),
                new HelpGraphEdge("connector", "profile", "delivers")
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("hero",
                    "The Hero module announces what happened to it. It does not know who cares.",
                    "public class HeroSignalsOutgoing\n{\n    public Signal<double> DecreaseCurrency = new();\n}"),
                new HelpGraphStep("connector",
                    "The Connector gets both signal holders - it never binds them - and joins one module's Outgoing to another's Incoming.",
                    "public class HeroConnectorSubContext : Context\n{\n    public override void Setup()\n    {\n        _heroSignals          = InjectionBinderCrossContext.GetInstance<HeroSignals>();\n        _playerProfileSignals = InjectionBinderCrossContext.GetInstance<PlayerProfileSignals>();\n\n        _heroSignals.Outgoing.DecreaseCurrency\n            .Connect(_playerProfileSignals.Incoming.DecreaseCurrency);\n    }\n}"),
                new HelpGraphStep("profile",
                    "The receiving module answers an ordinary incoming signal. It has no idea a Hero module exists.",
                    "CommandBinder.Bind(_signals.Incoming.DecreaseCurrency)\n    .ToSequence<DecreaseCurrencyCommand>();")
            };

            return new HelpGraph(nodes, edges, steps, 1.4f);
        }
    }
}

#endif