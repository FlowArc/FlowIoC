#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// Controllers, read in four passes: what the folder is made of, then the Command in full,
    /// then the Function beside it, then the rules on their own. The introduction carries no code
    /// at all - a reader meets the two parts as a picture and walks the flow once, and the code
    /// waits on the tab for whichever of the two they came for.
    /// </summary>
    internal class ControllersPage : HelpPage
    {
        public ControllersPage() : base(Build())
        {
        }

        public override string Title => "Controllers";

        public override string Icon => "cs Script Icon";

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Command", DrawCommand),
            new HelpTab("Function", DrawFunction),
            new HelpTab("Rules", DrawRules)
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Hero(
                "Controllers is where a module thinks.",
                "Two kinds of code live in the folder, and they are told apart by one question: "
                + "does it answer you, or does it announce?");

            painter.Parts(
                new HelpPart("Command",
                    "One unit of work. A signal runs it. It changes state and announces what "
                    + "happened, and gives you nothing back.",
                    "class AddCurrencyCommand : Command"),
                new HelpPart("Function",
                    "One value back. You call it where you stand and it answers. It orchestrates "
                    + "nothing and announces nothing.",
                    "class CalculateDamageFunction : FunctionReturn<double, string>"));

            painter.SubHeading("How a signal becomes a state change");
            painter.Paragraph(
                "Four boxes, and the whole of what a Command is for. Walk them with the buttons "
                + "under the diagram; the code behind each step is on the Command tab.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.Note(
                "Injection targets properties, never fields. A plain field is silently skipped - no "
                + "error, no warning, just null at runtime.");
        }

        /// <summary>
        /// The Command in full: where it is bound, how a binding is arranged, how the signal's
        /// payload reaches it, and what holds a sequence open while it waits.
        /// </summary>
        private void DrawCommand(HelpPainter painter)
        {
            painter.Hero(
                "A Command is bound in the Context and run by a signal.",
                "It never constructs itself and nothing calls it by name. The binding is the whole "
                + "of what decides when it runs.");

            painter.SubHeading("Where a Command is bound");
            painter.Paragraph(
                "One signal, and the commands that answer it, in the order they are written. "
                + "ToSequence steps wait for the step before them; ToParallel steps start at once "
                + "and hold nothing up. The two can be mixed in one binding.");
            painter.Code(
                "public override void CommandBindings()\n"
                + "{\n"
                + "    base.CommandBindings();\n"
                + "\n"
                + "    CommandBinder.Bind(_signals.Incoming.DecreaseCurrency)\n"
                + "        .ToSequence<DecreaseCurrencyCommand>()\n"
                + "        .ToSequence<SavePlayerCommand>()\n"
                + "        .ToParallel<PlayPurchaseFxCommand>();\n"
                + "}",
                "PlayerContext.cs");

            painter.Space();
            painter.Graph(Sequence());

            painter.Space();
            painter.SubHeading("Taking the signal's parameters");
            painter.Paragraph(
                "The signal declares what it carries. Each [SignalParam] property in the command is "
                + "filled from that payload, and the property is what is filled - a field of the "
                + "same name is skipped without a word.");
            painter.Code(
                "public class PlayerSignalsIncoming\n"
                + "{\n"
                + "    public Signal<CurrencyType, int> DecreaseCurrency = new();\n"
                + "}",
                "PlayerSignals.cs - Scripts/Signals");
            painter.Code(
                "public class DecreaseCurrencyCommand : Command\n"
                + "{\n"
                + "    [Inject]       private IPlayerModel  _playerModel { get; set; }\n"
                + "    [InjectSignal] private PlayerSignals _signals     { get; set; }\n"
                + "\n"
                + "    [SignalParam] private CurrencyType _type   { get; set; }\n"
                + "    [SignalParam] private int          _amount { get; set; }\n"
                + "\n"
                + "    public override void Execute()\n"
                + "    {\n"
                + "        _playerModel.Decrease(_type, _amount);\n"
                + "        _signals.Outgoing.CurrencyChanged.Dispatch(_playerModel.Currency);\n"
                + "    }\n"
                + "}",
                "DecreaseCurrencyCommand.cs - Scripts/Runtime/Controllers");

            painter.Space();
            painter.SubHeading("Two values of one type");
            painter.Paragraph(
                "When a signal carries more than one value of the same type, write the index of the "
                + "one you want. The index counts within that property's type, so inserting a "
                + "parameter of some other type into the signal does not shift it.");
            painter.Code(
                "public Signal<string, int, int> Damage = new();   // Dispatch(\"sword\", 12, 3)\n"
                + "\n"
                + "[SignalParam]    private string _weapon { get; set; }   // \"sword\"\n"
                + "[SignalParam(0)] private int    _amount { get; set; }   // 12\n"
                + "[SignalParam(1)] private int    _crit   { get; set; }   // 3");
            painter.Paragraph(
                "A property with no index takes the first value of its type that no other property "
                + "has claimed, so two same-typed properties resolve correctly on their own too.");

            painter.Space();
            painter.SubHeading("Holding the sequence open");
            painter.Paragraph(
                "A command that finishes asynchronously has to say so, or the step after it starts "
                + "while it is still working.");
            painter.Code(
                "public override void Execute()\n"
                + "{\n"
                + "    Retain();\n"
                + "    _coroutineProvider.StartCoroutine(DelayedComplete());\n"
                + "}\n"
                + "\n"
                + "private IEnumerator DelayedComplete()\n"
                + "{\n"
                + "    yield return new WaitForSeconds(3f);\n"
                + "    Release();\n"
                + "}");
            painter.Paragraph(
                "Release(params object[]) may pass data forward: the next command in the sequence "
                + "receives it through its typed Execute overload. Stop() abandons the rest of the "
                + "sequence.");
            painter.Code(
                "public class SavePlayerCommand : Command<IPlayerModel>\n"
                + "{\n"
                + "    public override void Execute(IPlayerModel playerModel) => playerModel.Save();\n"
                + "}");
        }

        /// <summary>
        /// The Function beside it. Short on purpose: a Function is the simpler of the two, and
        /// most of what is worth saying about it is what it deliberately does not do.
        /// </summary>
        private void DrawFunction(HelpPainter painter)
        {
            painter.Hero(
                "A Function answers a question and goes away.",
                "It returns a value, it orchestrates nothing, and it is called where the answer is "
                + "wanted rather than dispatched at.");

            painter.SubHeading("Writing one");
            painter.Paragraph(
                "FunctionReturn takes the return type first and the parameters after it, up to "
                + "four. A function that answers nothing is a FunctionVoid instead. It injects "
                + "whatever it needs, the way a Command does.");
            painter.Code(
                "public class CalculateDamageFunction : FunctionReturn<double, string>\n"
                + "{\n"
                + "    [Inject] private IWeaponsModel _weaponsModel { get; set; }\n"
                + "\n"
                + "    public override double Execute(string weaponId) =>\n"
                + "        _weaponsModel.GetConfigVO(weaponId).baseDamage;\n"
                + "}",
                "CalculateDamageFunction.cs - Scripts/Runtime/Functions");

            painter.Space();
            painter.SubHeading("Calling one");
            painter.Paragraph(
                "The function provider is injected wherever the answer is needed - a Command, "
                + "another Function - and hands the instance back to the pool once it has answered.");
            painter.Code(
                "[Inject] private IFunctionProvider _functionProvider { get; set; }\n"
                + "\n"
                + "var damage = _functionProvider\n"
                + "    .Execute<CalculateDamageFunction>()\n"
                + "    .AddParams(weaponId)\n"
                + "    .SetReturn<double>();");

            painter.Space();
            painter.Note(
                "A Function is deliberately invisible in the Flow Console. If you want the step "
                + "logged with the rest of the flow, the work belongs in a Command instead.");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A decision belongs in a Command, wherever it would otherwise be taken - a Context, a View, a Mediator, a System.");
            painter.Bullet("What needs no decision is not one. A close button that always closes is the Mediator calling _view.Hide().");
            painter.Bullet("A Command does one unit of work, holds no state between runs, and returns no value.");
            painter.Bullet("A Command never touches another module's model. What it needs from elsewhere arrives as a signal.");
            painter.Bullet("A Function returns a value and does not orchestrate. Want the step in the Flow Console? Write a Command.");
            painter.Bullet("Injection targets properties. A plain field is skipped silently - no error, no warning, null at runtime.");
            painter.Bullet("A Command lives in Scripts/Runtime/Controllers, a Function in Scripts/Runtime/Functions.");
            painter.Bullet("Create Command writes the file and its binding. Prefer it over writing either by hand.");
        }

        /// <summary>
        /// One binding, drawn as a map: what waits for what, and what does not wait at all. It has
        /// no steps, so the painter draws the boxes without the Previous and Next controls.
        /// </summary>
        private static HelpGraph Sequence()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("signal", "Incoming Signal", "DecreaseCurrency", 0, 0),
                new HelpGraphNode("first", "DecreaseCurrencyCommand", "ToSequence - step one", 0, 1),
                new HelpGraphNode("second", "SavePlayerCommand", "ToSequence - waits for it", 0, 2),
                new HelpGraphNode("parallel", "PlayPurchaseFxCommand", "ToParallel - waits for nothing", 1, 1)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("signal", "first", "runs"),
                new HelpGraphEdge("first", "second", "then"),
                new HelpGraphEdge("signal", "parallel", "and at once")
            };

            return new HelpGraph(nodes, edges, new List<HelpGraphStep>());
        }

        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("signal", "Incoming Signal", "AddCurrency", 0, 0),
                new HelpGraphNode("command", "AddCurrencyCommand", "one unit of work", 0, 1),
                new HelpGraphNode("model", "PlayerModel", "state changes here", 0, 2),
                new HelpGraphNode("outgoing", "Outgoing Signal", "CurrencyChanged", 1, 1)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("signal", "command", "runs"),
                new HelpGraphEdge("command", "model", "mutates"),
                new HelpGraphEdge("command", "outgoing", "dispatches")
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("signal",
                    "The Context binds an incoming signal to the command that answers it.",
                    "CommandBinder.Bind(_signals.Incoming.AddCurrency)\n    .ToSequence<AddCurrencyCommand>();"),
                new HelpGraphStep("command",
                    "A Command injects what it needs. Injection targets properties - a plain field is silently skipped.",
                    "public class AddCurrencyCommand : Command\n{\n    [Inject]       private IPlayerModel  _playerModel { get; set; }\n    [InjectSignal] private PlayerSignals _signals     { get; set; }\n\n    [SignalParam]  private double _amount { get; set; }\n}"),
                new HelpGraphStep("model",
                    "The Command calls the Model. The Model decides whether the change is legal; the Command does not.",
                    "_playerModel.AddCurrency(_amount);"),
                new HelpGraphStep("outgoing",
                    "Having changed something, the Command announces it. Who listens is not its business.",
                    "_signals.Outgoing.CurrencyChanged.Dispatch(_playerModel.Currency);")
            };

            return new HelpGraph(nodes, edges, steps, 1.4f);
        }
    }
}

#endif