# Commands

A command is one unit of work that runs because a signal was dispatched. Commands
are where a FlowIoC module's logic lives — they read and write Models, call
Services, and announce results by dispatching outgoing Signals.

This document is about *using* commands: what belongs in one, how to chain them,
and which mistakes the framework will let you make quietly.

- [What a Command Is](#what-a-command-is)
- [When to Use a Command](#when-to-use-a-command)
- [Writing a Command](#writing-a-command)
- [Binding Commands to Signals](#binding-commands-to-signals)
- [Command Groups](#command-groups)
- [Scenarios](#scenarios)
- [Silencing High-Frequency Chains](#silencing-high-frequency-chains)
- [Pitfalls](#pitfalls)

---

## What a Command Is

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

Three things are worth noticing:

- **It has no constructor and no caller.** You never write `new AddCurrencyCommand()`.
  The binder creates it, injects it, runs it, and puts it back in a pool.
- **Its inputs arrive by attribute.** `[Inject]` for dependencies, `[InjectSignal]`
  for the module's signal holder, `[SignalParam]` for the payload of the signal that
  triggered it.
- **It returns nothing.** A command reports by dispatching a signal, not by handing
  a value back to a caller.

> **Properties, not fields.** All three attributes are declared
> `AttributeTargets.Property`. A field with `[Inject]` on it compiles fine and is
> then silently skipped, leaving you with a null-reference at runtime. Always write
> `{ get; set; }`.

---

## When to Use a Command

Reach for a command when something *happens* and the game state has to change as a
result. If that is not what you are doing, one of these is a better fit:

| You want to… | Use | Why |
|---|---|---|
| React to an event and change state | **Command** | The flow shows up in the Flow Console, step by step |
| Compute or look something up and get an answer | **Function** | Commands cannot return values; a function can, and you call it directly |
| Keep a rule about your own data consistent | a **Model** method | A one-line state guard does not need its own signal |
| React to a view callback | **Mediator** → dispatch a signal → **Command** | The mediator wires, the command decides |
| Do the same work every frame | **Function** or a `IUpdateProvider` callback | A per-frame command chain floods the console and the pool |

The signal that triggers a command is not ceremony — it is the seam. Anything bound
to a signal can later be re-bound, re-ordered, silenced, or triggered from another
module through a Connector without touching the command itself.

---

## Writing a Command

### Reading the signal's payload

Each `[SignalParam]` property is filled from the payload of the signal that triggered
the command, matched by the property's type.

```csharp
// PlayerSignals.cs
public Signal<CurrencyType, int> DecreaseCurrency = new();
```

```csharp
public class DecreaseCurrencyCommand : Command
{
    [Inject] private IPlayerModel _playerModel { get; set; }

    [SignalParam] private CurrencyType _type   { get; set; }
    [SignalParam] private int          _amount { get; set; }

    public override void Execute() => _playerModel.Decrease(_type, _amount);
}
```

Signals that carry two values of the same type need an index. It counts within the
property's own type, not across the whole payload.

```csharp
// PlayerSignals.cs
public Signal<string, int, int> Damage = new();   // Dispatch("sword", 12, 3)
```

```csharp
public class ApplyDamageCommand : Command
{
    [SignalParam]    private string _weapon { get; set; }   // "sword"
    [SignalParam(0)] private int    _amount { get; set; }   // 12
    [SignalParam(1)] private int    _crit   { get; set; }   // 3

    public override void Execute() { }
}
```

Without an index a property takes the first value of its type that no other
property has claimed, so `[SignalParam] int _x` followed by `[SignalParam] int _y`
receives the first and second int. Use an explicit index when the declaration order
is not obvious from reading the class.

When a base class declares a `[SignalParam]` property and a derived class overrides
it, the property is read once and the base class's declaration decides the index. An
index written on the override is ignored, so put it on the declaration that owns the
attribute.

### Finishing later — `Retain()` and `Release()`

`Execute()` is expected to be finished when it returns. If your work continues past
that — a coroutine, a web call, an animation — call `Retain()` first, and `Release()`
when it is genuinely done. The chain waits in between.

```csharp
public class LoadProfileCommand : Command
{
    [Inject] private ICoroutineProvider _coroutineProvider { get; set; }
    [Inject] private IProfileService    _profileService    { get; set; }

    public override void Execute()
    {
        Retain();
        _coroutineProvider.StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        yield return _profileService.FetchRoutine();
        Release();
    }
}
```

### Handing data to the next command

`Release()` takes parameters. The next command in the sequence receives them through
its typed `Execute` overload:

```csharp
public class LoadConfigCommand : Command
{
    public override void Execute()
    {
        Retain();
        LoadConfig(config => Release(config.ServerUrl, config.ApiKey, config.Timeout));
    }
}

public class ConnectToServerCommand : Command<string, string, int>
{
    [Inject] private INetworkService _network { get; set; }

    public override void Execute(string url, string apiKey, int timeout)
        => _network.Connect(url, apiKey, timeout);
}
```

`Command` has five arities — `Command`, `Command<T1>`, up to `Command<T1, T2, T3, T4>`
— matching what the previous step released.

### Aborting the chain — `Stop()`

`Stop()` ends the group: the remaining steps do not run.

```csharp
public class ValidateSaveCommand : Command
{
    [Inject] private ISaveService _saveService { get; set; }

    public override void Execute()
    {
        Retain();

        if (!_saveService.HasWritableSlot)
        {
            FlowLogger.LogError(FlowLogType.PlayerModule, "No writable save slot.");
            Stop();
            return;
        }

        Release();
    }
}
```

> **`Release()` and `Stop()` only work on a retained command.** Both check
> `IsRetain` first, and if it is false they log
> `Command must be retained to call STOP!` and return without doing anything —
> the chain keeps going as if nothing happened. This is the single most common
> silent bug in FlowIoC command code. Call `Retain()` before either one, even on a
> path that fails immediately.

---

## Binding Commands to Signals

Bindings live in `Context.CommandBindings()`. One signal, one chain.

```csharp
public override void CommandBindings()
{
    base.CommandBindings();

    CommandBinder.Bind(_signals.Incoming.AddCurrency)
        .ToSequence<AddCurrencyCommand>()
        .ToSequence<SavePlayerCommand>();
}
```

### Sequence

`ToSequence` steps run one after another. Step *n+1* starts when step *n* returns —
or, if step *n* retained, when it releases.

```csharp
CommandBinder.Bind(_signals.Incoming.StartMatch)
    .ToSequence<ValidateMatchDataCommand>()
    .ToSequence<BuildBattlefieldCommand>()
    .ToSequence<SpawnUnitsCommand>();
```

Use it when each step depends on the one before it.

### Parallel

`ToParallel` steps all start together; the chain moves on when the last of them
finishes.

```csharp
CommandBinder.Bind(_signals.Incoming.LoadAssets)
    .ToSequence<ShowLoadingScreenCommand>()
    .ToParallel<LoadTexturesCommand>()
    .ToParallel<LoadAudioCommand>()
    .ToParallel<LoadModelsCommand>()
    .ToSequence<HideLoadingScreenCommand>();
```

Use it when the steps do not touch each other's results. Three parallel loads that
each take 400 ms cost 400 ms, not 1200 ms.

### Parameters fixed at bind time

Both terminators accept extra arguments, delivered to the command's typed `Execute`:

```csharp
CommandBinder.Bind(_signals.Incoming.StartTutorial)
    .ToSequence<BranchCommand>(true, _internalSignals.PathA, _internalSignals.PathB);
```

```csharp
public class BranchCommand : Command<bool, Signal, Signal>
{
    public override void Execute(bool condition, Signal onTrue, Signal onFalse)
    {
        Retain();
        (condition ? onTrue : onFalse).Dispatch();
        Release();
    }
}
```

This is how you keep a decision out of the Context: the Context declares *which*
signals are in play, the command decides *which one fires*.

---

## Command Groups

`ToGroupAsSequence` and `ToGroupAsParallel` splice another signal's whole chain into
this one. Declare a shared sub-flow once and reuse it.

```csharp
// The shared flow
CommandBinder.Bind(_internalSignals.RefreshInventory)
    .ToParallel<LoadOwnedItemsCommand>()
    .ToParallel<LoadEquippedItemsCommand>()
    .ToSequence<PublishInventoryCommand>();

// Two different flows that both need it
CommandBinder.Bind(_signals.Incoming.EnterShop)
    .ToSequence<OpenShopCommand>()
    .ToGroupAsSequence(_internalSignals.RefreshInventory)
    .ToSequence<HighlightNewItemsCommand>();

CommandBinder.Bind(_signals.Incoming.PurchaseCompleted)
    .ToSequence<GrantItemCommand>()
    .ToGroupAsSequence(_internalSignals.RefreshInventory);
```

`ToGroupAsSequence` waits for the whole sub-chain before continuing.
`ToGroupAsParallel` starts it alongside the other parallel steps.

A group may point at itself, which is how a tick loop is written:

```csharp
CommandBinder.Bind(_internalSignals.Tick)
    .ToSequence<AdvanceTimersCommand>()
    .ToSequence<PublishExpiredTimersCommand>()
    .ToGroupAsParallel(_internalSignals.Tick);
```

---

## Scenarios

### One job per command

```csharp
// ✅ Each command has a name you can read off the console and understand.
CommandBinder.Bind(_signals.Incoming.CompleteMatch)
    .ToSequence<PersistMatchResultCommand>()
    .ToSequence<GrantMatchRewardsCommand>()
    .ToSequence<PublishMatchSummaryCommand>();
```

```csharp
// ❌ One command doing three jobs. You cannot reorder, reuse, or skip any of them,
//    and the console tells you nothing about which part failed.
public class FinishMatchCommand : Command
{
    public override void Execute()
    {
        _matchModel.Persist();
        _rewardModel.Grant(_matchModel.Result);
        _signals.Outgoing.MatchSummary.Dispatch(_matchModel.Summary);
    }
}
```

### Sequence only where there is a dependency

```csharp
// ✅ Independent work runs at the same time.
CommandBinder.Bind(_signals.Incoming.Boot)
    .ToParallel<LoadRemoteConfigCommand>()
    .ToParallel<LoadLocalSaveCommand>()
    .ToParallel<WarmAudioPoolCommand>()
    .ToSequence<ShowMainMenuCommand>();
```

```csharp
// ❌ Three independent loads, serialized for no reason. The player waits three
//    times as long, and nothing in the code says why.
CommandBinder.Bind(_signals.Incoming.Boot)
    .ToSequence<LoadRemoteConfigCommand>()
    .ToSequence<LoadLocalSaveCommand>()
    .ToSequence<WarmAudioPoolCommand>()
    .ToSequence<ShowMainMenuCommand>();
```

### Every path out of a retained command ends in `Release()` or `Stop()`

```csharp
// ✅ Both branches resolve the retain.
public override void Execute()
{
    Retain();

    _saveService.SaveAsync(
        onSuccess: () => Release(),
        onFailure: error =>
        {
            FlowLogger.LogError(FlowLogType.PlayerModule, $"Save failed: {error}");
            Stop();
        });
}
```

```csharp
// ❌ The failure callback never resolves the retain. The chain hangs forever,
//    with no error and no timeout — the next step simply never runs.
public override void Execute()
{
    Retain();
    _saveService.SaveAsync(onSuccess: () => Release());
}
```

```csharp
// ❌ Stop() without Retain(). Logs "Command must be retained to call STOP!" and
//    then the chain continues anyway — the opposite of what this code intends.
public override void Execute()
{
    if (!_validator.IsValid(_userId))
    {
        Stop();
        return;
    }

    _userModel.Apply(_userId);
}
```

### Typed parameters, not an object bag

```csharp
// ✅ The compiler checks the shape, and the binder matches the arity for you.
public class CreateEntityCommand : Command<string, Vector3, int>
{
    public override void Execute(string entityId, Vector3 position, int health) { }
}
```

```csharp
// ❌ Casting at runtime. A reordered Dispatch call becomes an InvalidCastException
//    somewhere far away from the change that caused it.
public class CreateEntityCommand : Command
{
    [SignalParam] private object[] _params { get; set; }

    public override void Execute()
    {
        var entityId = (string) _params[0];
        var position = (Vector3) _params[1];
    }
}
```

### A command changes state; a function answers a question

```csharp
// ✅ The calculation is a function, called from the command that acts on the answer.
public class ApplyDamageCommand : Command
{
    [Inject] private IFunctionProvider _functionProvider { get; set; }
    [Inject] private IUnitsModel       _unitsModel       { get; set; }

    [SignalParam] private string _weaponId { get; set; }
    [SignalParam] private int    _targetId { get; set; }

    public override void Execute()
    {
        var damage = _functionProvider
            .Execute<CalculateDamageFunction>()
            .AddParams(_weaponId)
            .SetReturn<double>();

        _unitsModel.ApplyDamage(_targetId, damage);
    }
}
```

```csharp
// ❌ A command used as a calculator. It cannot return the number, so the result has
//    to be smuggled through a model field that nothing else owns — and every damage
//    calculation now costs a signal dispatch and a pool round-trip.
public class CalculateDamageCommand : Command
{
    public override void Execute() => _unitsModel.LastCalculatedDamage = /* ... */;
}
```

### Decisions belong in the command, not the mediator

```csharp
// ✅ The mediator translates a click into a signal. That is all it does.
public class ShopMediator : IMediator
{
    [Inject]       private ShopView    _view    { get; set; }
    [InjectSignal] private ShopSignals _signals { get; set; }

    public void OnRegister() => _view.BuyClicked += OnBuyClicked;
    public void OnRemove()   => _view.BuyClicked -= OnBuyClicked;

    private void OnBuyClicked(string itemId)
        => _signals.Outgoing.PurchaseRequested.Dispatch(itemId);
}
```

```csharp
// ❌ The purchase rule now lives in the presentation layer. It cannot be triggered
//    from a test, a cheat menu, or a deep link, and it does not appear in the
//    console as a command step.
private void OnBuyClicked(string itemId)
{
    if (_playerModel.Currency < _shopModel.PriceOf(itemId))
    {
        _view.ShowNotEnoughCurrency();
        return;
    }

    _playerModel.Spend(_shopModel.PriceOf(itemId));
    _inventoryModel.Add(itemId);
}
```

### Right-sized steps

```csharp
// ✅ Each step is a thing a designer would name.
CommandBinder.Bind(_signals.Incoming.LoadPlayer)
    .ToSequence<FetchPlayerDataCommand>()
    .ToSequence<ValidatePlayerDataCommand>()
    .ToSequence<ApplyPlayerDataCommand>();
```

```csharp
// ❌ Steps at the level of individual statements. Every one costs a pool entry and
//    a console line, and the chain is harder to read than the method it replaced.
CommandBinder.Bind(_signals.Incoming.LoadPlayer)
    .ToSequence<OpenFileCommand>()
    .ToSequence<ReadLineCommand>()
    .ToSequence<ParseFieldCommand>()
    .ToSequence<CloseFileCommand>();
```

### Stay inside the module

```csharp
// ✅ Announce what happened and let a Connector decide who cares.
public override void Execute()
{
    _heroModel.Select(_heroId);
    _signals.Outgoing.HeroSelected.Dispatch(_heroId);
}
```

```csharp
// ❌ Reaching into another module's model. HeroModule now cannot be compiled,
//    tested, or shipped without PlayerProfileModule.
public override void Execute()
{
    _heroModel.Select(_heroId);
    _playerProfileModel.SetLastUsedHero(_heroId);
}
```

---

## Silencing High-Frequency Chains

The framework logs every signal dispatch, command step, and pool return. For a tick
loop running many times per second that is noise. Two switches suppress *framework
lifecycle* logs only — your own `FlowLogger` calls inside a command body are never
affected.

| Switch | Scope | Silences |
|---|---|---|
| `[HideCommandLog]` on the command class | every instance of that command type | `[Command] Execute as ...`, `Command is returned to pool! - ...` |
| `new Signal(hideCommandLog: true)` | that one signal field | `Signal is dispatched: ...`, `InitializeGroupWithSignal`, `Command SubGroup is executed`, `CommandGroup is returned to pool!` |

They are complementary — a self-retriggering loop needs both to go fully quiet:

```csharp
internal class CounterInternalSignals : ISignalHolder
{
    public Signal Tick = new(hideCommandLog: true);
    public Signal<CounterRequestVO> AddCounter = new();   // still logged
}
```

```csharp
[HideCommandLog]
internal class AdvanceTimersCommand : Command { /* ... */ }

[HideCommandLog]
internal class PublishExpiredTimersCommand : Command { /* ... */ }
```

```csharp
CommandBinder.Bind(_internal.Tick)
    .ToSequence<AdvanceTimersCommand>()
    .ToSequence<PublishExpiredTimersCommand>()
    .ToGroupAsParallel(_internal.Tick);
```

Marking only the signal still leaves the per-command execute lines; marking only the
commands still leaves the dispatch lines.

To hide a whole project log channel instead, turn it off in `CD_FlowConsole` —
but that is a global switch, not a per-loop one.

---

## Pitfalls

### The command never runs

Check, in this order:

1. Is the binding inside `CommandBindings()`, and does it call `base.CommandBindings()`?
2. Was the signal bound before it was used? `SignalBindings()` runs before
   `CommandBindings()`, so the field must be assigned there.
3. Is the signal you dispatch the *same instance* you bound? Binding
   `_signals.Incoming.Foo` and dispatching a different holder's `Foo` is a common
   copy-paste result.
4. Is the Root's `AutoInitialize` still on in the inspector?

The Flow Console's `Signal` channel shows every dispatch. If the dispatch line
appears and no command line follows, the binding is the problem, not the signal.

### The chain hangs after a step

A command retained and never released. Look for an early `return`, an exception
thrown after `Retain()`, or a callback path that forgets to resolve. There is no
timeout — a hung group waits forever.

### `Command must be retained to call RELEASE!` / `... to call STOP!`

You called `Release()` or `Stop()` on a command that never called `Retain()`. The
call did nothing. Add the `Retain()`.

### State from the last run leaks into this one

Commands are pooled per type and reused. `Clean()` between runs resets only the
retain flag — **your own fields keep their values**. A command that accumulates into
a private list will still hold last run's contents the next time it executes.

```csharp
// ❌ _collected still holds the previous run's items.
public class CollectRewardsCommand : Command
{
    private readonly List<string> _collected = new();

    public override void Execute()
    {
        _collected.AddRange(_rewardModel.Pending);
        // ...
    }
}
```

Initialize local state at the top of `Execute()`, or better, keep no state on the
command at all — that is what Models are for.

### The parameters arrive wrong

`[SignalParam]` properties are filled from the signal's payload by type, and typed
`Execute(...)` overloads are matched against what the previous step released. If
either shape changes, update both ends.

Two `[SignalParam]` properties of the same type are distinguished by their index:
`[SignalParam(0)]` and `[SignalParam(1)]` take the first and second value of that
type. Properties with no index take the next unclaimed value of their type, in
declaration order. A property that cannot be bound — an index past the end, two
properties claiming the same value, or no unclaimed value left — logs an error
naming the command, the property and the reason.

### The steps run in an order you did not expect

Mixed `ToSequence` and `ToParallel` in one chain: parallel steps that sit between
two sequence steps start together and the chain resumes when the last of them
finishes. If you need a strict order, do not mix.

---

## Related

- [README — FlowIoC at a Glance](../../../../README.md#flowioc-at-a-glance) — how
  commands sit between signals, models and connectors
- [BaseModule](../../Documentation/BaseModule.md) — contexts, binders, lifecycle
- [Flow Console](../../../ConsoleModule/Documentation/FlowConsole.md) — reading the
  command channel while a chain runs
