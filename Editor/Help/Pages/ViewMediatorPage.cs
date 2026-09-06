#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// The pair that puts a module on screen, read in four passes: the two of them as a picture
    /// and the round trip a click makes, then the View as a scene object, then the Mediator as an
    /// injected class, then the rules.
    /// </summary>
    internal class ViewMediatorPage : HelpPage
    {
        public ViewMediatorPage() : base(Build())
        {
        }

        public override string Title => "View & Mediator";

        public override string Icon => "Canvas Icon";

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("View", DrawView),
            new HelpTab("Mediator", DrawMediator),
            new HelpTab("Rules", DrawRules)
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Hero(
                "Neither half knows a game rule.",
                "The View reports what happened on screen, the Mediator turns it into a signal, and "
                + "the decision is taken somewhere neither of them can see.");

            painter.Parts(
                new HelpPart("View",
                    "The MonoBehaviour in the scene. It holds references and raises callbacks. A "
                    + "View with an if about game rules is doing the Mediator's job.",
                    "class HudView : MonoBehaviour, IView"),
                new HelpPart("Mediator",
                    "A plain injected class that drives exactly one View. It listens to signals and "
                    + "dispatches them, and holds no game rules either.",
                    "class HudMediator : IMediator"));

            painter.SubHeading("The round trip a click makes");
            painter.Paragraph(
                "Six steps, and the loop closes without either end knowing the other. Walk them "
                + "with the buttons under the diagram.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.Note(
                "The rule lives in the Command. That is the only place the purchase is allowed or "
                + "refused - not in the View, and not in the Mediator.");
        }

        /// <summary>
        /// The View as a scene object: what it may hold, how it finds the Context it belongs to,
        /// and the one case - a pooled screen - where Awake and Start are the wrong place.
        /// </summary>
        private void DrawView(HelpPainter painter)
        {
            painter.Hero(
                "A View holds scene references and raw input.",
                "It does not know what the button means. It says that the button was pressed and "
                + "leaves the meaning to whoever is listening.");

            painter.SubHeading("Writing one");
            painter.Code(
                "[RequireComponent(typeof(ViewInjector))]\n"
                + "public class HudView : MonoBehaviour, IView\n"
                + "{\n"
                + "    public bool IsRegistered { get; set; }\n"
                + "\n"
                + "    public Action Buy { get; set; }\n"
                + "\n"
                + "    public Button BuyButton;\n"
                + "    public Text   CurrencyLabel;\n"
                + "\n"
                + "    private void Start() => BuyButton.onClick.AddListener(() => Buy?.Invoke());\n"
                + "}",
                "HudView.cs - Scripts/Runtime/ViewsMediators");

            painter.Space();
            painter.SubHeading("Which Context a View belongs to");
            painter.Paragraph(
                "The ViewInjector component lists one entry per IView on the object, and each entry "
                + "says where its Context comes from. A View authored under its module's Root wants "
                + "Bubble Up, which is the default.");
            painter.Bullet("Bubble Up - the first Root above the View in the hierarchy.");
            painter.Bullet("Selected Root - the Root named on the entry, wherever it sits in the scene.");
            painter.Bullet("Root Name - the Root whose GameObject carries that name, resolved at startup.");
            painter.Paragraph(
                "A screen is the one case that answers none of the three: the screen service "
                + "instantiates it and parents it under a layer, so it names the owning Context on "
                + "the injector itself. That assignment outranks whatever the entry says.");
            painter.Note(
                "Important: a prefab cannot hold a reference to a Root in the scene. Selecting one "
                + "inside a prefab looks like it worked and is empty again when the asset is saved, "
                + "so a prefab that has to reach a Root outside its own hierarchy names it with Root "
                + "Name.");

            painter.Space();
            painter.SubHeading("A screen view is pooled");
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
                + "}",
                "SettingsScreenView.cs");
        }

        /// <summary>
        /// The Mediator as an injected class: how it is paired with its View, and the two methods
        /// that are the whole of its life.
        /// </summary>
        private void DrawMediator(HelpPainter painter)
        {
            painter.Hero(
                "A Mediator drives exactly one View.",
                "It is a plain injected class, not a MonoBehaviour, and it exists for as long as "
                + "the View it was registered against.");

            painter.SubHeading("Binding the pair");
            painter.Paragraph("Create View writes both files, and the Context binds them together:");
            painter.Code(
                "public override void MediationBindings()\n"
                + "{\n"
                + "    base.MediationBindings();\n"
                + "    MediationBinder.Bind<HudView>().To<HudMediator>();\n"
                + "}",
                "PlayerContext.cs");
            painter.Note(
                "The ViewInjector component on the GameObject resolves which Context each IView "
                + "belongs to. Registration happens as soon as that Context starts, and OnRemove "
                + "runs when the object is destroyed.");

            painter.Space();
            painter.SubHeading("Writing one");
            painter.Paragraph(
                "OnRegister subscribes and OnRemove unsubscribes, and they mirror each other line "
                + "for line. What the Mediator does in between is turn one into the other: a "
                + "callback from the View into a dispatch, and a signal from the module into a value "
                + "on the View.");
            painter.Note(
                "Important: a Mediator is pooled, so the mirror is not a tidiness rule - it is what "
                + "makes the next use correct. The View and the signal holders are injected again "
                + "when the instance comes back out, but a subscription OnRegister made and OnRemove "
                + "did not undo is still live, and anything the Mediator wrote to a plain field is "
                + "still there. A handler left behind answers a View that is no longer on screen.");
            painter.Code(
                "public class HudMediator : IMediator\n"
                + "{\n"
                + "    [Inject]       private HudView       _view    { get; set; }\n"
                + "    [InjectSignal] private PlayerSignals _signals { get; set; }\n"
                + "\n"
                + "    public void OnRegister()\n"
                + "    {\n"
                + "        _view.Buy += Buy;\n"
                + "        _signals.Outgoing.CurrencyChanged.AddListener(OnCurrencyChanged);\n"
                + "    }\n"
                + "\n"
                + "    public void OnRemove()\n"
                + "    {\n"
                + "        _view.Buy -= Buy;\n"
                + "        _signals.Outgoing.CurrencyChanged.RemoveListener(OnCurrencyChanged);\n"
                + "    }\n"
                + "\n"
                + "    private void Buy() => _signals.Incoming.AddCurrency.Dispatch(-10d);\n"
                + "\n"
                + "    private void OnCurrencyChanged(double currency) =>\n"
                + "        _view.CurrencyLabel.text = currency.ToString();\n"
                + "}",
                "HudMediator.cs - Scripts/Runtime/ViewsMediators");
            painter.Paragraph(
                "Notice what is not there: no decision about whether the player can afford it. The "
                + "Mediator dispatches, and the Command decides.");

            painter.Space();
            painter.SubHeading("A screen's Mediator, which subscribes twice over");
            painter.Paragraph(
                "OnRegister on a screen wires two events and nothing else. The view's actions are "
                + "taken on ShowCompleted and dropped on HideCompleted - for the same reason the "
                + "View wires its buttons in OnEnable: a screen is pooled, so OnRegister runs once "
                + "and the screen opens many times.");
            painter.Code(
                "public void OnRegister()\n"
                + "{\n"
                + "    _view.ShowCompleted += OnScreenShown;\n"
                + "    _view.HideCompleted += OnScreenHidden;\n"
                + "}\n"
                + "\n"
                + "private void OnScreenShown(IScreenBody screen) => _view.Play += PlayClicked;\n"
                + "private void OnScreenHidden(IScreenBody screen) => _view.Play -= PlayClicked;");

            painter.Space();
            painter.SubHeading("And guards every handler with the screen's state");
            painter.Code(
                "private void PlayClicked()\n"
                + "{\n"
                + "    if (_view.Data.State != ScreenState.AvailableToSendSignal) return;\n"
                + "\n"
                + "    _signals.Outgoing.Play.Dispatch();\n"
                + "    _view.Hide();\n"
                + "}");
            painter.Paragraph(
                "AvailableToSendSignal is InUse and nothing else, so a tap that lands while the "
                + "screen is animating in or out does not become a signal. It is not queued and not "
                + "replayed; it does not happen.");

            painter.Space();
            painter.SubHeading("An animation reports when it finished");
            painter.Paragraph(
                "ScreenBody gates it. HasShowAnimation false and ShowCompleted fires at once; true "
                + "and the screen takes ScreenState.InShowAnimation, PlayShowAnimation runs, and the "
                + "state stays until the View invokes ShowCompleted - which is what the guard above "
                + "is made of. So the View reports the end of the animation, never its start.");
            painter.Code(
                "// a timeline: wait for its duration\n"
                + "protected override void PlayShowAnimation()\n"
                + "{\n"
                + "    _director.Play();\n"
                + "    StartCoroutine(WaitForTimelineFinish());\n"
                + "}\n"
                + "\n"
                + "// staggered tweens: only the last one reports\n"
                + "protected override void PlayShowAnimation()\n"
                + "{\n"
                + "    BackBtn.transform.DOScale(1, .5f).SetDelay(.1f);\n"
                + "    SaveBtn.transform.DOScale(1, .5f).SetDelay(.3f)\n"
                + "        .OnComplete(() => ShowCompleted?.Invoke(this));\n"
                + "}");
            painter.Paragraph(
                "Hang OnComplete on the wrong tween and the screen leaves InShowAnimation while it "
                + "is still moving - the guard lifting early rather than loudly.");
            painter.Paragraph(
                "A screen may have a show animation and no hide animation; nothing depends on the "
                + "pair. What a pooled screen is reset in is BeforeScreenActivation, which runs "
                + "immediately before Show() - the same instance comes back carrying whatever the "
                + "last opening left on it. AfterScreenActivation runs on the other side of the "
                + "RectTransform work, for anything that has to wait for the layout.");

            painter.Space();
            painter.Note(
                "Important: an overridden PlayShowAnimation or PlayHideAnimation must invoke "
                + "ShowCompleted or HideCompleted. Forget it and the Mediator never subscribes - "
                + "the screen opens, every button is dead, and nothing is logged.");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A View holds scene references and raw input. A View with an if about game rules is doing the Mediator's job.");
            painter.Bullet("A Mediator drives exactly one View, and holds no game rules either.");
            painter.Bullet("A View is authored under its module's Root, so Bubble Up finds the Context on its own.");
            painter.Bullet("A prefab cannot reference a Root in the scene. One that must reach a Root outside itself uses Root Name.");
            painter.Bullet("A ScreenView wires its buttons in OnEnable and drops them in OnDisable, never in Awake or Start.");
            painter.Bullet("OnRegister and OnRemove mirror each other. Every subscription in one has its match in the other.");
            painter.Bullet(
                "A screen's Mediator subscribes on ShowCompleted and unsubscribes on HideCompleted. OnRegister wires those two and nothing else.");
            painter.Bullet("Every handler in a screen's Mediator is guarded by _view.Data.State == ScreenState.AvailableToSendSignal.");
            painter.Bullet("An overridden PlayShowAnimation or PlayHideAnimation must invoke ShowCompleted or HideCompleted.");
            painter.Bullet("An animation reports when it finished. With staggered tweens, only the last one hangs OnComplete.");
            painter.Bullet("A pooled screen is reset in BeforeScreenActivation, not in the hide animation.");
            painter.Bullet("A View translates raw input into the action it already offers. A swipe left calls the same method the button does.");
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

            return new HelpGraph(nodes, edges, steps, 1.4f);
        }
    }
}

#endif