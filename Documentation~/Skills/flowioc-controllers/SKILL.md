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
where new work goes. *Tools ▸ FlowIoC ▸ Create Command* and *Create Function* place the file
correctly on their own; prefer them over writing it by hand.

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

## The shapes a binding takes

They combine freely in one binding. What you are choosing between is when a step starts and what it
is handed.

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
CommandBinder.Bind(_signals.Incoming.StartTutorial)
    .ToSequence<BranchCommand>(true, _internalSignals.PathA, _internalSignals.PathB);

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

**A Command whose only job is to dispatch is not written.** Bind `DispatchSignalCommand` with the
signal and its payload, so the signal leaving is a line in the Context rather than a class to open.

**A step that orders another module about is dispatched from the sequence**, so the sequence says
what the operation manages:

```csharp
CommandBinder.Bind(_mapSignals.Incoming.PlayRequest)
    .ToSequence<ClaimPlayRequestCommand>()
    .ToSequence<PrepareMatchDataCommand>()
    .ToSequence<DispatchSignalCommand<string>>(_signals.Outgoing.SwitchCamera, "Match")
    .ToSequence<DispatchSignalCommand>(_signals.Outgoing.HideNavBar)
    .ToSequence<BakeNavmeshCommand>()
    .ToSequence<DispatchSignalCommand>(_signals.Outgoing.LoadGameScene);
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
            FlowLogger.LogError(FlowLogType.MainScreenModule,
                "OpenMainScreenCommand - the screen did not open.");
            Stop();
            return;
        }

        screen.ShowPlayButton(true);
        Release();
    }
    catch (Exception exception)
    {
        FlowLogger.LogError(FlowLogType.MainScreenModule,
            $"OpenMainScreenCommand threw: {exception}");
        Stop();
    }
}
```

What the `catch` does is the game's decision and not the framework's - `Stop()`, a `Release()` that
carries on regardless, a signal that opens something else - which is why no base class writes it for
you. What is **not** a decision is that the retain is resolved on all three paths.

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

- **`DispatchSignalCommand`** and its generic arities - a step that dispatches a signal with a
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
- **A Command written only to dispatch a signal.** Bind `DispatchSignalCommand` instead.
- **A Function where a Command belonged.** If it is a step somebody should read in the sequence, it
  is a Command - a Function does not appear in the Flow Console.
- **Deriving from `FunctionBody`.** The constructor is internal; take one of the shipped arities.
- **A decision left in a Context, a View or a Mediator.** Each of those is somewhere a decision tries
  to settle, and the answer is the same every time: dispatch, and let a Command read the conditions
  and decide.
- **A command that runs every frame burying the console.** `[HideCommandLog]` drops that command's
  two log lines and nothing else.
