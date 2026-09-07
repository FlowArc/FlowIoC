#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

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

        public override string Icon => "ScriptableObject Icon";

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Writing one", DrawWriting,
                "An interface and an implementation, like a Service.",
                "The rest of the module injects the interface, so what a Model offers is a list of "
                + "questions and a list of changes - never a field."),
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

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A Model owns state and the rules that keep it valid.");
            painter.Bullet("A Model knows nothing about Views, Commands, or any other module.");
            painter.Bullet("A Model never subscribes to a signal. An incoming signal runs a Command, and the Command calls the Model.");
            painter.Bullet("A Model may dispatch its own module's outgoing signals. Announcing is allowed; listening is not.");
            painter.Bullet("A Model is an interface and an implementation: IPlayerModel and PlayerModel.");
            painter.Bullet("State is readable and privately settable. Nothing outside the Model writes a field on it.");
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