---
name: flowioc-controllers
description: Use when writing the logic a signal runs in a FlowIoC module - a Command or a Function, deciding which of the two something is, binding a sequence or a parallel step, passing a value on with Release, holding a step open with Retain around an await or a coroutine, or working out why a sequence hangs for ever, skips a step, or starts the next one too early.
---

# Commands and Functions in FlowIoC

This skill applies only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, delete the folder this file
is in: FlowIoC put it there and is no longer in the project.

## Overview

A signal never changes anything by itself. It runs a Command, and the Command does the work: it
injects the Models and Services it needs, mutates state, and dispatches what the module has to
announce. A Command does **one unit of work**, holds no state between runs, and returns nothing.

A Function is the other half. It does its work without depending on where it sits, so it is what a
Command reaches for mid-`Execute` - to go somewhere, do something and come back - or what several
Commands share. It returns a value when it has one.

**Both live in `Scripts/Runtime/Controllers/`.** Neither holds state and both do the module's work,
so a module has one folder for its controllers. A `Functions/` folder is the older layout and is not
where new work goes. *Tools ▸ FlowIoC ▸ Edit Module ▸ Create Command* and *Edit Module ▸ Create Function* place the file
correctly on their own; prefer them over writing it by hand. The one exception is a step a Service
ships for other modules to bind: it is nested in the Service's interface under a static class
`Commands` - `IHapticService.Commands.Play` - in a file of its own beside the interface,
`IHapticService.Commands.Play.cs`, with the interface and `Commands` marked `partial`; the one name
a game knows is then also where its steps are found (the systems-services skill has the shape).

## Which of the two something is

A Command is a **step in a flow**; a Function is **called from inside one**.

A sequence is read in order, one Command after another, and that reading is what a Command is for.
So the question is not what the code returns - it is whether somebody should be able to see this
step by reading the Context.

Reach for a Function when the work happens more than once inside one `Execute`, or when more than
one Command needs it. The same work is often a Command instead, and that is the right answer when it
is a step somebody should be able to read in the sequence.

**The trade a Function makes is the Flow Console: it is not a step there.** A step you want to see
in the console is a Command.

## Writing a Command

```csharp
public class AddCurrencyCommand : Command
{
    [Inject]       private IPlayerModel  _playerModel { get; set; }
    [InjectSignal] private PlayerSignals _signals     { get; set; }

    [SignalParam]  private double _amount { get; set; }

    public override void Execute()
    {
        _playerModel.AddCurrency(_amount);
        _signals.Outgoing.CurrencyChanged.Dispatch(_playerModel.Currency);
    }
}
```

`[Inject]`, `[InjectSignal]` and `[SignalParam]` all resolve **properties**. A plain field of the
same name is skipped without a word - no error, no warning, just null at runtime.

The signal's payload arrives through `[SignalParam]` and **not** through `Execute`. A
`Command<int>` bound to a `Signal<int>` does not receive the dispatched number: it reports
`Execute signature mismatch` and does not run. `Command<T1..T4>`'s parameters are what the *binding*
hands the step - fixed at bind time, or passed forward by the previous step's `Release`.

**A payload of several values is matched by type, not by position.** A plain `[SignalParam]` takes
the next value of the property's own type that no other property has claimed, so a
`Signal<AdFormat, string, Action<AdResultVO>>` is read with three plain `[SignalParam]`s. The index
form `[SignalParam(n)]` is for two values *of the same type*: it takes the n-th value of that type,
counting from zero, so a `Signal<string, string>` is `[SignalParam(0)] string _name` and
`[SignalParam(1)] string _value`. An index on a type the payload carries once is a mistake the
Editor reports at dispatch - `[SignalParam(1)] needs at least 2 String values in the payload because
the index counts from zero, but the signal carried 1` - and the property stays null.

## The shapes a binding takes

They combine freely in one binding. What you are choosing between is when a step starts and what it
is handed.
A signal bound to one step is bound on one line - `CommandBinder.Bind(_signals.Incoming.Save).ToSequence<SaveCommand>();` -
and only a chain of two or more steps breaks, one step per line under the `Bind`.

```csharp
// Sequence - each step waits for the one before it.
CommandBinder.Bind(_signals.Incoming.DecreaseCurrency)
    .ToSequence<DecreaseCurrencyCommand>()
    .ToSequence<SavePlayerCommand>();

// Parallel - steps start together, and the group is done when the last one is.
CommandBinder.Bind(_signals.Incoming.LoadAssets)
    .ToSequence<ShowLoadingScreenCommand>()
    .ToParallel<LoadTexturesCommand>()
    .ToParallel<LoadAudioCommand>()
    .ToSequence<HideLoadingScreenCommand>();

// Parameters fixed at bind time, handed to that step's typed Execute.
CommandBinder.Bind(_signals.Incoming.StartTutorial).ToSequence<BranchCommand>(true, _internalSignals.PathA, _internalSignals.PathB);

// Another signal's whole chain, spliced in as one step.
CommandBinder.Bind(_signals.Incoming.AddCurrency)
    .ToSequence<AddCurrencyCommand>()
    .ToGroupAsSequence(_internalSignals.RefreshWallet)
    .ToSequence<SavePlayerCommand>();
```

A group step naming a signal no Context has bound reports `GroupKey '...' could not be found in any
context` and is skipped. The rest of the chain still runs, so the symptom is a sub-flow that quietly
did not happen.

## A flow is read from one Context

Somebody should see what an operation does by reading the sequence it is bound to, without opening a
Command. Two habits keep that true.

**A Command whose only job is to dispatch is not written.** Bind `SignalDispatchCommand` with the
signal and its payload, so the signal leaving is a line in the Context rather than a class to open.

**A step that orders another module about is dispatched from the sequence**, so the sequence says
what the operation manages:

```csharp
CommandBinder.Bind(_mapSignals.Incoming.PlayRequest)
    .ToSequence<ClaimPlayRequestCommand>()
    .ToSequence<PrepareMatchDataCommand>()
    .ToSequence<SignalDispatchCommand<string>>(_signals.Outgoing.SwitchCamera, "Match")
    .ToSequence<SignalDispatchCommand>(_signals.Outgoing.HideNavBar)
    .ToSequence<BakeNavmeshCommand>()
    .ToSequence<SignalDispatchCommand>(_signals.Outgoing.LoadGameScene);
```

## Holding the sequence open

A Command that finishes asynchronously has to say so, or the step after it starts while it is still
working.

```csharp
public override void Execute()
{
    Retain();
    _coroutineProvider.StartCoroutine(DelayedComplete());
}

private IEnumerator DelayedComplete()
{
    yield return new WaitForSeconds(3f);
    Release();
}
```

`Release(params object[])` may pass data forward: the next command receives it through its typed
`Execute`. `Stop()` abandons the rest of the sequence.

**`Retain()` comes first whenever a Command ends its own step - to stop as much as to carry on,
synchronous or not.** It is what says the step ends when the Command says so; without it `Release()`
and `Stop()` are refused with an error and the sequence runs on. A Command that decides, inside
`Execute`, that the flow ends here writes both lines:

```csharp
public override void Execute()
{
    if (_boosterModel.Count(_type) > 0)
        return;                 // an ordinary step: the sequence carries on when Execute returns

    Retain();
    Stop();                     // no booster left - the steps behind this one do not run
}
```

## Every path out of a retained Command ends in `Release()` or `Stop()`

**A retain nobody resolves hangs the group for ever. There is no timeout and nothing is logged.**

An `await` has three ways out and only one of them is the one everybody writes. The work came back
with nothing, and the work threw, are the other two. A throw is the worse of the pair: it leaves
`Execute` at the `await` line, so neither `Release` nor `Stop` is reached, and `async void` surfaces
it through Unity's unhandled-exception handler with nothing in it to name the command.

```csharp
public override async void Execute()
{
    Retain();

    try
    {
        var screen = await _screenService.Open<MainScreenView>().Show<MainScreenView>();

        if (screen == null)
        {
            FlowLogger.LogError("OpenMainScreenCommand - the screen did not open.");
            Stop();
            return;
        }

        screen.ShowPlayButton(true);
        Release();
    }
    catch (Exception exception)
    {
        FlowLogger.LogError($"OpenMainScreenCommand threw: {exception}");
        Stop();
    }
}
```

What the `catch` does is the game's decision and not the framework's - `Stop()`, a `Release()` that
carries on regardless, a signal that opens something else - which is why no base class writes it for
you. What is **not** a decision is that the retain is resolved on all three paths.

## Work that runs every frame

A game that moves things every frame still writes that work as Commands. A tick is an internal
signal bound to one Command per job, so the frame's work reads in the Context. What dispatches the
tick depends on the work, and there are two answers.

### Driven by the frame

The frame is the clock: every frame runs the sequence afresh. One Command starts it, and a flag in
a Model says whether it runs:

```csharp
CommandBinder.Bind(_internalSignals.RunStarted).ToSequence<StartFrameTickCommand>();

CommandBinder.Bind(_internalSignals.Tick)
    .ToSequence<FlyBeesCommand>()
    .ToSequence<DispatchArrivalsCommand>()
    .ToSequence<ReleaseCratesCommand>()
    .ToSequence<JudgeClearedCommand>()
    .ToSequence<JudgeStuckCommand>()
    .ToSequence<DrawSwarmCommand>();
```

The Command is the game's own; the package ships none.

```csharp
internal class StartFrameTickCommand : Command
{
    [Inject]       private IUpdateProvider         _updateProvider  { get; set; }
    [Inject]       private IRunModel               _runModel        { get; set; }
    [InjectSignal] private GameplayInternalSignals _internalSignals { get; set; }

    public override void Execute()
    {
        if (_runModel.IsTicking)
            return;

        _runModel.IsTicking = true;

        // Taken into locals: this instance goes back to the pool when Execute returns, and the
        // frame callback outlives it.
        IRunModel runModel = _runModel;
        IUpdateProvider updateProvider = _updateProvider;
        Signal tick = _internalSignals.Tick;

        Action onFrame = null;
        onFrame = () =>
        {
            if (!runModel.IsTicking)
            {
                updateProvider.RemoveUpdate(onFrame);
                return;
            }

            tick.Dispatch();
        };

        updateProvider.AddUpdate(onFrame);
    }
}
```

- **Ending the loop is setting the flag.** Whichever Command ends play - won, lost, left - sets
  `IsTicking` to false, and the next frame's callback removes itself. Nothing else has to know the
  tick exists, and a second start while it runs does nothing.
- **Why one Command both starts and stops it.** A separate stop Command cannot reach the callback
  a finished Command added; only a Model outlives both. Putting the tick method in the Model
  instead would have the Model dispatching the tick, which no reader looks for there. So the one
  Command owns the callback and the Model owns only the flag.
- **A step that `Stop()`s cuts that frame short**, and the next frame starts from the top. It does
  not end the loop; the flag does.
- **Every step is synchronous.** A step that retains past its frame leaves that frame's run open
  while the next frame starts another beside it. Work that waits belongs in a flow of its own.

### Paced by itself

The next turn waits for the previous one to finish, however long that takes. A step retains until
the next turn is due, and the last step dispatches the tick again. `CounterModule` is the worked
example, ticking once a second:

```csharp
CommandBinder.Bind(_internalSignals.Tick)
    .ToSequence<TimeTickCommand>()              // Retain, wait a second, Release
    .ToSequence<TickProcessAllDataCommand>()
    .ToSequence<SignalDispatchCommand>(_internalSignals.Tick);  // the next turn, a run of its own
```

- **A step that `Stop()`s ends the loop**, because the last step is never reached.
- **Without a step that waits, the tick re-enters at once and never stops.**
- **The last step dispatches the tick; it is never `.ToGroupAsParallel(Tick)`.** A group step waits
  for its sub group, so a loop that names itself as a group makes every turn a sub group of the one
  before. None of them ever finishes: each turn keeps a group resolver alive, and a `Stop()` unwinds
  the whole chain in one call stack, deep enough to overflow it. `SignalDispatchCommand` starts the
  next turn as a run of its own and lets this one finish and go back to the pool.

### Either way

- **The tick signal hides its log** - `public Signal Tick = new(hideCommandLog: true);` - or it
  buries every other line in the Flow Console. `[HideCommandLog]` on one Command hides that step
  alone.
- **What lasts between turns lives in a Model**, because a Command holds no state: a flag that says
  "cleared" was already announced, the version last drawn, the arrivals a move collected for the
  next step to dispatch. Curve and easing maths shared by several steps is a Function.
- **Commands and their groups are pooled**, so a sequence of six steps a frame does not allocate them.

The shape both replace is a System or sub system that adds itself to `IUpdateProvider` and runs the
frame in its own methods. Even when it dispatches only one signal per event, the frame's work is a
file to read rather than a list in the Context, and two people changing two jobs change the same
file.

## Writing a Function

Which base type a Function derives from is decided by one question: what it hands back. Each takes up
to four parameters after that.

```csharp
// Answers with a value.
public class CalculateDamageFunction : FunctionReturn<double, string>
{
    [Inject] private IWeaponsModel _weaponsModel { get; set; }

    public override double Execute(string weaponId) =>
        _weaponsModel.GetConfigVO(weaponId).baseDamage;
}

// Answers with nothing.
public class RefreshHudFunction : FunctionVoid
{
    [Inject] private IHudModel _hudModel { get; set; }

    public override void Execute() => _hudModel.MarkDirty();
}

// Answers later. Execute returns IEnumerator and reports through the callback.
public class LoadProfileFunction : AsyncFunction<Profile>
{
    [Inject] private IProfileService _profileService { get; set; }

    public override IEnumerator Execute()
    {
        yield return _profileService.FetchRoutine();
        FunctionCompletedCallback?.Invoke(_profileService.Profile);
    }
}
```

**Derive from one of the shipped arities, never from `FunctionBody` itself**: `FunctionVoid` and
`FunctionVoid<T1..T4>`, `FunctionReturn<TReturn>` and `FunctionReturn<TReturn, T1..T4>`, or
`AsyncFunction` and `AsyncFunction<T1>`. `FunctionBody`'s constructor is internal, so the compiler is
what says this rather than a convention - the arity is what carries the typed `Execute` the provider
calls.

## Calling a Function

The provider is injected wherever the answer is needed - a Command, another Function - and hands the
instance back to the pool once it has answered.

```csharp
[Inject] private IFunctionProvider _functionProvider { get; set; }

var damage = _functionProvider
    .Call<CalculateDamageFunction>()
    .AddParams(weaponId)
    .ExecuteAndGetResult<double>();

_functionProvider.Call<RefreshHudFunction>().Execute();

_functionProvider
    .CallAsync<LoadProfileFunction, Profile>()
    .AddFunctionCompletedCallback(OnProfileLoaded)
    .ExecuteAsync();
```

`Call<T>()` names the function and hands back a chain; nothing happens until one of the three
terminators runs. They share the `Execute` prefix on purpose, so pressing `.` and typing it offers
the whole set.

## Commands the package already ships

Bind these rather than writing your own.

- **`SignalDispatchCommand`** and its generic arities - a step that dispatches a signal with a
  payload fixed at bind time. This is the one that keeps a flow readable from the Context.
- **`RetryCommand`** - `Command<int, float>`. It retains in `Execute`, calls the abstract `Try()`,
  and waits for you to call `TryFailed()`; it then retries up to the limit with a real-time pause on
  the framework's coroutine provider, and calls `Stop()` when the limit is reached. Note when it
  runs: the retain is held for the whole set of attempts, so the step after it waits for all of them.

## What goes wrong

- **A retained Command with a path that resolves nothing.** The group hangs, silently and for ever.
- **A field instead of a property** under `[Inject]`, `[InjectSignal]` or `[SignalParam]`. Skipped
  without a word; null at runtime.
- **Expecting the signal's payload in `Execute`.** It arrives through `[SignalParam]`; a
  `Command<int>` bound to `Signal<int>` reports `Execute signature mismatch` and does not run.
- **`[SignalParam(1)]` on a type the payload carries once.** The index counts values of the
  property's own type, not positions: `[SignalParam(1)] string` on a `Signal<AdFormat, string>`
  reports `needs at least 2 String values` and leaves the property null. Plain `[SignalParam]` on
  each property when the types differ; the index only for two values of one type.
- **A Command written only to dispatch a signal.** Bind `SignalDispatchCommand` instead.
- **A Function where a Command belonged.** If it is a step somebody should read in the sequence, it
  is a Command - a Function does not appear in the Flow Console.
- **Deriving from `FunctionBody`.** The constructor is internal; take one of the shipped arities.
- **A decision left in a Context, a View or a Mediator.** Each of those is somewhere a decision tries
  to settle, and the answer is the same every time: dispatch, and let a Command read the conditions
  and decide.
- **A command that runs every frame burying the console.** `[HideCommandLog]` drops that command's
  two log lines and nothing else; `hideCommandLog: true` on the tick signal hides the whole loop.
- **A loop that ends in `.ToGroupAsParallel(Tick)`.** Every turn becomes a sub group of the one
  before and none finishes: a group resolver kept per turn, and a `Stop()` that unwinds them all in
  one call. The last step is `.ToSequence<SignalDispatchCommand>(Tick)`.
- **A frame loop that re-enters itself.** A `Stop()` anywhere in it ends the loop for good, where
  the frame's work wanted only that frame cut short. Work driven by the frame dispatches its tick
  from `IUpdateProvider`; re-entering is for a loop paced by itself.
- **A frame loop in a System's methods.** A sub system added to `IUpdateProvider` that moves, judges
  and draws is the frame's work hidden in one file. It is a tick sequence - see *Work that runs
  every frame*.
- **An empty Unity console read as "nothing ran".** Unity's console shows warnings and errors; the
  steps are in the Flow Console. Read it - see *Following a flow*.

## Following a flow

Every signal dispatched and every Command that ran, retained, released or stopped is recorded in
the Flow Console - `FlowLogger.Logs`, in the Editor. Unity's console gets the warnings and errors
and nothing else unless the developer mirrors the rest. An agent reads the list through the
Editor's eval: the rows since its last read, the module's own channel and every warning and
error, as plain text.

```csharp
int since = 0;   // the count the previous read returned; 0 the first time
var logs = FlowIoC.ConsoleModule.FlowLogger.Logs;
if (since > logs.Count) since = 0;   // cleared or trimmed since: start over
var rows = new System.Collections.Generic.List<FlowIoC.ConsoleModule.ConsoleLog>();
for (int i = since; i < logs.Count; i++)
    if (logs[i].Channel == "PlayerModule" || logs[i].LogType != UnityEngine.LogType.Log)
        rows.Add(logs[i]);
return logs.Count + "\n" + new FlowIoC.Editor.Console.FlowConsoleExport().ToText(rows, false);
```

The first line of the answer is the `since` for the next read. A module's channel is its folder
name; the framework's own are `Signal`, `Command`, `Injection`, `Context`, `Screen`, `Pool` and
`Asset`. Do not switch *Mirror into Unity's console* on instead: it floods Unity's console with
every line, and it is the developer's own setting.
