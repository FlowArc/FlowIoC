# FlowIoC

[![openupm](https://img.shields.io/npm/v/com.flowarc.flowioc.core?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.flowarc.flowioc.core/)

**A signal-driven IoC container and modular MVC framework for the Unity Engine.**

FlowIoC splits a game into self-contained **modules**. Each module owns its data
(Models), its logic (Commands and Functions), its presentation (Views and
Mediators), and a public **Signal** surface. Modules never reference each other's
internals — they are wired together declaratively by **Connectors**. The result is
a codebase where a feature can be added, tested in isolation, or deleted without
touching the rest of the game.

---

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [FlowIoC at a Glance](#flowioc-at-a-glance)
- [Core Concepts](#core-concepts)
- [Application Lifecycle](#application-lifecycle)
- [Quick Start](#quick-start)
- [Signals](#signals)
- [Commands](#commands)
- [Injection](#injection)
- [Views and Mediators](#views-and-mediators)
- [Connectors](#connectors)
- [Functions](#functions)
- [Bundled Modules](#bundled-modules)
- [Editor Tools](#editor-tools)
- [AI Agent Rules](#ai-agent-rules)
- [Module Layout Convention](#module-layout-convention)
- [Data Types](#data-types)
- [Documentation Index](#documentation-index)
- [License](#license)

---

## Requirements

| | |
|---|---|
| Package name | `com.flowarc.flowioc.core` |
| Minimum Unity | `6000.0` (declared in `package.json`) |
| Actively developed against | Unity 6 (`6000.3`) |
| Dependencies | `com.unity.addressables` 2.9.1+, `com.unity.render-pipelines.core` 17.0.0+ (resolved automatically) |
| Assemblies | `FlowIoC` (runtime), `FlowIoC.Editor` (editor) |

---

## Installation

### From OpenUPM (recommended)

FlowIoC is published on
[OpenUPM](https://openupm.com/packages/com.flowarc.flowioc.core/). With the
[openupm-cli](https://github.com/openupm/openupm-cli) installed:

```bash
openupm add com.flowarc.flowioc.core
```

Or declare the scoped registry yourself in `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.flowarc"
      ]
    }
  ],
  "dependencies": {
    "com.flowarc.flowioc.core": "1.3.0"
  }
}
```

Either way the package shows up in the Package Manager under **My Registries**, together
with every release from 1.1.0 onwards, so an upgrade is a version number rather than a new
URL.

### From a Git URL

In the editor: **Window → Package Manager → + → Install package from git URL**, then enter:

```
https://github.com/FlowArc/FlowIoC.git#1.3.0
```

Or add it to `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.flowarc.flowioc.core": "https://github.com/FlowArc/FlowIoC.git#1.3.0"
  }
}
```

Always pin a tag. Without `#<tag>` Unity resolves the tip of `master` and then locks that
commit into `packages-lock.json`, so the package silently stops tracking new releases.
To upgrade, change the tag and let Unity re-resolve.

> **Upgrading from `com.flowioc.core`?** The package was renamed in 1.1.0. Do it with the
> Editor closed and delete `Library/` before reopening — see the migration note in
> [`CHANGELOG.md`](CHANGELOG.md).

### As a git submodule (for working on FlowIoC itself)

```bash
git submodule add https://github.com/FlowArc/FlowIoC.git Packages/FlowIoC
git submodule update --init
```

Any folder under `Packages/` containing a `package.json` is treated by Unity as an *embedded*
package: it is writable, so you can edit and commit the framework straight from the consuming
project, and it takes precedence over any registry or Git version of `com.flowarc.flowioc.core`.

Anyone cloning a project that uses the submodule must run `git submodule update --init`,
otherwise `Packages/FlowIoC` stays empty and the project will not compile.

---

## FlowIoC at a Glance

One module, and how a click becomes a state change and comes back out as an event
another module can act on.

```mermaid
flowchart LR
    ROOT["<b>ROOT</b><br/><i>MonoBehaviour in the scene</i>"]

    CONTEXT["<b>CONTEXT</b><br/><i>binds everything together</i><br/>InjectionBinder<br/>InjectionBinderCrossContext<br/>MediationBinder<br/>CommandBinder"]

    subgraph VIEWSIDE ["VIEW — presentation only"]
        direction TB
        V1["View"] <--> M1["Mediator"]
        V2["View"] <--> M2["Mediator"]
    end

    subgraph SIGNALS ["SIGNALS — the module's only public surface"]
        direction TB
        IN["Incoming"]
        OUT["Outgoing"]
    end

    subgraph CONTROLLER ["CONTROLLER — logic, one step at a time"]
        direction TB
        CMD1["Command"]
        CMD2["Command"]
        CMD3["Command"]
    end

    FUNCTIONS["<b>FUNCTIONS</b><br/><i>injectable methods<br/>that return a value</i>"]
    MODELS["<b>MODELS</b><br/><i>state, held in<br/>value objects (VO)</i>"]
    SERVICES["<b>SERVICES</b><br/><i>talk to the world<br/>outside the module</i>"]
    WORLD["<b>OUTSIDE WORLD</b><br/><i>backend, device,<br/>third-party SDKs</i>"]
    CONNECTOR["<b>CONNECTOR</b><br/><i>the only place two<br/>modules meet</i>"]
    OTHER["<b>OTHER MODULES</b>"]

    ROOT -- 1 --> CONTEXT
    CONTEXT -- 2 --> SIGNALS
    M1 -- 3 --> IN
    CONNECTOR -- 3 --> IN
    IN -- 4 --> CONTROLLER
    CONTROLLER -- 5 --> MODELS
    CONTROLLER -- 5 --> SERVICES
    CONTROLLER -- 5 --> FUNCTIONS
    FUNCTIONS -- 5 --> MODELS
    SERVICES <--> WORLD
    CONTROLLER -- 6 --> OUT
    OUT -- 7 --> CONNECTOR
    CONNECTOR -- 7 --> OTHER
    OUT -- 8 --> M2
```

1. **Root** creates the Context and registers it with the `RootsManager`.
2. **Context** binds signals, models, mediations and commands, then every Root in
   the scene runs `Setup()` and `Launch()`.
3. A **Mediator** — or another module, through a **Connector** — dispatches an
   **Incoming** signal.
4. The **CommandBinder** runs the command chain bound to that signal, in sequence
   or in parallel.
5. **Commands** act on **Models** and **Services**, and call **Functions** when they
   need a value back.
6. A command dispatches an **Outgoing** signal to announce what happened.
7. A **Connector** carries that outgoing signal to another module's incoming
   signals. The two modules never reference each other.
8. **Mediators** listen to signals and drive their **Views**.

### Who does what

| | Responsible for | Must not |
|---|---|---|
| **Root** | Being the module's presence in the scene. Owns the Context, sets `initializeOrder`, hosts sub-contexts. | Contain game logic. A Root is normally an empty class. |
| **Context** | Declaring bindings — and nothing else. One `Bind` line per signal, model, mediation and command. | Run logic. If a Context has an `if`, that decision belongs in a Command. |
| **Signal** | Naming an event in the module's vocabulary, typed. `Incoming` is what the module accepts, `Outgoing` is what it announces. | Carry behaviour. A signal is a name and a payload, never a method call in disguise. |
| **Command** | One unit of work triggered by a signal. Injects models and services, mutates state, dispatches outgoing signals. | Hold state between runs, touch another module's model, or return a value — use a Function for that. |
| **Function** | An injectable method you call directly and get an answer from: a calculation, a lookup, a raycast. | Mutate state that a Command should own, or replace a Command in a flow you want visible in the console. |
| **Model** | State, and the rules that keep that state valid. Injected wherever it is needed. May dispatch an outgoing signal to announce that a value it holds has changed. | Know about Views, Commands, or any other module. Subscribe to a signal — an incoming signal runs a Command, and the Command calls the Model. |
| **Service** | A self-contained unit of work that answers the input it is given — a countdown, a parser, a storage wrapper. Reusable in any project, and the one thing another module may reference directly: add its assembly, inject its interface. | Depend on anything outside itself, or wait on another module's signal. A Service more than one module needs gets its own module. |
| **System** | The game-specific work a module owns. May lean on other Systems and Services: waiting on a signal they raise, or working from data they share. | Appear in another module's assembly. Two Systems in separate modules meet through a Connector, never through a reference. |
| **View** | Scene references and raw input. Exposes fields and callbacks. | Contain logic, or reach for a model. A View that has an `if` about game rules is doing the Mediator's job. |
| **Mediator** | Driving one View: subscribes to signals in `OnRegister`, unsubscribes in `OnRemove`, and turns view callbacks into outgoing signals. | Do the work itself. A Mediator dispatches; a Command decides. |
| **Connector** | Wiring one module's `Outgoing` signals to another's `Incoming` signals, in one readable place. | Transform game state. A converter that reshapes a payload is fine; a rule is not. |

A module that exists to provide a Service is named for what it does, while its Root and Context
keep the `Service` suffix — `CounterModule` holds `Modules.Counter`, and inside it sit
`CounterServiceRoot` and `CounterServiceContext` beside `ICounterService`. The suffix is what the
inspector reads: a Root takes the colour of whatever it roots and decides that from its own name,
so `CounterRoot` would be drawn as a plain Root while `CounterServiceRoot` is drawn as a Service.
The module name has no such job, so it says what the module counts, parses or stores.

---

## Core Concepts

| Concept | Base type | Responsibility |
|---|---|---|
| **Root** | `Root<TContext>` | The `MonoBehaviour` that lives in the scene and owns a Context. Drives the whole lifecycle. |
| **Context** | `Context` | Declares every binding of a module: signals, injections, mediations, commands. |
| **Signal** | `Signal`, `Signal<T1..T4>` | A typed event. The only thing a module exposes to the outside world. |
| **Command** | `Command`, `Command<T1..T4>` | A unit of logic triggered by a Signal. Sequential or parallel, retainable. |
| **Function** | `FunctionReturn<…>`, `FunctionVoid<…>`, `AsyncFunction` | An injectable method you call directly, with a return value if you need one. |
| **Model** | any class, usually `IXModel` / `XModel` | State. Injected wherever it is needed. |
| **View / Mediator** | `IView` + `ViewInjector`, `IMediator` | A `MonoBehaviour` in the scene and the injected class that drives it. |
| **Connector** | `SignalConnector` | Wires one module's outgoing Signals to another module's incoming Signals. |

The two binders you will use constantly:

| Binder | Scope | Use for |
|---|---|---|
| `InjectionBinder` | This Context only | Internal models, internal signals |
| `InjectionBinderCrossContext` | The whole application | Public signals, shared models and services |

---

## Application Lifecycle

A Root drives its Context through a fixed order. Sub-contexts run through the same
binding phases immediately after their parent.

```mermaid
flowchart TD
    A["Root.Awake()"] --> B["CreateContext()<br/>InitializeSubContexts()"]
    B --> C["RootsManager.Register()"]
    C --> D["Root.Start()<br/>RootsManager.StartContexts()"]

    subgraph P ["per Root, ordered by initializeOrder"]
        E["Context.Start()<br/>CoreBindings()"] --> F["SignalBindings()"]
        F --> G["InjectionBindings()"]
        G --> H["MediationBindings()"]
        H --> I["CommandBindings()"]
        I --> J["InjectAllInstances()<br/>ExecutePostConstructMethods()"]
    end

    D --> E
    J --> K["── end of frame ──"]
    K --> L["Setup() on every Root"]
    L --> M["Launch() on every Root"]
```

The barrier matters: `Setup()` does not run until **every** Root in the scene has
finished binding, and `Launch()` does not run until every Root has finished
`Setup()`. So `Setup()` is the only safe place to reach across modules — which is
exactly what Connector contexts do — and `Launch()` is where you dispatch the first
signal.

That gives each phase a job. The binding phases **declare**: they say what the module
is made of and decide nothing. `Setup()` **initialises**: everything in the scene is
bound by then, so a module readies its Models here if they need readying, and a
Connector wires two modules together. `Launch()` **starts**: it dispatches the module's
first signal, and the entry point's `Launch()` is what sets the game going.

Roots are ordered among themselves by the `initializeOrder` field exposed in the
inspector. Each phase can also be toggled off per-Root (`AutoInitialize`,
`AutoBindInjections`, `AutoBindMediations`, `AutoSetup`, `AutoLaunch`) so a context
can be driven manually in a test scene.

### Ordering Roots

Initialize Order is not a free number. It falls into bands, and placing a Root means
picking the band it belongs to:

The whole range is `-100` to `100`. Nothing needs to sit outside it: `-100` is as early as a Root
can be and `100` is as late.

| Order | Who sits there | Why |
|---|---|---|
| -100 | A module that must finish before anything reads its data | `PostConstruct` runs during the binding pass, and each Root finishes its own before the next begins - so being first is what puts data in place before anything reads it. Restoring saved data is the case this exists for. |
| negative | Services | A Service depends on nothing else, so it comes up first and is ready for everyone. |
| 0 – 97 | The game's own modules and Systems | Gameplay, input, camera - whatever this game is made of. |
| 98 | `ConnectorRoot` | After every module it wires, so the scene reads as modules first and wiring after them. |
| 99 | `ScreenRoot` | The screen manager owns the screen prefabs, so it is up before the flow that opens the first screen. |
| 100 | `MainRoot` | The entry point. Its `Launch()` dispatches the first signal, last of all. |

The shipped Roots use `-99` for the screen service, `-2` for the pool service, `-1` for
the asset service, `0` for gameplay and input, `1` for the camera system. Inside a band
the exact number rarely matters - two modules that never touch can both sit at `0`.

`MainScene` is authored in the same order, with separators between the bands, so the
Hierarchy shows the boot order without opening an inspector:

```
MainScene
├── ScreenServiceRoot          -99
├── PoolServiceRoot             -2
├── ------------------------
├── GameplayRoot                 0
├── ------------------------
├── ConnectorRoot               98
├── ScreenRoot                  99
└── MainRoot                   100
```

The order decides who binds first and who is called first inside the `Setup()` and
`Launch()` passes. It is not what makes reaching across modules safe - the barrier above
is. `ConnectorRoot` at `98` is about reading order: it comes after every module it wires,
and any other number in the band would work just as well.

A GameObject the module needs in the scene goes under its Root. The Root is the module's one
presence there, so an EventSystem, an adapter, anything the module owns hangs off it rather
than sitting loose beside it.

A Root otherwise lives and dies with its scene. A module whose work outlives one — input,
audio, analytics — makes its Root persistent in `BeforeCreateContext`, which runs just before
the context is built:

```csharp
protected override void BeforeCreateContext()
{
    transform.SetParent(null);
    DontDestroyOnLoad(gameObject);
}
```

The reparenting is not decoration: Unity marks only root level objects as do not destroy, so a
Root authored under something else has to detach itself before it can survive.

---

## Quick Start

We will build a small `PlayerModule` that holds currency and reacts to a signal.

### 1. Generate the module

> **Tools ▸ FlowIoC ▸ Create Module**

The generator lays out the folder structure, writes the assembly definition, and
creates the `Root` / `Context` pair. The rest of this section shows what goes
inside.

A main module that gets a Root also picks a **Role**, which names that pair for
what the Root roots — the inspector reads the Root's name to colour it. **System**
writes `PlayerSystemRoot` and `PlayerSystemContext` and is what the dropdown starts
on, **Service** writes `CounterServiceRoot` and `CounterServiceContext`, and **Core**
writes the plain `PlayerRoot` and `PlayerContext`. The module folder, its assembly
and its namespaces are the same either way; the examples below use the plain names.

### 2. Declare the signals

Split the surface into what the module *listens to* and what it *announces*.
Everything the outside world touches goes here; nothing else is public.

```csharp
using FlowIoC.BaseModule.Signals;

namespace Modules.PlayerModule.Signals
{
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
}
```

### 3. Write the model

```csharp
namespace Modules.PlayerModule.Models
{
    public interface IPlayerModel
    {
        double Currency { get; }
        void AddCurrency(double amount);
    }

    public class PlayerModel : IPlayerModel
    {
        public double Currency { get; private set; }

        public void AddCurrency(double amount) => Currency += amount;
    }
}
```

### 4. Write a command

Note that FlowIoC injects **properties**, not fields. `[Inject]`, `[InjectSignal]`
and `[SignalParam]` all target properties — a plain field is silently skipped.

```csharp
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PlayerModule.Models;
using Modules.PlayerModule.Signals;

namespace Modules.PlayerModule.Controllers
{
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
}
```

### 5. Bind everything in the Context

```csharp
using FlowIoC.BaseModule.Contexts;
using Modules.PlayerModule.Controllers;
using Modules.PlayerModule.Models;
using Modules.PlayerModule.Signals;

namespace Modules.PlayerModule.RootsContexts
{
    public class PlayerContext : Context
    {
        private PlayerSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            // Cross-context: other modules may connect to these signals.
            _signals = InjectionBinderCrossContext.Bind<PlayerSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinderCrossContext.Bind<IPlayerModel, PlayerModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.InitializePlayer)
                .ToSequence<InitializePlayerCommand>();

            CommandBinder.Bind(_signals.Incoming.AddCurrency)
                .ToSequence<AddCurrencyCommand>()
                .ToSequence<SavePlayerCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            _signals.Incoming.InitializePlayer.Dispatch();
        }
    }
}
```

### 6. Add the Root to the scene

```csharp
using FlowIoC.BaseModule.Root;

namespace Modules.PlayerModule.RootsContexts
{
    public class PlayerRoot : Root<PlayerContext> { }
}
```

Drop `PlayerRoot` on a GameObject in your bootstrap scene. That is the whole
module — dispatching `_signals.Incoming.AddCurrency.Dispatch(100d)` from anywhere
now runs both commands in order.

---

## Signals

Signals come in five arities: `Signal`, `Signal<T1>`, `Signal<T1, T2>`,
`Signal<T1, T2, T3>`, `Signal<T1, T2, T3, T4>`.

```csharp
public Signal InitializePlayer = new();
public Signal<double> AddCurrency = new();
public Signal<string, int> SelectHero = new();
```

Every signal supports direct listeners as well as command bindings:

```csharp
_signals.Incoming.AddCurrency.AddListener(OnCurrencyAdded);
_signals.Incoming.AddCurrency.AddListenerOnce(OnFirstCurrencyOnly);
_signals.Incoming.AddCurrency.RemoveListener(OnCurrencyAdded);

_signals.Incoming.AddCurrency.Dispatch(100d);
```

A module keeps two holders, and where each one lives decides who can reach it.

`PlayerSignals`, with its `Incoming` and `Outgoing` nested classes, is the module's
public surface and lives in `Scripts/Shared/Signals/` — inside the module's Shared
assembly. A Connector references `Modules.Player.Shared` and never `Modules.Player`,
which is what keeps one module's assembly out of another's. Whatever a public signal
carries has to live in Shared too.

`PlayerInternalSignals` lives in `Scripts/Runtime/Signals/` and is what the module
says to its own commands. It is `internal`, so nothing outside the module's assembly
can dispatch it, and it has no `Incoming` or `Outgoing`: those two halves describe a
boundary, and an internal signal never crosses one.

```csharp
internal class PlayerInternalSignals : ISignalHolder
{
    public Signal Tick = new(hideCommandLog: true);
}
```

Every dispatch is logged to the Flow Console unless the signal was constructed
with `hideCommandLog: true`.

---

## Commands

A command is bound to a signal and executed when that signal is dispatched.

### Sequence and parallel

```csharp
CommandBinder.Bind(_signals.Incoming.AddCurrency)
    .ToSequence<AddCurrencyCommand>()
    .ToSequence<SavePlayerCommand>();

CommandBinder.Bind(_signals.Incoming.Refresh)
    .ToParallel<RefreshInventoryCommand>()
    .ToParallel<RefreshProfileCommand>();
```

`ToSequence` steps wait for the previous step; `ToParallel` steps all start at
once. The two can be mixed in a single binding.

### Retain and Release

A command that finishes asynchronously must hold the sequence open:

```csharp
public class ACommand : Command
{
    [Inject] public ICoroutineProvider _coroutineProvider { get; set; }

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
}
```

`Release(params object[])` may pass data forward — the next command in the
sequence receives it through its typed `Execute` overload:

```csharp
public class SavePlayerCommand : Command<IPlayerModel>
{
    public override void Execute(IPlayerModel playerModel) => playerModel.Save();
}
```

`Stop()` aborts the rest of the sequence.

### Reading signal parameters

Each `[SignalParam]` property is filled from the payload of the signal that
triggered the command.

```csharp
public Signal<CurrencyType, int> DecreaseCurrency = new();
```

```csharp
[SignalParam] private CurrencyType _type   { get; set; }
[SignalParam] private int          _amount { get; set; }
```

When a signal carries more than one value of the same type, write the index of the
one you want. The index counts within that property's type, so inserting a
parameter of some other type into the signal does not shift it.

```csharp
public Signal<string, int, int> Damage = new();   // Dispatch("sword", 12, 3)
```

```csharp
[SignalParam]    private string _weapon { get; set; }   // "sword"
[SignalParam(0)] private int    _amount { get; set; }   // 12
[SignalParam(1)] private int    _crit   { get; set; }   // 3
```

A property with no index takes the first value of its type that no other property
has claimed, so two same-typed properties also resolve correctly on their own:

```csharp
public Signal<int, int> Move = new();   // Dispatch(3, 7)
```

```csharp
[SignalParam] private int _x { get; set; }   // 3
[SignalParam] private int _y { get; set; }   // 7
```

### Command groups

`ToGroupAsSequence` / `ToGroupAsParallel` splice another signal's entire command
chain into the current one, so shared sub-flows are declared once:

```csharp
CommandBinder.Bind(_signals.Incoming.StartGroupTest)
    .ToSequence<ACommand>()
    .ToGroupAsSequence(_internalSignals.TriggerGroupA)
    .ToSequence<BCommand>();

CommandBinder.Bind(_internalSignals.TriggerGroupA)
    .ToSequence<GroupCommandA>();
```

Constructor-style parameters can be passed at bind time:

```csharp
CommandBinder.Bind(_signals.Incoming.StartJumpTest)
    .ToSequence<GCommand>(true, _internalSignals.TriggerGroupA,
                                _internalSignals.TriggerGroupB);
```

See [`Controller.md`](Runtime/BaseModule/Controller/Documentation/Controller.md)
for the full execution model.

---

## Injection

```csharp
// Bind a concrete type; the instance is created and returned.
_model = InjectionBinder.Bind<PlayerModel>();

// Bind an interface to an implementation.
InjectionBinder.Bind<IPlayerModel, PlayerModel>();

// Bind an existing object.
InjectionBinder.BindInstance<IClock>(new ServerClock());

// Bind a MonoBehaviour, created on the context GameObject.
InjectionBinderCrossContext.BindMonoBehaviorInstance<IInputProvider, InputProvider>();

// Named bindings, when one interface has several implementations.
InjectionBinder.Bind<IStorage, RemoteStorage>("remote");
```

Retrieve with `[Inject]` (or `[Inject("remote")]`) on a property, or imperatively
via `InjectionBinder.GetInstance<T>()`.

`InjectionBinderCrossContext` is shared by every context in the application — bind
there when another module has to see the object, and to `InjectionBinder` when it
must not.

Two providers are always available without binding anything:

```csharp
[Inject] private ICoroutineProvider _coroutineProvider { get; set; }
[Inject] private IUpdateProvider    _updateProvider    { get; set; }

_updateProvider.AddUpdate(Tick);        // also AddLateUpdate / AddFixedUpdate
```

---

## Views and Mediators

A View is the `MonoBehaviour` in the scene; it holds references and raises
callbacks, and contains no logic. A Mediator is a plain injected class that drives
it.

> **Tools ▸ FlowIoC ▸ Create View** generates both and places them in the right
> module folder.

```csharp
[RequireComponent(typeof(ViewInjector))]
public class ConnectionFailView : MonoBehaviour, IView
{
    public bool IsRegistered { get; set; }

    public Action RetryConnection { get; set; }
    public Button RetryButton;

    private void Start() => RetryButton.onClick.AddListener(() => RetryConnection?.Invoke());
}
```

```csharp
public class ConnectionFailMediator : IMediator
{
    [Inject]       private ConnectionFailView _view    { get; set; }
    [InjectSignal] private ConnectionSignals  _signals { get; set; }

    public void OnRegister()
    {
        _view.RetryConnection += Retry;
    }

    public void OnRemove()
    {
        _view.RetryConnection -= Retry;
    }

    private void Retry() => _signals.Outgoing.RetryConnection.Dispatch();
}
```

Bind the pair in the context:

```csharp
public override void MediationBindings()
{
    base.MediationBindings();

    MediationBinder.Bind<ConnectionFailView>().To<ConnectionFailMediator>();
}
```

`Start` is fine for a View that lives and dies with its GameObject. A `ScreenView` is
pooled — hiding it deactivates the object and reopening it shows the same instance — so
wire its buttons in `OnEnable` and drop them in `OnDisable` instead. See
[Screens](Runtime/ScreenModule/Documentation/ScreenModule.md).

The `ViewInjector` component lists every `IView` on the GameObject and resolves
which Context each one belongs to. Each entry says so with **Context Source** —
`Bubble Up` walks the hierarchy to the first Root above the View and is the
default, `Selected Root` names a Root in the scene, and `Root Name` names one by
its GameObject name and looks it up at startup. Registration happens as soon as
that Context is started, and `OnRemove` runs when the object is destroyed.

A prefab cannot hold a reference to a Root in the scene, so a prefab that has to
reach a Root outside its own hierarchy uses `Root Name`. A screen answers none of
the three: the screen service names the owning Context on the injector itself,
which outranks whatever the entry says.

Clear **Auto Register** for a view in the ViewInjector list to take over yourself:

```csharp
using FlowIoC.BaseModule.ViewsMediators.Utils;

_view.Register();
_view.UnRegister();
```

---

## Connectors

Connectors are what keep modules independent. A module dispatches its `Outgoing`
signals without knowing who listens; a Connector context joins those to other
modules' `Incoming` signals.

A Connector **gets** the signal holders, it never binds them. Each module binds its own
holder during its binding phase, and by the time any `Setup()` runs every one of them
exists — so the Connector asks for what is there with `GetInstance` instead of `Bind`.
`Bind` would hand it a holder of its own the moment the owning module is missing from
the scene: nothing would fail, and nothing would ever arrive either.

```csharp
using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;

public class HeroConnectorSubContext : Context
{
    private HeroSignals                _heroSignals;
    private PlayerProfileSignals       _playerProfileSignals;
    private HeroSelectionScreenSignals _heroSelectionScreenSignals;

    public override void Setup()
    {
        Signals();
        IncomingSignals();
        OutGoingSignals();
    }

    private void Signals()
    {
        _heroSignals                = InjectionBinderCrossContext.GetInstance<HeroSignals>();
        _playerProfileSignals       = InjectionBinderCrossContext.GetInstance<PlayerProfileSignals>();
        _heroSelectionScreenSignals = InjectionBinderCrossContext.GetInstance<HeroSelectionScreenSignals>();
    }

    private void IncomingSignals()
    {
        _heroSelectionScreenSignals.Outgoing.PurchaseHero.Connect(_heroSignals.Incoming.PurchaseHero);
        _heroSelectionScreenSignals.Outgoing.SelectHero.Connect(_heroSignals.Incoming.SelectHero);
    }

    private void OutGoingSignals()
    {
        _heroSignals.Outgoing.DecreaseCurrency.Connect(_playerProfileSignals.Incoming.DecreaseCurrency);
    }
}
```

`Connect` also accepts a plain delegate, and can adapt between signals whose
parameter types differ:

```csharp
// Signal -> Action
_heroSignals.Outgoing.CurrencySpent.Connect(vo => Analytics.Log(vo));

// Signal<A> -> Signal<B> through a converter
_matchSignals.Outgoing.MatchEnded.Connect(_analyticsSignals.Incoming.LogEvent,
                                          summary => summary.ToAnalyticsEvent());
```

Every connection can carry a `groupId` so it can be torn down as a unit:

```csharp
private const string Group = nameof(HeroConnectorSubContext);

_heroSignals.Outgoing.DecreaseCurrency
    .Connect(_playerProfileSignals.Incoming.DecreaseCurrency, Group);

// ...
SignalConnector.DisconnectGroup(Group);
```

Connections registered without a group are removed with `signal.Disconnect()`.
`SignalConnector.DisconnectAll()` clears everything and runs automatically on
subsystem registration, so connections never leak between play sessions.

> **In production:** *HitNPoP* keeps a dedicated `ConnectorModule` whose root owns
> fifteen sub-contexts — one per domain (`HeroConnectorSubContext`,
> `MatchConnectorSubContext`, `ScreenConnectorSubContext`, …). Every cross-module
> edge in the game lives in that one module, so the wiring can be read top to
> bottom in a single folder.

---

## Functions

Functions are injectable methods. Unlike commands they return values and are called
directly instead of being dispatched.

```csharp
public class CalculateDamageFunction : FunctionReturn<double, string>
{
    [Inject] private IPlayerModel  _playerModel  { get; set; }
    [Inject] private IWeaponsModel _weaponsModel { get; set; }

    public override double Execute(string weaponId)
    {
        var config = _weaponsModel.GetConfigVO(weaponId);
        return config.baseDamage * _playerModel.GetDamageMultiplier();
    }
}
```

```csharp
[Inject] private IFunctionProvider _functionProvider { get; set; }

var damage = _functionProvider
    .Execute<CalculateDamageFunction>()
    .AddParams(weaponId)
    .SetReturn<double>();

_functionProvider.Execute<RefreshHudFunction>().SetVoid();
```

| Base type | Terminator | Shape |
|---|---|---|
| `FunctionReturn<TReturn>`, `FunctionReturn<TReturn, T1..T4>` | `.SetReturn<TReturn>()` | returns a value |
| `FunctionVoid`, `FunctionVoid<T1..T4>` | `.SetVoid()` | returns nothing |
| `AsyncFunction`, `AsyncFunction<T1>` | `.SetAsync()` | runs on a coroutine, reports back through `AddFunctionCompletedCallback` |

```csharp
_functionProvider
    .ExecuteAsync<LoadProfileFunction>()
    .AddFunctionCompletedCallback(OnProfileLoaded)
    .SetAsync();
```

---

## Bundled Modules

| Module | Entry point | What it does |
|---|---|---|
| **ScreenModule** | `IScreenService.Open<TScreen>().Show()` | UI screens and popups: layers, pooling, addressable loading, opening and closing animations. → [docs](Runtime/ScreenModule/Documentation/ScreenModule.md) |
| **PoolModule** | `IPoolService.Get<T>(key, parent)` | Config-driven object pooling with groups and prewarming. → [docs](Runtime/PoolModule/Documentation/PoolModule.md) |
| **AssetModule** | `IAssetService.LoadAssetAsync<T>(key, groupId)` | Load-once Addressables layer with group-scoped release. → [docs](Runtime/AssetModule/Documentation/AssetModule.md) |
| **ConsoleModule** | `FlowLogger.Log(FlowLogType.PlayerModule, …)` | A filterable in-editor console, wired into the framework itself. → [docs](Runtime/ConsoleModule/Documentation/FlowConsole.md) |
| **ExtensionModule** | `transform.position.WithY(0f)` | Extension methods that carry no framework of their own: vector and float maths, enum flags, list conversion and UTC time formatting. |

The framework logs its own activity on the built-in channels `Context`,
`Injection`, `Signal`, `Command`, `CommandOperation`, `Function`, `Screen`, `Pool`,
`Model` and `Asset`, each of which can be toggled in the Flow Console window — so
you can watch every signal dispatch and command step without adding a single log
line.

For your own logs, Flow Console auto-registers one channel per module and
regenerates `Assets/Plugins/FlowIoC/Generated/FlowLogType.cs` with a constant for each:

```csharp
using FlowIoC.ConsoleModule;

FlowLogger.Log(FlowLogType.PlayerModule, $"{nameof(Execute)} - {nameof(AddCurrencyCommand)}");
FlowLogger.LogError(FlowLogType.PlayerModule, "Currency went negative.");
```

Logging is compiled out unless the `ENABLE_LOG` scripting define is set.

---

## Ready-Made Modules

The bundled modules above are part of the framework and are always there. A ready-made module is
different: it is an ordinary FlowIoC module that FlowIoC happens to ship, and installing it copies
it into your own `Assets/Modules/` folder, where it is yours — read it, change it, delete the half
you do not want.

| Module | Injected as | What it does |
|---|---|---|
| **CounterModule** | `ICounterService` | Named counters with once-a-second callbacks: `CountDownFrom` towards zero or `CountUpFrom` measuring elapsed time, seconds left or 0..1, several listeners per id, and a pluggable time source so a server clock can replace the device one. |

Install one from **Tools > FlowIoC > Help > Modules**: pick the module and press **Install** on its
page. Copying the files is only part of it — the installer also registers the module in the module
index, gives it its `FlowLogType` channel, and writes the `.csproj.DotSettings` its namespaces
need, which is exactly what copying the folder by hand would miss.

A module already in `Assets/Modules/` is never overwritten. The copy in your project is the one you
have been editing, so the button reads *Installed* and does nothing; delete the folder first if you
want the shipped version back.

The payload lives in `Modules~/` inside the package. Unity does not import a folder whose name ends
in a tilde, so the modules carry their own asmdefs without compiling until they are installed.

A ready-made module adds no branch to the **Tools > FlowIoC** menu. Everything it offers arrives
with it, its test scene included: open the scene under the test module's `Scenes` folder and press
Play. The scene is an ordinary asset in the payload, and the `.meta` of every script it references
ships beside it, so the GUIDs resolve in your project exactly as they do in ours.

---

## Editor Tools

| Menu | Purpose |
|---|---|
| `Tools/FlowIoC/Create Module` | Scaffold a module: folders, assembly definition, Root and Context |
| `Tools/FlowIoC/Create Command` | Generate a command |
| `Tools/FlowIoC/Create Model` | Generate an `IXModel` / `XModel` pair |
| `Tools/FlowIoC/Create View` | Generate a View, a Mediator, and the prefab |
| `Tools/FlowIoC/Add Shared Data` | Give an existing module a `Scripts/Shared` assembly and wire the references to it |
| `Tools/FlowIoC/Delete Module` | Remove a module and its references |
| `Tools/FlowIoC/Flow Console` | The filterable runtime log window |
| `Tools/FlowIoC/Model Viewer` | Inspect live model state at runtime |
| `Tools/FlowIoC/Folder Painter` | Colour Project window folders by path or by folder |
| `Tools/FlowIoC/Screen Scanner` | Every screen context on a Root in the open scenes, with its manager, layer, tag and animation flags editable in place |
| `Tools/FlowIoC/Module Scanner` | Report every module's folders, assemblies, references and namespace settings, and repair what is safe to repair |
| `Tools/FlowIoC/AI/Agent Rules` | Write FlowIoC's architecture rules into the project's `AGENTS.md` |
| `Tools/FlowIoC/AI/Agent Skills` | Install the skills FlowIoC ships into the project's `.claude/skills` |
| `Tools/FlowIoC/Help` | An introduction to the architecture, one topic at a time, inside the Editor. Its Welcome page has a **What's New** tab, read out of the package's `CHANGELOG.md`, and the window opens itself there once after FlowIoC has been updated |

`Module Scanner` also writes `<Solution>.sln.DotSettings`, the ReSharper and Rider
code style FlowIoC ships: naming rules, the `_` prefix on private members, the `VO` suffix
family, spacing. Rider only reads a settings file named after the solution, which differs per
project, so the file is generated rather than shipped. Only the keys FlowIoC owns are written -
anything else in the file survives - and the result belongs in version control, unlike the
`.sln.DotSettings.user` file beside it.

Attributes that affect the editor: `[CustomClassHeader]` colors a Root or Context
header, `[ShowInModelViewer]` / `[HideInModelViewer]` control Model Viewer output,
`[ExcludeFromContextWindow]` hides a context from the sub-context picker, and
`[ReadOnly]` locks an inspector field.

See [`Editor/README.md`](Editor/README.md) and
[`CodeGenerator/Documentation.md`](Editor/CodeGenerator/Documentation.md).

---

## AI Agent Rules

FlowIoC imposes an architecture that nothing in the C# type system enforces, so an
AI coding assistant that has not been told the rules will happily write code that
compiles and violates every one of them — logic in a Context, one module injecting
another's model, `[Inject]` on a field where it is silently skipped.

> **Tools ▸ FlowIoC ▸ AI ▸ Agent Rules**

The window writes those rules into your project's root `AGENTS.md` — the convention
Claude Code, Codex, Cursor, Zed and Gemini CLI all read — and points `CLAUDE.md` at
that file. The rules land inside a marked block:

```
<!-- FLOWIOC:BEGIN version=<installed> hash=<rule text> | ... -->
...
<!-- FLOWIOC:END -->
```

Nothing outside the markers is ever touched, so rules you wrote yourself are safe,
and a malformed marker makes the tool refuse to write rather than guess. FlowIoC
offers to install the block the first time you open a project and to refresh it when
the rules change; the offer is remembered, so declining it is permanent until the
rules themselves change. Removing FlowIoC through the Package Manager removes the
block with it.

The rule text ships in `Documentation~/AgentRules.md`.

### Agent Skills

The rules are what an assistant is told on every task, so they stay short. A skill is what it
reaches for when one particular kind of work comes up, and it can afford to be longer.

> **Tools ▸ FlowIoC ▸ AI ▸ Agent Skills**

You do not have to ask for them. FlowIoC writes each skill it ships into the project's
`.claude/skills` folder when the Editor opens — one folder per skill, logged to the console so
the folder is never a mystery — and refreshes one that the package has since changed. Only the
files the package owns are ever compared, written or deleted, so a skill you wrote yourself is
never touched. The window above is for seeing what is installed and for putting a deleted skill
back without waiting for the next Editor session.

Removing FlowIoC through the Package Manager takes the shipped skills with it, file by file:
nobody asked for them, so nobody is left with folders they cannot explain. A note left beside a
shipped skill survives that — the shipped file goes, the note stays, and the folder it lives in
stays with it.

A package removed some other way — `manifest.json` edited by hand, or the folder deleted —
raises no event for FlowIoC to act on. Every shipped skill opens by saying so: it applies only
while FlowIoC is installed, and names the check and the folder to delete if it is not.

| Skill | Covers |
|---|---|
| `flowioc-data-types` | The `CD_`, `RD_`, `PD_`, `ED_` and `DD_` prefixes, the `VO` suffix family that goes with them, and which folder each kind belongs in. |
| `flowioc-scaffolding` | Which menu item lays a module out and what to fill in, why the optional folders are the step that is easiest to get wrong, where the `.csproj.DotSettings` files land, and how to drive the generators from a terminal against an open Editor. |

The skills ship in `Documentation~/Skills/`.

---

## Module Layout Convention

`Create Module` produces this shape. Keeping it makes the generators and the
namespace tools work without configuration:

```
Modules/
└── PlayerModule/
    ├── Modules.Player.asmdef
    ├── Prefabs/
    ├── Resources/
    ├── Scenes/
    ├── Scripts/
    │   ├── Editor/
    │   ├── Runtime/
    │   │   ├── Constants/         # constant strings and keys
    │   │   ├── Controllers/       # commands
    │   │   ├── Data/
    │   │   │   ├── UnityObjects/  # ScriptableObjects (CD_, RD_, PD_, ED_, DD_)
    │   │   │   └── ValueObjects/  # plain data (…VO, …CVO, …RVO, …PVO)
    │   │   ├── Entities/          # MonoBehaviours owned by the module
    │   │   ├── Enums/
    │   │   ├── Functions/
    │   │   ├── Models/
    │   │   ├── RootsContexts/     # PlayerRoot, PlayerContext, sub-contexts
    │   │   ├── Services/          # self-contained, reusable in any project
    │   │   ├── Signals/           # PlayerInternalSignals — the module's own traffic
    │   │   ├── Systems/           # this game's own logic
    │   │   └── ViewsMediators/
    │   └── Shared/                # Modules.Player.Shared.asmdef — ticked by default
    │       ├── Constants/
    │       ├── Data/
    │       │   ├── UnityObjects/
    │       │   └── ValueObjects/
    │       ├── Enums/
    │       └── Signals/           # PlayerSignals — the module's public surface
    ├── zScreenModules/
    ├── zSubModules/
    └── zTestModules/
```

### Publishing data through `Shared`

`Scripts/Shared/` is an assembly of its own — `Modules.Player.Shared`, beside
`Modules.Player` — and it is how a module hands data to another module without
handing over its logic. Only data belongs there: value objects, the
ScriptableObjects built out of them, and the enums and constants those need.

Whoever reads that data references `Modules.Player.Shared`, never `Modules.Player`.
A `PlayerScreenModule` can read `CD_PlayerRules` and still has no way to reach
`PlayerModel` or `AddCurrencyCommand`. Tick **Shared** when creating a main module
and `Create Module` writes the reference for you — into the module's own assembly,
and into every screen, sub and test module created under it afterwards.

The parent references its own Shared assembly as well: the asmdef inside
`Scripts/Shared/` carves that folder out of `Modules.Player`, so the reference is
what lets the module read the data it publishes.

Namespaces follow the folder, as they already do for a module: a value object under
`Scripts/Shared/Data/ValueObjects/` is in
`Modules.PlayerModule.Shared.Data.ValueObjects`, so it cannot collide with the
Runtime type of the same name in `Modules.PlayerModule.Data.ValueObjects`. The
generator writes `Modules.Player.Shared.csproj.DotSettings` alongside the module's
own — a `.csproj.DotSettings` applies only to the project it is named after, so the
module's file cannot skip the `Scripts` folder on the Shared assembly's behalf.
*Tools ▸ FlowIoC ▸ Module Scanner* rewrites both.

Shared is offered on main modules only. If two modules need the same data and
neither owns it, that data belongs in a module of its own — the same answer as for
a Service more than one module needs.

The generator creates the module folder, the assembly definition, the managed
folders (`Controllers`, `Models`, `RootsContexts`, `Services`, `Systems`,
`ViewsMediators`, `UnityObjects`, `ValueObjects`,
`Editor`, `Resources`, `Prefabs`, `Scenes`, and the three `z` folders) and —
optionally — the `Root` / `Context` pair. Their names are not hard-coded; they come
from the module config and can be renamed under
the code generator settings asset. `Constants`, `Data`, `Entities`, `Enums`,
`Functions` and `Signals` are team convention rather than generator output — add
them as the module needs them.

Every module the generator creates is recorded in one project asset —
`Assets/Plugins/FlowIoC/Editor/CodeGenerator/ED_ModuleIndex.asset` — keyed on the
module folder's Unity GUID rather than its name or path, so renaming or moving a
module in the Project window does not desynchronise the tools from what is actually
on disk. The index is a cache: name, kind and nesting are all read back off the
folder tree, so a stale or missing entry is fixed by rebuilding it — *Tools ▸ FlowIoC ▸
Module Scanner*, or just reopening the project — rather
than by editing the asset.

Because it is a cache, FlowIoC keeps it out of version control: a `.gitignore` next to
the asset ignores it and its `.meta`. Two people adding modules on separate branches
would otherwise meet in the same serialized file, resolving by hand something the next
rebuild reproduces anyway. The rule sits in that folder rather than in your project's
root `.gitignore`, which is yours; your own lines in it are left alone, and everything
outside the `FLOWIOC:BEGIN`/`FLOWIOC:END` markers survives.

> **A project that already committed the index.** `.gitignore` does not untrack what git
> is already tracking. Untrack it once, keeping the file on disk:
> ```
> git rm --cached Assets/Plugins/FlowIoC/Editor/CodeGenerator/ED_ModuleIndex.asset*
> ```

> **`Systems` in a project that predates it.** The folder list lives in
> `Assets/Plugins/FlowIoC/Editor/CodeGenerator/ED_MainModuleDirectoryStructure.asset`, which
> is written once, in your project. Upgrading FlowIoC does not rewrite it, so a project
> created before `Systems` existed keeps its old list and the generator will not produce
> the folder. Add it in that asset's inspector, or delete the asset and let FlowIoC
> recreate it — deleting also discards any folder renames you made.

The three `z`-prefixed folders sort to the bottom and each holds a nested module
with its own Context:

- **`zSubModules/`** — a feature that belongs to this module but is large enough to
  deserve its own Context, attached to the parent Root as a sub-context.
- **`zScreenModules/`** — one folder per UI screen, so a screen's signals, commands
  and views travel together.
- **`zTestModules/`** — an isolated test scene and context, marked `IsTest` so it
  never starts in a real build.

A nested module may use the types of the module it sits in — a screen module reaching
its parent's System, for instance. The direction is one way: a module never knows what
its own `z` folders contain, so the parent's assembly never references theirs.

`zTestModules` is exempt from all of it. Everything there is test code, so it may
reference any module in the project; in exchange, every script in it is wrapped in
`#if UNITY_EDITOR` and never reaches a build.

Sub-contexts are attached from the Root's inspector (*Add Sub Context*), which
lists every `Context` type in the project. Mark a context with
`[ExcludeFromContextWindow]` to keep it out of that list.

---

## Data Types

A module keeps its data in two folders, and the name of a type says which kind of data it is
before you open it. `Data/UnityObjects/` holds the ScriptableObject assets; `Data/ValueObjects/`
holds the plain `[Serializable]` classes those assets are built out of.

The prefix on an asset says where its contents come from, and the value objects it carries take
the matching suffix:

| Prefix | What it holds | Filled by | Value objects inside |
|---|---|---|---|
| `CD_` | Config data. Constant: the same in every session, on every device. | Whoever authors the game, in the Editor. | `MapCVO` |
| `RD_` | Runtime data. Produced while the game runs, gone when it stops. | Play. | `MapRVO` |
| `PD_` | Player data. This one player's state: loaded at startup, written back to the save system whenever it changes. | Play, through the save system. | `MapPVO` |
| `ED_` | Editor data. Settings and caches only editor tooling reads. | Editor tools. | `MapEVO` |
| `DD_` | Database data. A copy of something a backend owns. | A download. | `MapDVO` |

So a level catalogue authored by hand is `CD_Maps` and its entries are `MapCVO`; that player's
progress through the same levels is `PD_Maps`, made of `MapPVO`. Reading the two names side by
side tells you which one is safe to regenerate and which one has to survive a restart.

```csharp
[CreateAssetMenu(fileName = "CD_Maps", menuName = "Game/Data/CD_Maps")]
internal class CD_Maps : ScriptableObject
{
    public List<MapCVO> Maps = new();
}

[Serializable]
public class MapCVO
{
    public string Id;
    public int    StarTarget;
}
```

A plain `VO` suffix is the right name when the data belongs to no one kind in particular - a
payload passed between commands, the shape a Function returns. And a value object that carries
two kinds at once is named after neither of them:

```csharp
[Serializable]
public class GameHexVO
{
    public GameHexCVO Config;   // what the level author placed
    public GameHexRVO Runtime;  // what play produced
}
```

Calling it `GameHexCVO` would be a lie about half its contents, so it is named for the hex. A
project that needs a family of its own adds one the same way: a new prefix, a matching suffix,
both declared in the code style.

Which prefixes and suffixes are legal is declared in `<Solution>.sln.DotSettings`, written by
*Tools ▸ FlowIoC ▸ Module Scanner*. What each one means is the
table above, and the agent rules carry a short version of it so an AI assistant names data the
same way.

---

## Documentation Index

One document per subsystem. Each is written the same way: what the piece is for, how
to use it, worked good-versus-bad scenarios, and the pitfalls that bite in practice.

| Area | Document |
|---|---|
| Roots, contexts, injection, signals, mediation | [Base Module](Runtime/BaseModule/Documentation/BaseModule.md) |
| Writing and chaining logic | [Commands](Runtime/BaseModule/Controller/Documentation/Controller.md) |
| UI screens and popups | [Screens](Runtime/ScreenModule/Documentation/ScreenModule.md) |
| Object pooling | [Pooling](Runtime/PoolModule/Documentation/PoolModule.md) |
| Addressables and asset groups | [Assets](Runtime/AssetModule/Documentation/AssetModule.md) |
| Runtime logging and diagnosis | [Flow Console](Runtime/ConsoleModule/Documentation/FlowConsole.md) |
| The FlowIoC editor menu | [Editor Tools](Editor/README.md) |
| Scaffolding modules and classes | [Code Generator](Editor/CodeGenerator/Documentation.md) |

---

## License

See [LICENSE](LICENSE.md).
