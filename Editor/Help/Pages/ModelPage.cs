#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// The one place worth reading when you want to know what a module holds. A Model is a single
    /// thing rather than a folder of several, so the introduction opens with the sentence and the
    /// diagram instead of a row of cards, and the code sits on the tab beside it.
    /// </summary>
    internal class ModelPage : HelpPage
    {
        public ModelPage() : base(Build())
        {
        }

        public override string Title => "Model";

        public override FlowIcon Icon => FlowIcon.Database;

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Writing one", DrawWriting,
                "An interface and an implementation, like a Service.",
                "The rest of the module injects the interface, so what a Model offers is a list of "
                + "questions and a list of changes - never a field."),
            new HelpTab("Holding data", DrawHolding,
                "One record per thing, filled one source at a time.",
                "What the config says of a thing and what the save holds of it sit on one object, "
                + "so every read is one lookup and nothing has to be put back together."),
            new HelpTab("Rules", DrawRules,
                "The rules, in one list.",
                "What a Model owns, and the one thing it must never do.")
        };

        protected override string BodyHeadline => "A Model owns state, and the rules that keep it valid.";

        protected override string BodyTagline =>
            "It knows nothing about Views, Commands, or any other module - which is what makes it "
            + "the one place worth reading when you want to know what a module actually holds.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("Nothing reaches in");
            painter.Paragraph(
                "The crossed arrow is the whole point of the page. A signal never arrives at a "
                + "Model: it runs a Command, and the Command calls the Model.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.Note(
                "A Model may dispatch its own module's outgoing signals to announce that a value it "
                + "holds has changed. Announcing is allowed; listening is not.");
        }

        /// <summary>
        /// The pair every Model ships as, and the one habit that keeps it honest: state that only
        /// the Model itself may set.
        /// </summary>
        private void DrawWriting(HelpPainter painter)
        {
            painter.SubHeading("The pair");
            painter.Code(
                "public interface IPlayerModel\n"
                + "{\n"
                + "    double Currency { get; }\n"
                + "    void AddCurrency(double amount);\n"
                + "}",
                "IPlayerModel.cs - Scripts/Runtime/Models");
            painter.Code(
                "public class PlayerModel : IPlayerModel\n"
                + "{\n"
                + "    public double Currency { get; private set; }\n"
                + "\n"
                + "    public void AddCurrency(double amount) => Currency += amount;\n"
                + "}",
                "PlayerModel.cs - Scripts/Runtime/Models");
            painter.Paragraph(
                "The private set is not decoration. A settable property is an invitation for a "
                + "Command to write the field directly, and the moment that happens the rules that "
                + "keep the value legal live in two places.");

            painter.Separator();
            painter.SubHeading("Binding it");
            painter.Paragraph(
                "The Context binds the implementation to the interface once, and everything that "
                + "needs it injects the interface. It binds it to InjectionBinder rather than to "
                + "InjectionBinderCrossContext, because a Model is the module's own: the only way "
                + "into it is a Command of that same module, so nothing outside the Context has any "
                + "business seeing it.");
            painter.Code(
                "public override void InjectionBindings()\n"
                + "{\n"
                + "    base.InjectionBindings();\n"
                + "    InjectionBinder.Bind<IPlayerModel, PlayerModel>();\n"
                + "}",
                "PlayerContext.cs");
            painter.Code(
                "[Inject] private IPlayerModel _playerModel { get; set; }",
                "In the Command that changes it");
            painter.Paragraph(
                "A Model goes cross-context only when something outside the Context genuinely "
                + "reaches it, and that is rare enough to be worth a comment where it happens: "
                + "LocalSaveModule binds its model across because the Root writes the save on quit, "
                + "when a Command dispatched during shutdown might not finish in time.");

            painter.Separator();
            painter.SubHeading("Announcing a change");
            painter.Paragraph(
                "A Model that dispatches its module's outgoing signal saves every Command that "
                + "changes it from remembering to. What it must never do is listen: a Model that "
                + "subscribes has a second way in, and the Command stops being the only one.");
            painter.Code(
                "public class PlayerModel : IPlayerModel\n"
                + "{\n"
                + "    [InjectSignal] private PlayerSignals _signals { get; set; }\n"
                + "\n"
                + "    public double Currency { get; private set; }\n"
                + "\n"
                + "    public void AddCurrency(double amount)\n"
                + "    {\n"
                + "        Currency += amount;\n"
                + "        _signals.Outgoing.CurrencyChanged.Dispatch(Currency);\n"
                + "    }\n"
                + "}");

            painter.Space();
            painter.Note(
                "Create Model writes both files and the binding. Prefer it over writing them by "
                + "hand.");
        }

        /// <summary>
        /// How a Model keeps a thing whole: the record, the index that may sit beside it, the
        /// passes that fill it and what it hands out. The pieces it replaces are shown first,
        /// because that is the shape a Model drifts into when nobody says otherwise.
        /// </summary>
        private void DrawHolding(HelpPainter painter)
        {
            painter.SubHeading("One record per thing");
            painter.Paragraph(
                "What the config says of a power-up and what the save holds of it are two halves of "
                + "one thing, so they sit on one object, found under one key. The record is a plain VO "
                + "and its halves keep their own suffixes.");
            painter.Code(
                "internal class PowerUpVO\n"
                + "{\n"
                + "    public PowerUpCVO Config;   // the CD_PowerUps entry itself\n"
                + "    public PowerUpSVO Save;     // the SD_PowerUps entry itself\n"
                + "}\n"
                + "\n"
                + "private readonly Dictionary<PowerUpType, PowerUpVO> _powerUps = new();",
                "PowerUpVO.cs and PowerUpModel.cs");
            painter.Paragraph("What it replaces is a thing kept in pieces:");
            painter.Code(
                "private readonly Dictionary<PowerUpType, PowerUpSVO> _owned = new();\n"
                + "private readonly List<PowerUpType> _types = new();\n"
                + "private readonly Dictionary<PowerUpType, int> _unlockLevels = new();   // copied out of the config",
                "Three collections, one key");
            painter.Paragraph(
                "Every read puts the pieces back together, one field of the config was copied out and "
                + "the entry thrown away, and the next field means a fourth collection. Parallel lists "
                + "read at the same index are the same mistake, and worse: one insertion in one of them "
                + "pairs the wrong halves, and nothing reports it.");

            painter.Space();
            painter.Note(
                "A second collection is fine when it holds the same records - a List in the order the "
                + "config gives them beside a Dictionary that finds them by key. That is an index, not "
                + "a second half.");

            painter.Separator();
            painter.SubHeading("Filling it");
            painter.Paragraph(
                "PostConstruct reads the module's own assets off the Root adapter and fills the "
                + "records one source at a time, each in one pass. No loop runs inside another.");
            painter.Bullet("The config decides what exists: its pass creates the records, and a duplicate is reported and skipped.");
            painter.Bullet(
                "The save attaches to them by key. An entry the config does not list stays in the file untouched; a second entry for the same key is not read.");
            painter.Bullet(
                "What is still missing starts from the config, and the new save entry is added to the SD_ asset so it is written with the rest.");
            painter.Bullet("Values are clamped once, on the way in, so no read has to.");
            painter.Code(
                "public void PostConstruct()\n"
                + "{\n"
                + "    var adapter = _root.GetComponent<RootAdapter>();\n"
                + "    FillConfigs(adapter.GetScriptable<CD_PowerUps>());\n"
                + "    FillSaves(adapter.GetScriptable<SD_PowerUps>());\n"
                + "}",
                "PowerUpModel.cs");

            painter.Separator();
            painter.SubHeading("What it hands out");
            painter.Paragraph(
                "Reading is open. What it holds may be handed out as it is - the config entry, the save "
                + "entry, a shared asset's value object - to a Command that needs it, or on a signal to a "
                + "screen that shows four of its ten fields. Copying that into a new structure buys "
                + "nothing.");
            painter.Paragraph(
                "A field the Model keeps valid, or the save writes, changes through the Model: a count "
                + "that must not go negative changes through Spend and Grant, because a caller that sets "
                + "it itself skips the rule and the save writes the result. A flag a Command keeps for its "
                + "own flow - a watch already running, a start already announced - guards no rule and "
                + "reaches no save, so the Command sets it where it uses it. A Model method that is one "
                + "assignment protects nothing.");
            painter.Paragraph(
                "A new value object is written only when nothing existing carries what the reader "
                + "needs: a value derived by a rule, such as whether a power-up is still locked; several "
                + "records combined; or a Runtime type another module cannot reference, where a public "
                + "signal needs a type in Shared.");
            painter.Code(
                "public PowerUpStateVO GetState(PowerUpType type)",
                "Derived IsLocked, and PowerUpVO is internal - so a Shared value object earns its place");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A Model owns state and the rules that keep it valid.");
            painter.Bullet("A Model knows nothing about Views, Commands, or any other module.");
            painter.Bullet("A Model never subscribes to a signal. An incoming signal runs a Command, and the Command calls the Model.");
            painter.Bullet("A Model may dispatch its own module's outgoing signals. Announcing is allowed; listening is not.");
            painter.Bullet("A Model is an interface and an implementation: IPlayerModel and PlayerModel.");
            painter.Bullet(
                "State the Model keeps valid, or the save writes, is readable and privately settable. A flag a Command keeps for its own flow is the Command's to set.");
            painter.Bullet(
                "A Model holds one record per thing, under one key - never parallel lists or dictionaries that every read puts back together.");
            painter.Bullet("A Model fills its records in PostConstruct: the config decides what exists, the save attaches to it.");
            painter.Bullet("A Model is bound with InjectionBinder, not InjectionBinderCrossContext. It belongs to its own Context.");
        }

        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("signal", "Incoming Signal", "never arrives here directly", 0, 0),
                new HelpGraphNode("command", "Command", "the only way in", 0, 1),
                new HelpGraphNode("model", "PlayerModel", "state and its rules", 0, 2),
                new HelpGraphNode("outgoing", "Outgoing Signal", "the value changed", 1, 2)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("signal", "command", "runs"),
                new HelpGraphEdge("command", "model", "calls"),
                new HelpGraphEdge("model", "outgoing", "announces"),
                new HelpGraphEdge("signal", "model", "never", HelpGraphEdgeKind.Forbidden)
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("command",
                    "A Model never subscribes to a signal. An incoming signal runs a Command, and the Command calls the Model.",
                    "public override void Execute() => _playerModel.AddCurrency(_amount);"),
                new HelpGraphStep("model",
                    "The Model keeps its own state valid. Nothing reaches in and sets a field from outside.",
                    "public class PlayerModel : IPlayerModel\n{\n    public double Currency { get; private set; }\n\n    public void AddCurrency(double amount) => Currency += amount;\n}"),
                new HelpGraphStep("outgoing",
                    "A Model may dispatch its own module's outgoing signals. Announcing is allowed; listening is not.",
                    "_signals.Outgoing.CurrencyChanged.Dispatch(Currency);")
            };

            return new HelpGraph(nodes, edges, steps, 1.4f);
        }
    }
}

#endif