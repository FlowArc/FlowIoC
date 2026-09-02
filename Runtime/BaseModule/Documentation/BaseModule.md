# Base Module

The Base Module is the part of FlowIoC every other module is built on: the **Root**
that puts a module in the scene, the **Context** that declares its bindings, the
**injection** that wires objects together, and the **signals** that let modules talk
without knowing about each other.

This document is about using those pieces. Commands have their own document —
see [Commands](../Controller/Documentation/Controller.md).

- [The Shape of a Module](#the-shape-of-a-module)
- [Root](#root)
- [Context](#context)
- [The Lifecycle](#the-lifecycle)
- [Injection](#injection)
- [Signals](#signals)
- [Views and Mediators](#views-and-mediators)
- [Sub-Contexts](#sub-contexts)
- [Providers](#providers)
- [Tearing Down](#tearing-down)
- [Scenarios](#scenarios)
- [Pitfalls](#pitfalls)

---

## The Shape of a Module

A FlowIoC module is a folder of ordinary C# classes plus two small ones that make it
a module:

```csharp
// The scene presence. Almost always empty.
public class PlayerRoot : Root<PlayerContext> { }
```

```csharp
// The wiring. Declarations only, no logic.
public class PlayerContext : Context
{
    private PlayerSignals _signals;

    public override void SignalBindings()
    {
        base.SignalBindings();
        _signals = InjectionBinderCrossContext.Bind<PlayerSignals>();
    }

    public override void InjectionBindings()
    {
        base.InjectionBindings();
        InjectionBinderCrossContext.Bind<IPlayerModel, PlayerModel>();
    }

    public override void MediationBindings()
    {
        base.MediationBindings();
        MediationBinder.Bind<PlayerHudView>().To<PlayerHudMediator>();
    }

    public override void CommandBindings()
    {
        base.CommandBindings();
        CommandBinder.Bind(_signals.Incoming.AddCurrency).ToSequence<AddCurrencyCommand>();
    }

    public override void Launch()
    {
        base.Launch();
        _signals.Incoming.InitializePlayer.Dispatch();
    }
}
```

Everything else in the module — models, commands, functions, views, services — is
reached through one of those four `Bind` calls.

---

## Root

A Root is a `MonoBehaviour`. Drop it on a GameObject and the module exists; delete
the GameObject and the module is gone.

```csharp
public class MatchRoot : Root<MatchContext> { }
```

Every Root is destroyed with its scene, and every instance gets its own Context. A
module that has to outlive a scene load — audio, analytics, player profile — says so
on its own Root:

```csharp
public class AudioRoot : Root<AudioContext>
{
    protected override void BeforeCreateContext()
    {
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }
}
```

Keep that Root in a bootstrap scene rather than in a prefab that gets spawned:
nothing stands a second copy down for you, so two instances mean two Contexts.

The inspector exposes the switches that matter:

| Field | Effect |
|---|---|
| `initializeOrder` | Orders this Root against the other Roots in the scene. Lower runs first. |
| `AutoInitialize` | Off: the Context is created but never started. Call `StartContext(true)` yourself. |
| `AutoBindInjections` / `AutoBindMediations` | Off: skip that binding phase. Useful when a test provides its own doubles. |
| `AutoSetup` / `AutoLaunch` | Off: `Setup()` / `Launch()` do not run. Call `Setup(true)` / `Launch(true)` from a test. |
| `IsTest` | Marks the context as a test context. Sub-contexts carry their own flag. |

A Root should stay empty. If you find yourself adding fields to one, the state
belongs in a Model and the behaviour in a Command. The four `protected virtual`
hooks — `BeforeCreateContext`, `AfterCreateBeforeStartContext`,
`AfterBindingsBeforeInjections`, `AfterStarBeforeLaunchContext` — exist for
framework-level extensions, not for game logic.

---

## Context

A Context has five methods you override, and each has one job.

| Method | Bind here | Runs |
|---|---|---|
| `SignalBindings()` | signal holders | first |
| `InjectionBindings()` | models, services | second |
| `MediationBindings()` | view → mediator pairs | third |
| `CommandBindings()` | signal → command chains | fourth |
| `Setup()` | cross-module wiring | after **every** Root has finished binding |
| `Launch()` | the first dispatch | after every Root has finished `Setup()` |

Always call `base.<Method>()` first — the base implementations are empty today, but
skipping them is how a future framework change silently breaks your module.

The order is not arbitrary: `CommandBindings()` needs the signal fields that
`SignalBindings()` assigned, so a signal must be bound before it can be bound *to*.

Four binders are available on every Context:

| Binder | Scope |
|---|---|
| `InjectionBinder` | this Context only |
| `InjectionBinderCrossContext` | every Context in the application |
| `MediationBinder` | this Context only |
| `CommandBinder` | this Context only |

---

## The Lifecycle

```
Root.Awake()
  └─ creates the Context, initializes sub-contexts, registers with RootsManager

Root.Start()  →  RootsManager.StartContexts()
  └─ for each Root, ordered by initializeOrder:
       Context.Start()  →  CoreBindings()
       SignalBindings() → InjectionBindings() → MediationBindings() → CommandBindings()
       InjectAllInstances()
       ExecutePostConstructMethods()

  ──────── end of frame ────────

  Setup()  on every Root
  Launch() on every Root
```

Two properties of this order are worth internalising, because most cross-module bugs
come from ignoring them:

1. **Bindings of one Root are complete before injection runs for that Root.** So a
   model can inject another model bound in the same Context, even if it was bound
   later in the file.
2. **`Setup()` does not start until every Root in the scene has finished binding.**
   That is what makes Connectors possible: at `Setup()` time, every module's signals
   exist, whatever order the Roots happen to sit in the hierarchy.

`Launch()` is the same barrier one step later, which is why it is the right place to
dispatch the signal that starts the game.

---

## Injection

### Binding

```csharp
// Concrete type — the binder creates it and hands it back.
_playerModel = InjectionBinder.Bind<PlayerModel>();

// Interface to implementation — this is the usual form.
InjectionBinder.Bind<IPlayerModel, PlayerModel>();

// An object you already have.
InjectionBinder.BindInstance<IClock>(new ServerClock());

// A MonoBehaviour, added to the Context's GameObject.
InjectionBinderCrossContext.BindMonoBehaviorInstance<IInputProvider, InputProvider>();

// Named, when one interface has several implementations.
InjectionBinder.Bind<IStorage, LocalStorage>("local");
InjectionBinder.Bind<IStorage, RemoteStorage>("remote");
```

### Consuming

```csharp
[Inject]          private IPlayerModel  _playerModel { get; set; }
[Inject("remote")] private IStorage     _storage     { get; set; }
[InjectSignal]    private PlayerSignals _signals     { get; set; }
```

> **Properties, not fields.** `[Inject]`, `[InjectSignal]` and `[SignalParam]` are
> declared `AttributeTargets.Property`. A field carrying one of them compiles and is
> then skipped, leaving a null at runtime.

Imperative lookup is available where an attribute will not do:

```csharp
var model = InjectionBinder.GetInstance<IPlayerModel>();
var storage = InjectionBinder.GetInstance<IStorage>("remote");
```

### Choosing the scope

`InjectionBinderCrossContext` is one shared container for the whole application.
Bind there when another module has to see the object; bind to `InjectionBinder`
when it must not. Signal holders that appear in a Connector are always cross-context;
a `…SignalsInternal` holder never is.

### Reaching the Context's GameObject

`CoreBindings()` binds the Context's own GameObject under the Context type's name, so
a model that needs to parent something into the module can ask for it:

```csharp
public class CameraModel : ICameraModel
{
    [Inject(nameof(CameraContext))] private GameObject _root { get; set; }
}
```

### Running code after injection — `IConstructable`

A model often cannot finish initialising in its constructor, because its injected
dependencies are not there yet. Implement `IConstructable` and the framework calls
`PostConstruct()` once, after every injection for the context is complete.

```csharp
public class CameraModel : ICameraModel, IConstructable
{
    [Inject] private ICameraConfigProvider _configProvider { get; set; }

    public bool IsPostConstructed { get; set; }
    public bool IsDeConstructed   { get; set; }

    public void PostConstruct()
    {
        // _configProvider is guaranteed to be here.
        _blendSettings = _configProvider.LoadBlendSettings();
    }

    public void Deconstruct()
    {
        _blendSettings = null;
    }
}
```

`Deconstruct()` runs when the binding is removed. There is no `[PostConstruct]`
attribute — the interface is the mechanism.

---

## Signals

A signal is a typed event. It is the only thing a module exposes.

```csharp
public class PlayerSignals : ISignalHolder
{
    public PlayerSignalsIncoming Incoming = new();
    public PlayerSignalsOutgoing Outgoing = new();
}

public class PlayerSignalsIncoming
{
    public Signal InitializePlayer = new();
    public Signal<double> AddCurrency = new();
}

public class PlayerSignalsOutgoing
{
    public Signal<double> CurrencyChanged = new();
}
```

Five arities exist: `Signal` through `Signal<T1, T2, T3, T4>`.

```csharp
_signals.Incoming.AddCurrency.Dispatch(100d);

_signals.Outgoing.CurrencyChanged.AddListener(OnCurrencyChanged);
_signals.Outgoing.CurrencyChanged.AddListenerOnce(OnFirstChangeOnly);
_signals.Outgoing.CurrencyChanged.RemoveListener(OnCurrencyChanged);
```

### The Incoming / Outgoing convention

The framework does not require the split — `ISignalHolder` is an empty marker
interface. The convention is what makes a Connector readable, because every line in
one reads the same way:

```csharp
_shopSignals.Outgoing.PurchaseRequested.Connect(_playerSignals.Incoming.SpendCurrency);
```

Read it as: *this module announces X, that module accepts X*. Signals that must never
leave the module go in a separate `…SignalsInternal` holder bound with the plain
`InjectionBinder`, so no Connector can reach them.

### Connecting modules

Connections are made in `Setup()`, never in a binding phase — the other module's
signals may not exist yet.

```csharp
public class ShopConnectorSubContext : Context
{
    private const string Group = nameof(ShopConnectorSubContext);

    private ShopSignals   _shopSignals;
    private PlayerSignals _playerSignals;

    public override void Setup()
    {
        _shopSignals   = InjectionBinderCrossContext.GetInstance<ShopSignals>();
        _playerSignals = InjectionBinderCrossContext.GetInstance<PlayerSignals>();

        _shopSignals.Outgoing.PurchaseRequested
            .Connect(_playerSignals.Incoming.SpendCurrency, Group);
    }
}
```

`Connect` also takes a delegate, and can adapt between differently-shaped signals
through a converter:

```csharp
_matchSignals.Outgoing.MatchEnded
    .Connect(_analyticsSignals.Incoming.LogEvent, summary => summary.ToAnalyticsEvent());
```

`SignalConnector.DisconnectGroup(Group)` removes every connection made with that
group id. `DisconnectAll()` clears everything and runs automatically on subsystem
registration, so connections never survive into the next play session.

---

## Views and Mediators

A View is a `MonoBehaviour` that holds scene references and raises callbacks. A
Mediator is a plain injected class that drives it.

```csharp
[RequireComponent(typeof(ViewInjector))]
public class PlayerHudView : MonoBehaviour, IView
{
    public bool IsRegistered { get; set; }

    public Text CurrencyLabel;
    public Action SpendClicked;
}
```

```csharp
public class PlayerHudMediator : IMediator
{
    [Inject]       private PlayerHudView _view    { get; set; }
    [InjectSignal] private PlayerSignals _signals { get; set; }

    public void OnRegister()
    {
        _signals.Outgoing.CurrencyChanged.AddListener(Render);
        _view.SpendClicked += OnSpendClicked;
    }

    public void OnRemove()
    {
        _signals.Outgoing.CurrencyChanged.RemoveListener(Render);
        _view.SpendClicked -= OnSpendClicked;
    }

    private void Render(double amount) => _view.CurrencyLabel.text = $"{amount:N0}";
    private void OnSpendClicked() => _signals.Incoming.SpendCurrency.Dispatch();
}
```

```csharp
MediationBinder.Bind<PlayerHudView>().To<PlayerHudMediator>();
```

The `ViewInjector` component on the GameObject lists every `IView` on it and resolves
which Context each belongs to — by bubbling up the hierarchy, by an explicit Root
reference, or by Root name. Registration happens as soon as that Context is started,
and `OnRemove` runs when the object is destroyed.

Turn off **Auto Register** for a view in the ViewInjector list to control it yourself:

```csharp
using FlowIoC.BaseModule.ViewsMediators.Utils;

_view.Register();
_view.UnRegister();
```

Whatever you subscribe in `OnRegister`, unsubscribe in `OnRemove`. Mediators are
pooled and reused; a listener left behind is a leak that fires against a destroyed
view.

---

## Sub-Contexts

A sub-context is a full Context that runs inside another Root. It goes through the
same four binding phases immediately after its parent, and gets its own `Setup()` and
`Launch()`.

Attach one from the Root's inspector (*Add Sub Context*), which lists every `Context`
type in the project. Each entry carries its own `AutoSetup` and `IsTest` flags.

Use a sub-context when a feature is big enough to deserve its own bindings file but
does not need its own GameObject in the scene:

```
ShopModule/
├── Scripts/Runtime/RootsContexts/
│   ├── ShopRoot.cs
│   └── ShopContext.cs
└── zSubModules/
    └── DailyDealModule/Scripts/Runtime/RootsContexts/
        └── DailyDealSubContext.cs      ← attached to ShopRoot
```

Mark a Context with `[ExcludeFromContextWindow]` to keep it out of the picker.

---

## Providers

Two services are bound by `CoreBindings()` and are always injectable, with no
registration on your part:

```csharp
[Inject] private ICoroutineProvider _coroutineProvider { get; set; }
[Inject] private IUpdateProvider    _updateProvider    { get; set; }
```

`ICoroutineProvider` gives a non-`MonoBehaviour` class a place to run coroutines:

```csharp
_coroutineProvider.StartCoroutine(LoadRoutine());
_coroutineProvider.WaitForSeconds(2f, OnTimeout);
_coroutineProvider.WaitForEndOfFrame(OnFrameEnd);
_coroutineProvider.WaitUntil(() => _model.IsReady, OnReady);
```

`IUpdateProvider` gives it the Unity update loops:

```csharp
_updateProvider.AddUpdate(Tick);
_updateProvider.AddLateUpdate(LateTick);
_updateProvider.AddFixedUpdate(PhysicsTick);

_updateProvider.RemoveUpdate(Tick);   // always pair these
```

---

## Tearing Down

Destroying the Root's GameObject destroys the Context: mediations, commands and
injections are unbound, and `Deconstruct()` runs on every `IConstructable`.

`OnApplicationPause` routes to `Context.PauseContext()` and `ResumeContext()`, both
`virtual` and empty by default. Override them for anything that must stop when the
app goes to the background:

```csharp
public override void PauseContext()
{
    base.PauseContext();
    _signals.Incoming.SaveNow.Dispatch();
}
```

Individual bindings can be removed while the context lives:

```csharp
InjectionBinder.UnBind<ITemporaryService>();
InjectionBinder.UnBind<IStorage>("remote");
```

---

## Scenarios

### A Context declares; it does not decide

```csharp
// ✅ Every line is a binding. You can read the module's whole surface in one screen.
public override void CommandBindings()
{
    base.CommandBindings();

    CommandBinder.Bind(_signals.Incoming.StartMatch)
        .ToSequence<ValidateMatchCommand>()
        .ToSequence<BuildMatchCommand>();
}
```

```csharp
// ❌ A decision hidden in the wiring. Nothing logs it, no test can reach it, and the
//    two branches drift apart over time.
public override void CommandBindings()
{
    base.CommandBindings();

    if (_saveModel.IsFirstRun)
        CommandBinder.Bind(_signals.Incoming.StartMatch).ToSequence<TutorialMatchCommand>();
    else
        CommandBinder.Bind(_signals.Incoming.StartMatch).ToSequence<BuildMatchCommand>();
}
```

Bind one chain and let a command choose, so the decision appears in the console.

### Cross-context only for what is genuinely public

```csharp
// ✅ The public signal holder is shared; the internal one is not.
public override void SignalBindings()
{
    base.SignalBindings();

    _signals  = InjectionBinderCrossContext.Bind<MatchSignals>();
    _internal = InjectionBinder.Bind<MatchSignalsInternal>();
}
```

```csharp
// ❌ Everything cross-context. Any module can now dispatch this module's internal
//    steps, and nothing tells you who really depends on whom any more.
_signals  = InjectionBinderCrossContext.Bind<MatchSignals>();
_internal = InjectionBinderCrossContext.Bind<MatchSignalsInternal>();
```

### Wire modules in `Setup()`, not in a binding phase

```csharp
// ✅ At Setup() time every module has finished binding, whatever the Root order is - so the
//    Connector gets the holders that are there instead of binding ones of its own.
public override void Setup()
{
    _shopSignals   = InjectionBinderCrossContext.GetInstance<ShopSignals>();
    _playerSignals = InjectionBinderCrossContext.GetInstance<PlayerSignals>();

    _shopSignals.Outgoing.PurchaseRequested.Connect(_playerSignals.Incoming.SpendCurrency);
}
```

```csharp
// ❌ Works only while ShopRoot happens to initialize after PlayerRoot. Reordering the
//    hierarchy, or loading the shop from a different scene, breaks it silently.
public override void SignalBindings()
{
    base.SignalBindings();

    _shopSignals.Outgoing.PurchaseRequested.Connect(_playerSignals.Incoming.SpendCurrency);
}
```

### Initialise in `PostConstruct()`, not in the constructor

```csharp
// ✅ Injections are guaranteed to be present.
public class MatchModel : IMatchModel, IConstructable
{
    [Inject] private IRulesProvider _rules { get; set; }

    public bool IsPostConstructed { get; set; }
    public bool IsDeConstructed   { get; set; }

    public void PostConstruct() => _turnLimit = _rules.TurnLimit;
}
```

```csharp
// ❌ _rules is still null here — the binder injects after construction.
public class MatchModel : IMatchModel
{
    [Inject] private IRulesProvider _rules { get; set; }

    public MatchModel() => _turnLimit = _rules.TurnLimit;   // NullReferenceException
}
```

### One Root per module, and let `initializeOrder` do the ordering

```csharp
// ✅ Independent Roots, one module each.
public class AudioRoot : Root<AudioContext> { }
public class MatchRoot : Root<MatchContext> { }
```

```csharp
// ❌ One Root owning two unrelated modules. Neither can be tested, reused, or
//    unloaded on its own.
public class GameRoot : Root<EverythingContext> { }
```

### A Mediator wires; it does not decide

```csharp
// ✅ The mediator turns a view callback into a signal and renders what it is told.
public void OnRegister()
{
    _signals.Outgoing.CurrencyChanged.AddListener(Render);
    _view.SpendClicked += () => _signals.Incoming.SpendCurrency.Dispatch();
}
```

```csharp
// ❌ The spending rule now lives in the presentation layer, where no command chain
//    and no console line will ever show it.
private void OnSpendClicked()
{
    if (_playerModel.Currency < _price) { _view.ShowError(); return; }
    _playerModel.Spend(_price);
}
```

---

## Pitfalls

### Injected property is null

Three causes, in order of likelihood:

1. It is a **field**, not a property. `[Inject]` only applies to properties.
2. The type was never bound, or was bound in a different Context with the plain
   `InjectionBinder` while you inject from another Context.
3. You are reading it in a constructor. Move the work to `PostConstruct()` and
   implement `IConstructable`.

### A signal fires but nothing happens

Check whether the signal instance you dispatched is the one you bound. Two contexts
each calling `InjectionBinderCrossContext.Bind<FooSignals>()` get the *same*
instance — that is the point — but a context that used `InjectionBinder.Bind<FooSignals>()`
gets its own, and the two never meet.

The same goes for a Connector that reached for `Bind` instead of `GetInstance`. If the
module that owns the holder is not in the scene, `Bind` gives the Connector a holder of its
own and every connection it makes is to a signal nobody dispatches.

The Flow Console's `Signal` channel shows every dispatch. If the dispatch appears and
no command follows, the binding is the problem.

### `Setup()` runs but the other module's signals are empty

`Setup()` runs after all Roots have bound — but only Roots that were in the scene at
that moment. A module loaded later by an additive scene load gets its own
`StartContexts()` pass; wire it from its own Connector rather than assuming it was
present at boot.

### Listener still fires after the view is gone

A Mediator subscribed in `OnRegister` and did not unsubscribe in `OnRemove`.
Mediators are pooled, so the stale subscription comes back attached to the next view
that uses that mediator type.

### A sub-context never runs

Sub-contexts are attached in the Root's inspector, not in code. If you renamed or
moved the Context class, the stored `ContextFullName` no longer resolves and the
console logs `Context Type couldn't find!`. Re-add it from the inspector, or run
*Tools ▸ FlowIoC ▸ Module Scan*.

### A second copy of an app-wide module appears

Nothing deduplicates Roots. A Root that calls `DontDestroyOnLoad` and also sits in a
prefab that gets spawned will exist twice, and each copy builds its own Context and
registers with the `RootsManager` under the same name. Keep a Root that outlives its
scene in a bootstrap scene, and nowhere else.

---

## Related

- [README — FlowIoC at a Glance](../../../README.md#flowioc-at-a-glance) — how the
  pieces fit together
- [Commands](../Controller/Documentation/Controller.md) — writing and chaining logic
- [Flow Console](../../ConsoleModule/Documentation/FlowConsole.md) — watching the
  lifecycle at runtime
