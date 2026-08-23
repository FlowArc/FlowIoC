#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    internal class ControllersPage : HelpPage
    {
        public ControllersPage() : base(Build())
        {
        }

        public override string Title => "Controllers";

        public override string Icon => "cs Script Icon";

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Rules", DrawRules)
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Controllers/ is where a module's logic lives: its Commands, and the Functions they "
                + "call when they need a value back.");
            painter.Paragraph(
                "A Command is one unit of work. It is triggered by a signal, it injects the models "
                + "and services it needs, it mutates state and it dispatches whatever the module "
                + "wants to announce. It holds no state between runs and returns no value.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.Note(
                "Injection targets properties, never fields. A plain field is silently skipped - no "
                + "error, no warning, just null at runtime.");

            painter.SubHeading("Functions");
            painter.Paragraph(
                "A Function returns a value and does not orchestrate. It is called directly through "
                + "the function provider rather than dispatched. If you want the step to show up in "
                + "the Flow Console, write a Command instead - a Function is deliberately invisible.");
            painter.Code(
                "public class CalculateDamageFunction : FunctionReturn<double, string>\n"
                + "{\n"
                + "    [Inject] private IWeaponsModel _weaponsModel { get; set; }\n"
                + "\n"
                + "    public override double Execute(string weaponId) =>\n"
                + "        _weaponsModel.GetConfigVO(weaponId).baseDamage;\n"
                + "}");
            painter.Code(
                "var damage = _functionProvider\n"
                + "    .Execute<CalculateDamageFunction>()\n"
                + "    .AddParams(weaponId)\n"
                + "    .SetReturn<double>();");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A Command does one unit of work, holds no state between runs, and returns no value.");
            painter.Bullet("A Command never touches another module's model. What it needs from elsewhere arrives as a signal.");
            painter.Bullet("A Function returns a value and does not orchestrate. Want the step in the Flow Console? Write a Command.");
            painter.Bullet("Injection targets properties. A plain field is skipped silently - no error, no warning, null at runtime.");

            painter.SubHeading("Taking the signal's parameters");
            painter.Paragraph(
                "A command is bound in the Context and run by the signal it is bound to. Each "
                + "[SignalParam] property is filled from that signal's payload.");
            painter.Code(
                "// PlayerSignalsIncoming\n"
                + "public Signal<CurrencyType, int> DecreaseCurrency = new();");
            painter.Code(
                "// PlayerContext.CommandBindings\n"
                + "CommandBinder.Bind(_signals.Incoming.DecreaseCurrency)\n"
                + "    .ToSequence<DecreaseCurrencyCommand>()\n"
                + "    .ToSequence<SavePlayerCommand>();");
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
                + "}");

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

            painter.SubHeading("Sequence, parallel, and waiting");
            painter.Paragraph(
                "ToSequence steps wait for the step before them; ToParallel steps all start at "
                + "once. The two can be mixed in one binding.");
            painter.Paragraph("A command that finishes asynchronously has to hold the sequence open:");
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

            return new HelpGraph(nodes, edges, steps);
        }
    }
}

#endif
