#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    internal class ViewMediatorPage : HelpPage
    {
        public ViewMediatorPage() : base(Build())
        {
        }

        public override string Title => "View & Mediator";

        public override string Icon => "Canvas Icon";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A View is the MonoBehaviour in the scene: it holds references and raises callbacks. "
                + "A View with an if about game rules is doing the Mediator's job.");
            painter.Paragraph(
                "A Mediator is a plain injected class that drives exactly one View. It listens to "
                + "signals and dispatches them, and holds no game rules either - those live in "
                + "Commands and Models.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.SubHeading("Binding the pair");
            painter.Paragraph("Create View writes both files, and the Context binds them together:");
            painter.Code(
                "public override void MediationBindings()\n"
                + "{\n"
                + "    base.MediationBindings();\n"
                + "    MediationBinder.Bind<HudView>().To<HudMediator>();\n"
                + "}");
            painter.Note(
                "The ViewInjector component on the GameObject resolves which Context each IView "
                + "belongs to. Registration happens as soon as that Context starts, and OnRemove runs "
                + "when the object is destroyed.");

            painter.Space();
            painter.SubHeading("Screen views are pooled");
            painter.Paragraph(
                "Start is fine for a View that lives and dies with its GameObject. A ScreenView does "
                + "not: hiding it deactivates the object and opening it again shows that same "
                + "instance, so Awake and Start run once while the screen opens many times. Wire its "
                + "buttons in OnEnable and drop them in OnDisable.");
            painter.Code(
                "private void OnEnable()\n"
                + "{\n"
                + "    _buyButton.onClick.AddListener(() => Buy?.Invoke());\n"
                + "}\n"
                + "\n"
                + "private void OnDisable()\n"
                + "{\n"
                + "    _buyButton.onClick.RemoveAllListeners();\n"
                + "}");
        }

        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("view", "HudView", "buttons, labels, raw input", 0, 0),
                new HelpGraphNode("mediator", "HudMediator", "drives exactly one View", 0, 1),
                new HelpGraphNode("incoming", "Incoming Signal", "AddCurrency", 0, 2),
                new HelpGraphNode("outgoing", "Outgoing Signal", "CurrencyChanged", 1, 1),
                new HelpGraphNode("command", "Command", "one unit of work", 1, 2)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("view", "mediator", "raw input"),
                new HelpGraphEdge("mediator", "incoming", "dispatches"),
                new HelpGraphEdge("incoming", "command", "runs"),
                new HelpGraphEdge("command", "outgoing", "announces"),
                new HelpGraphEdge("outgoing", "mediator", "listens")
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("view",
                    "The View reports what happened on screen and nothing more. It does not know what the button means.",
                    "[RequireComponent(typeof(ViewInjector))]\npublic class HudView : MonoBehaviour, IView\n{\n    public bool IsRegistered { get; set; }\n\n    public Action Buy { get; set; }\n    public Button BuyButton;\n    public Text CurrencyLabel;\n\n    private void Start() => BuyButton.onClick.AddListener(() => Buy?.Invoke());\n}"),
                new HelpGraphStep("mediator",
                    "The Mediator is a plain injected class that drives exactly one View. It decides nothing about the game.",
                    "public class HudMediator : IMediator\n{\n    [Inject]       private HudView       _view    { get; set; }\n    [InjectSignal] private PlayerSignals _signals { get; set; }\n\n    public void OnRegister() => _view.Buy += Buy;\n    public void OnRemove()   => _view.Buy -= Buy;\n}"),
                new HelpGraphStep("incoming",
                    "What leaves the Mediator is an ordinary incoming signal - the same one any other caller would dispatch.",
                    "private void Buy() => _signals.Incoming.AddCurrency.Dispatch(-10d);"),
                new HelpGraphStep("command",
                    "The rule lives in the Command. This is the only place the purchase is allowed or refused.",
                    "public override void Execute() => _playerModel.AddCurrency(_amount);"),
                new HelpGraphStep("outgoing",
                    "The module announces the new value without knowing that a HUD exists.",
                    "_signals.Outgoing.CurrencyChanged.Dispatch(_playerModel.Currency);"),
                new HelpGraphStep("mediator",
                    "The Mediator hears the announcement and writes it into the View. The loop closes without either end knowing the other.",
                    "public void OnRegister()\n{\n    _view.Buy += Buy;\n    _signals.Outgoing.CurrencyChanged.AddListener(OnCurrencyChanged);\n}\n\nprivate void OnCurrencyChanged(double currency) =>\n    _view.CurrencyLabel.text = currency.ToString();")
            };

            return new HelpGraph(nodes, edges, steps);
        }
    }
}

#endif