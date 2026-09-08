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
            new HelpTab("Command", DrawCommand,
                "A Command is bound in the Context and run by a signal.",
                "It never constructs itself and nothing calls it by name. The binding is the whole "
                + "of what decides when it runs."),
            new HelpTab("Function", DrawFunction,
                "A Command is a step in a flow. A Function is called from inside one.",
                "A sequence is read in order, and that reading is what a Command is for. A Function "
                + "does its work without depending on where it sits, so it is what a Command "
                + "reaches for mid-Execute, and what several Commands share."),
            new HelpTab("Rules", DrawRules,
                "The rules, in one list.",
                "What a Command may hold, where a Function belongs, and what the Context is left saying.")
        };

        protected override string BodyHeadline => "Controllers is where a module thinks.";

        protected override string BodyTagline =>
            "Two kinds of code live in the folder, and they are told apart by one question: does "
            + "it answer you, or does it announce?";

        protected override void DrawBody(HelpPainter painter)
        {
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

            painter.Separator();
            painter.SubHeading("The shapes a binding takes");
            painter.Paragraph(
                "Five, and they combine freely in one binding. What you are choosing between is "
                + "when a step starts and what it is handed.");

            painter.Rule("Sequence - each step waits for the one before it");
            painter.Paragraph(
                "The ordinary shape. Use it where a step needs what the step before it did: the "
                + "save has to see the decreased balance.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.DecreaseCurrency)\n"
                + "    .ToSequence<DecreaseCurrencyCommand>()\n"
                + "    .ToSequence<SavePlayerCommand>();");

            painter.Space();
            painter.Rule("Parallel - steps start together");
            painter.Paragraph(
                "Use it where the steps do not touch each other's results. Three loads of 400 ms "
                + "cost 400 ms rather than 1200. The group carries on when the last of them is "
                + "done, so the sequence step after them still waits for all three.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.LoadAssets)\n"
                + "    .ToSequence<ShowLoadingScreenCommand>()\n"
                + "    .ToParallel<LoadTexturesCommand>()\n"
                + "    .ToParallel<LoadAudioCommand>()\n"
                + "    .ToParallel<LoadModelsCommand>()\n"
                + "    .ToSequence<HideLoadingScreenCommand>();");

            painter.Space();
            painter.Rule("Parameters fixed at bind time");
            painter.Paragraph(
                "Both terminators take arguments, and they are handed to that step's typed Execute. "
                + "This is how a decision stays out of the Context: the Context declares which "
                + "signals are in play, the Command picks which one fires.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.StartTutorial)\n"
                + "    .ToSequence<BranchCommand>(true, _internalSignals.PathA, _internalSignals.PathB);\n"
                + "\n"
                + "public class BranchCommand : Command<bool, Signal, Signal>\n"
                + "{\n"
                + "    public override void Execute(bool condition, Signal onTrue, Signal onFalse)\n"
                + "    {\n"
                + "        Retain();\n"
                + "        (condition ? onTrue : onFalse).Dispatch();\n"
                + "        Release();\n"
                + "    }\n"
                + "}");

            painter.Space();
            painter.Rule("Dispatching a signal is a step, not a Command you write");
            painter.Paragraph(
                "A Command whose only job is to dispatch is not written. Bind DispatchSignalCommand "
                + "with the signal and its payload, and the signal leaving is a line in the Context "
                + "rather than a class to open.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.GameOver)\n"
                + "    .ToSequence<SaveScoreCommand>()\n"
                + "    .ToSequence<DispatchSignalCommand<int>>(_signals.Outgoing.ScoreSubmitted, _score);");

            painter.Space();
            painter.Rule("Group - another signal's whole chain, spliced into this one");
            painter.Paragraph(
                "ToGroupAsSequence and ToGroupAsParallel run everything bound to another signal as "
                + "one step of this binding. Declare a shared sub-flow once and reuse it. As a "
                + "sequence the chain waits for the whole sub-flow; as a parallel it starts it and "
                + "moves on. The group's own signal is dispatched with the outer payload unless the "
                + "step names its own.");
            painter.Code(
                "// The shared sub-flow, bound once.\n"
                + "CommandBinder.Bind(_internalSignals.RefreshWallet)\n"
                + "    .ToSequence<ReadWalletCommand>()\n"
                + "    .ToSequence<PushWalletToHudCommand>();\n"
                + "\n"
                + "// Two flows that both need it.\n"
                + "CommandBinder.Bind(_signals.Incoming.AddCurrency)\n"
                + "    .ToSequence<AddCurrencyCommand>()\n"
                + "    .ToGroupAsSequence(_internalSignals.RefreshWallet)\n"
                + "    .ToSequence<SavePlayerCommand>();\n"
                + "\n"
                + "CommandBinder.Bind(_signals.Incoming.PurchaseCompleted)\n"
                + "    .ToSequence<GrantPurchaseCommand>()\n"
                + "    .ToGroupAsParallel(_internalSignals.RefreshWallet, CurrencyType.Soft);",
                "PlayerContext.cs");
            painter.Note(
                "A group step naming a signal no Context has bound reports \"GroupKey '...' could "
                + "not be found in any context\" and is skipped. The rest of the chain still runs, "
                + "so the symptom is a sub-flow that quietly did not happen.");

            painter.Space();
            painter.Graph(Sequence());

            painter.Separator();
            painter.SubHeading("Taking the signal's parameters");
            painter.Paragraph(
                "The signal declares what it carries. Each [SignalParam] property in the command is "
                + "filled from that payload, and the property is what is filled - a field of the "
                + "same name is skipped without a word.");
            painter.Note(
                "Important: the signal's payload does not reach Execute. A Command<int> bound to a "
                + "Signal<int> does not receive the dispatched number - it reports \"Execute "
                + "signature mismatch\" and does not run. The payload arrives through [SignalParam].");

            painter.Separator();
            painter.SubHeading("Where a command's data comes from");
            painter.Paragraph(
                "Three doors, and each value uses exactly one of them. Knowing which door a value "
                + "arrives through is most of knowing how to write the class.");
            painter.Bullet("The signal's payload arrives in [SignalParam] properties.");
            painter.Bullet("Execute's parameters come from the binding, or from the previous command's Release.");
            painter.Bullet("Models, services and signal holders arrive in [Inject] and [InjectSignal] properties.");
            painter.Paragraph(
                "Execute has exactly those two sources and the signal is neither of them. When a "
                + "step has both, the binding wins: arguments written at bind time are what that "
                + "step is given, whatever the command before it released.");
            painter.Code(
                "// Bound to Signal<int>, dispatched with 7.\n"
                + "\n"
                + "public class WrongCommand : Command<int>\n"
                + "{\n"
                + "    // Never runs: the binding gave Execute nothing, so this reports\n"
                + "    // \"it takes 1 parameter(s) and the signal carried 0\".\n"
                + "    public override void Execute(int amount) { }\n"
                + "}\n"
                + "\n"
                + "public class RightCommand : Command\n"
                + "{\n"
                + "    [SignalParam] private int _amount { get; set; }   // 7\n"
                + "\n"
                + "    public override void Execute() { }\n"
                + "}");
            painter.Paragraph(
                "The other way in is the step before. What a retained command passes to Release "
                + "becomes the next sequence step's Execute parameters, which is how one step hands "
                + "its result to the next without either of them knowing a signal.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.Connect)\n"
                + "    .ToSequence<LoadConfigCommand>()\n"
                + "    .ToSequence<ConnectToServerCommand>();\n"
                + "\n"
                + "public class LoadConfigCommand : Command\n"
                + "{\n"
                + "    public override void Execute()\n"
                + "    {\n"
                + "        Retain();\n"
                + "        LoadConfig(config => Release(config.ServerUrl, config.Timeout));\n"
                + "    }\n"
                + "}\n"
                + "\n"
                + "public class ConnectToServerCommand : Command<string, int>\n"
                + "{\n"
                + "    public override void Execute(string url, int timeout) { }\n"
                + "}");
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

            painter.Separator();
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

            painter.Separator();
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

            painter.Space();
            painter.Rule("Every way out of a retained command resolves the retain");
            painter.Paragraph(
                "A retain nobody resolves hangs the group for ever: there is no timeout and nothing "
                + "is logged. An await has three ways out and only one of them is the one everybody "
                + "writes - the work came back with nothing, and the work threw, are the other two. "
                + "A throw is the worse of the pair: it leaves Execute at the await line, so neither "
                + "Release nor Stop is reached, and async void surfaces it through Unity's "
                + "unhandled-exception handler with nothing in it to name the command.");
            painter.Code(
                "public override async void Execute()\n"
                + "{\n"
                + "    Retain();\n"
                + "\n"
                + "    try\n"
                + "    {\n"
                + "        var screen = await _screenService.Open<MainScreenView>()\n"
                + "            .Show<MainScreenView>();\n"
                + "\n"
                + "        if (screen == null)\n"
                + "        {\n"
                + "            FlowLogger.LogError(FlowLogType.MainScreenModule,\n"
                + "                \"OpenMainScreenCommand - the screen did not open.\");\n"
                + "            Stop();\n"
                + "            return;\n"
                + "        }\n"
                + "\n"
                + "        screen.ShowPlayButton(true);\n"
                + "        Release();\n"
                + "    }\n"
                + "    catch (Exception exception)\n"
                + "    {\n"
                + "        FlowLogger.LogError(FlowLogType.MainScreenModule,\n"
                + "            $\"OpenMainScreenCommand threw: {exception}\");\n"
                + "        Stop();\n"
                + "    }\n"
                + "}");
            painter.Paragraph(
                "What the catch does is your decision and not the framework's - Stop(), a Release() "
                + "that carries on regardless, or a signal that opens something else - which is why "
                + "no base class writes it for you. What is not a decision is that the retain has to "
                + "be resolved on all three paths.");
            painter.Note(
                "Prefer Show<T>() over Show(). A typed view compares against null through Unity's "
                + "own operator; an IScreenBody is an interface and does not, so a destroyed screen "
                + "would not read as null.");

            painter.Separator();
            painter.SubHeading("A flow is read from one Context");
            painter.Paragraph(
                "Somebody should see what an operation does by reading the sequence it is bound to, "
                + "without opening a Command or crossing to the Connector. Two habits keep that "
                + "true.");
            painter.Paragraph(
                "A Command whose only job is to dispatch is not written. Bind DispatchSignalCommand "
                + "with the signal and its payload, and the signal leaving becomes a line in the "
                + "Context rather than something a reader finds by opening a class.");
            painter.Paragraph(
                "And a step that orders another module about - hide the nav bar, switch the "
                + "camera - is dispatched from the sequence, so the sequence says what the "
                + "operation manages.");
            painter.Code(
                "CommandBinder.Bind(_mapSignals.Incoming.PlayRequest)\n"
                + "    .ToSequence<ClaimPlayRequestCommand>()\n"
                + "    .ToSequence<PrepareMatchDataCommand>()\n"
                + "    .ToSequence<DispatchSignalCommand<string>>(_signals.Outgoing.SwitchCamera, \"Match\")\n"
                + "    .ToSequence<DispatchSignalCommand>(_signals.Outgoing.HideNavBar)\n"
                + "    .ToSequence<DispatchSignalCommand>(_signals.Outgoing.HideTopBar)\n"
                + "    .ToSequence<BakeNavmeshCommand>()\n"
                + "    .ToSequence<DispatchSignalCommand>(_signals.Outgoing.LoadGameScene);",
                "Starting a match, read without leaving the Context");

            painter.Space();
            painter.Note(
                "The other side of it: a list of consequences hung off one announcement belongs "
                + "here, in the sequence, and not in the Connector. A Connector translates one "
                + "announcement into one order; it does not decide what a thing having happened "
                + "should cause.");

            painter.Separator();
            painter.SubHeading("Silencing a command that runs every frame");
            painter.Paragraph(
                "The Flow Console logs every command as it executes and again as it returns to "
                + "the pool, which is what makes a flow readable - until one command runs sixty "
                + "times a second and buries the rest. [HideCommandLog] drops those two lines for "
                + "that command alone.");
            painter.Code(
                "[HideCommandLog]\n"
                + "internal class AdvanceTimersCommand : Command\n"
                + "{\n"
                + "    [Inject] private ITimerModel _timers { get; set; }\n"
                + "\n"
                + "    public override void Execute() => _timers.Advance();\n"
                + "}",
                "AdvanceTimersCommand.cs - Scripts/Runtime/Controllers");
            painter.Paragraph(
                "It silences the framework's lines only. What the command logs itself still "
                + "appears, which is what makes the attribute worth reaching for instead of "
                + "switching the whole channel off - the loop stops narrating itself and still "
                + "says the one thing you asked it to.");
            painter.Note(
                "The signal half of the same loop is silenced separately, with "
                + "new Signal(hideCommandLog: true) where the signal is declared. The attribute "
                + "does not cover the dispatch and the flag does not cover the command lines, so "
                + "a fully silent loop carries both.");
        }

        /// <summary>
        /// The Function beside it. Short on purpose: a Function is the simpler of the two, and
        /// most of what is worth saying about it is what it deliberately does not do.
        /// </summary>
        private void DrawFunction(HelpPainter painter)
        {
            painter.SubHeading("When to reach for one");
            painter.Bullet("The work happens more than once inside one Execute.");
            painter.Bullet("More than one Command needs it. Map's FindEditHexLinksFunction is called from twelve.");
            painter.Bullet(
                "The Command has to go somewhere, do something and come back, rather than hand the "
                + "next step of the flow on.");
            painter.Paragraph(
                "The same work is often a Command instead, and that is the right answer when it is "
                + "a step somebody should be able to read in the sequence. The trade a Function "
                + "makes is the Flow Console: it is not a step there.");

            painter.Separator();
            painter.SubHeading("The three kinds");
            painter.Paragraph(
                "Which base type a function derives from is decided by one question: what it hands "
                + "back. Each takes up to four parameters after that, so there are five arities of "
                + "each - FunctionVoid through FunctionVoid<T1, T2, T3, T4> - and AsyncFunction has "
                + "two, one carrying a value into its callback and one carrying nothing.");
            painter.Parts(
                new HelpPart("Answers with a value",
                    "The return type comes first and the parameters after it. This is the ordinary "
                    + "Function.",
                    "class CalculateDamageFunction : FunctionReturn<double, string>"),
                new HelpPart("Answers with nothing",
                    "Work that happens where you stand and hands nothing back - a redraw, a reset, "
                    + "a write.",
                    "class RefreshHudFunction : FunctionVoid"),
                new HelpPart("Answers later",
                    "Runs on a coroutine and reports through the callback the caller handed in. Its "
                    + "Execute returns IEnumerator.",
                    "class LoadProfileFunction : AsyncFunction<Profile>"));

            painter.Separator();
            painter.SubHeading("Writing one");
            painter.Paragraph(
                "A function injects whatever it needs, the way a Command does, and writes one "
                + "Execute. Nothing about it says where it is called from - that is the whole "
                + "difference from a Command.");
            painter.Code(
                "public class CalculateDamageFunction : FunctionReturn<double, string>\n"
                + "{\n"
                + "    [Inject] private IWeaponsModel _weaponsModel { get; set; }\n"
                + "\n"
                + "    public override double Execute(string weaponId) =>\n"
                + "        _weaponsModel.GetConfigVO(weaponId).baseDamage;\n"
                + "}",
                "CalculateDamageFunction.cs - Scripts/Runtime/Controllers");
            painter.Code(
                "public class RefreshHudFunction : FunctionVoid\n"
                + "{\n"
                + "    [Inject] private IHudModel _hudModel { get; set; }\n"
                + "\n"
                + "    public override void Execute() => _hudModel.MarkDirty();\n"
                + "}");
            painter.Paragraph(
                "An AsyncFunction's Execute returns IEnumerator, so it yields the way a coroutine "
                + "does, and it reports by invoking the callback the caller handed in rather than "
                + "by returning.");
            painter.Code(
                "public class LoadProfileFunction : AsyncFunction<Profile>\n"
                + "{\n"
                + "    [Inject] private IProfileService _profileService { get; set; }\n"
                + "\n"
                + "    public override IEnumerator Execute()\n"
                + "    {\n"
                + "        yield return _profileService.FetchRoutine();\n"
                + "\n"
                + "        FunctionCompletedCallback?.Invoke(_profileService.Profile);\n"
                + "    }\n"
                + "}");

            painter.Separator();
            painter.SubHeading("Calling one");
            painter.Paragraph(
                "The function provider is injected wherever the answer is needed - a Command, "
                + "another Function - and hands the instance back to the pool once it has answered.");
            painter.Code(
                "[Inject] private IFunctionProvider _functionProvider { get; set; }\n"
                + "\n"
                + "var damage = _functionProvider\n"
                + "    .Call<CalculateDamageFunction>()\n"
                + "    .AddParams(weaponId)\n"
                + "    .ExecuteAndGetResult<double>();\n"
                + "\n"
                + "_functionProvider.Call<RefreshHudFunction>().Execute();\n"
                + "\n"
                + "_functionProvider\n"
                + "    .CallAsync<LoadProfileFunction, Profile>()\n"
                + "    .AddFunctionCompletedCallback(OnProfileLoaded)\n"
                + "    .ExecuteAsync();");
            painter.Paragraph(
                "Call<T>() names the function and hands back a chain; nothing happens until one of "
                + "the three terminators is called. That is why the provider says Call and the "
                + "terminator says Execute - the same word the function's own method carries, and "
                + "the same word a Command runs under. The three share the Execute prefix on "
                + "purpose: typing E after the dot offers all three rather than making you know in "
                + "advance which one this function needs, and the type parameter of "
                + "ExecuteAndGetResult is spelled out so it reads as what it is - double is what "
                + "comes back, not something being passed in.");

            painter.Space();
            painter.Rule("Which terminator");
            painter.Parts(
                new HelpPart("FunctionReturn",
                    "Answers with a value, so the call site reads as an assignment.",
                    "Call<T>().ExecuteAndGetResult<double>()"),
                new HelpPart("FunctionVoid",
                    "Answers with nothing, so the call is a statement of its own.",
                    "Call<T>().Execute()"),
                new HelpPart("AsyncFunction",
                    "Started here and answered later, through the callback. CallAsync, not Call.",
                    "CallAsync<T, TValue>().ExecuteAsync()"));

            painter.Space();
            painter.Rule("Parameters");
            painter.Paragraph(
                "AddParams lines the values up for the typed Execute, in the order it declares "
                + "them. Too few, too many, or one of the wrong type is reported by name and the "
                + "function does not run - the answer comes back as the return type's default "
                + "rather than as an exception, because the mistake is in a call site somebody has "
                + "to be able to find.");
            painter.Code(
                "_functionProvider\n"
                + "    .Call<FindLinksFunction>()\n"
                + "    .AddParams(hexId, radius, includeDiagonals)\n"
                + "    .ExecuteAndGetResult<List<HexLink>>();");

            painter.Space();
            painter.Rule("Holding one open");
            painter.Paragraph(
                "A function goes back to the pool the moment its Execute returns, so a function "
                + "that hands its instance to something outliving the call - a subscription, a "
                + "service that will call back - says Retain() and keeps it until Release(). An "
                + "AsyncFunction needs neither: the provider runs its coroutine to the end before "
                + "it pools anything.");
            painter.Code(
                "public class WatchDownloadFunction : FunctionVoid<string>\n"
                + "{\n"
                + "    [Inject] private IDownloadService _downloadService { get; set; }\n"
                + "\n"
                + "    public override void Execute(string url)\n"
                + "    {\n"
                + "        Retain();\n"
                + "        _downloadService.Watch(url, OnFinished);\n"
                + "    }\n"
                + "\n"
                + "    private void OnFinished() => Release();\n"
                + "}");
            painter.Note(
                "A Function may retain and release inside one Execute, the way a Command may: the "
                + "instance is already back in the pool when the run is judged, and the run knows "
                + "not to pool it again. Release on a function that never retained is reported and "
                + "does nothing.");

            painter.Space();
            painter.Note(
                "A Function is deliberately invisible in the Flow Console. If you want the step "
                + "logged with the rest of the flow, the work belongs in a Command instead.");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A decision belongs in a Command, wherever it would otherwise be taken - a Context, a View, a Mediator, a System.");
            painter.Bullet("A flow is read from one Context. A Command whose only job is to dispatch is not written - bind DispatchSignalCommand.");
            painter.Bullet("A list of consequences hung off one announcement belongs in the sequence, not in the Connector.");
            painter.Bullet("What needs no decision is not one. A close button that always closes is the Mediator calling _view.Hide().");
            painter.Bullet("A Command does one unit of work, holds no state between runs, and returns no value.");
            painter.Bullet(
                "Every way out of a retained Command resolves the retain. An await has three: it worked, it came back with nothing, it threw.");
            painter.Bullet("A Command never touches another module's model. What it needs from elsewhere arrives as a signal.");
            painter.Bullet("A Command is a step in a flow. A Function is called from inside one, and is not a step in the console.");
            painter.Bullet("Reach for a Function when the work happens twice inside one Execute, or when more than one Command needs it.");
            painter.Bullet("Injection targets properties. A plain field is skipped silently - no error, no warning, null at runtime.");
            painter.Bullet("A Command and a Function both live in Scripts/Runtime/Controllers. Both are controllers.");
            painter.Bullet("A Function derives from a shipped arity - FunctionVoid, FunctionReturn or AsyncFunction - never from FunctionBody.");
            painter.Bullet("Create Command and Create Function write the file for you. Prefer them over writing either by hand.");
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