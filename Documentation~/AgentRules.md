## FlowIoC

These rules apply only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, ignore this block and delete it
from `AGENTS.md`.

FlowIoC is a signal-driven IoC framework for Unity. A game is split into modules that own
their state, logic and presentation. Modules never reference one another; they are wired
together declaratively by Connectors. Nothing in the C# type system enforces any of this,
so follow the rules below deliberately.

**This block is what you need before you know which task you are on.** The detail for one kind of
work lives in the skill for it - see *Where the detail lives* at the end - and this block says only
what is true whatever you are about to do.

### Rules that are not negotiable

- A module never reaches into another module. No type from `Modules.A` appears in
  `Modules.B`, and the only crossing point is a Connector. The three exceptions that
  follow are the whole list.
- **A Service crosses directly.** Reference the Service module's assembly and inject its
  interface, the way `OpenMatchBoardScreenCommand` injects `ICounterService`. Being
  usable this way is the point of a Service.
- **A sub-module reaches the module it lives in.** A screen or sub module may use its
  parent's types. The direction is one way: a module never knows what sits in its own
  `zScreenModules` or `zSubModules`.
- **A test module reaches anything.** Everything under `zTestModules` is test code, so it
  may reference any module in the project. In exchange, every script in it is wrapped in
  `#if UNITY_EDITOR`.
- A module's tests are that test module, and nothing else. There is no assembly of unit tests
  under `Scripts/`: the shape `Create Module` writes has no `Tests` folder, and adding one gives
  the module a second assembly nothing else in the project has. What a module hands the reader
  is the scene its test module runs, the same way it ships everything else it offers.
- Systems are never added to one another's assemblies. Two Systems in separate modules
  talk through signals wired in a Connector, like any other cross-module traffic.
- A Connector gets signal holders, it never binds them:
  `InjectionBinderCrossContext.GetInstance<PlayerSignals>()` in `Setup`, never `Bind`. The
  module that owns a holder is the one that binds it, and a Connector that binds instead
  quietly creates a second holder nobody dispatches when the owning module is absent.
  Failing to get one is the report that the module's Root is not in the scene - put the Root
  back rather than binding around it.
- **A decision belongs in a Command, wherever it would otherwise be taken.** A Context declares
  bindings and nothing else, a View holds no `if` about game rules, a Mediator holds none either,
  and a System type holds no method that does work. Each of them is somewhere a decision tries to
  settle, and the answer is the same every time - dispatch, and let a Command read the conditions
  and decide. What needs no decision is not a decision: a close button that always closes is the
  Mediator calling `_view.Hide()`, and routing that out to a Command and back answers a question
  nobody asked.
- **A flow is read from one Context.** Somebody should see what an operation does by reading the
  sequence it is bound to, without opening a Command or crossing to the Connector. So a Command
  whose only job is to dispatch is not written - bind `DispatchSignalCommand<T>` - and a step that
  orders another module about is dispatched from the sequence.
- **The Connector translates, it does not decide.** One module's announcement joined to another
  module's order is a crossing. A list of consequences hung off one announcement is not: what
  follows an announcement is a decision, and it belongs where the deciding happens.
- The binding phases declare, `Setup` initialises, `Launch` starts. `SignalBindings`,
  `InjectionBindings`, `MediationBindings` and `CommandBindings` only say what the module is
  made of. `Setup` runs once **every** Root in the scene has finished binding, so that is where
  a module readies its Models if they need readying, and the only phase that may reach across
  modules - which is what a Connector does there. `Launch` runs after every `Setup` and
  dispatches the module's first signal.
- **Initialize Order runs from `-100` to `100`**, and the number decides binding order and the
  order of the `Setup` and `Launch` passes. It is not what makes crossing modules safe: `Setup`
  runs a frame after every Root has finished binding, and that barrier is. Author the Hierarchy in
  the same order as the numbers.
- A Command does one unit of work, holds no state between runs, and returns no value. It
  injects models and services, mutates state, and dispatches outgoing signals.
- **Every path out of a retained Command ends in `Release()` or `Stop()`, and an `await` has three
  of them.** A `Retain()` that is never resolved hangs the group for ever - there is no timeout and
  nothing is logged. The success path is the one everybody writes; the work came back with nothing,
  and the work threw, are the two that hang.
- **A Command is a step in a flow; a Function is called from inside one**, and both live in
  `Controllers/`. A sequence is read in order, and that reading is what a Command is for; a
  Function does its work without depending on where it sits. A Function is not a step in the Flow
  Console, so a step you want to see there is a Command.
- A Model owns the module's state and its data, and the rules that keep both valid. It knows
  nothing about Views, Commands, or any other module.
- A Model never subscribes to a signal. Nothing reaches in and changes its state: an
  incoming signal runs a Command, and the Command calls the Model.
- A Model may dispatch its own module's outgoing signals to announce that a value it
  holds has changed. Announcing is allowed; listening is not.
- **Which of the three a module is, is something you can see.** A Service has files under
  `Services/`; a screen module has a context deriving from `ScreenSubContext<TView, TMediator>`;
  everything else is a System. There is no fourth question to ask.
- A Service is a self-contained unit of work. It is not specific to the game it sits in and
  depends on nothing outside itself: it answers the input it is given. A countdown, a
  parser, a storage wrapper. A Service that more than one module needs gets its own module.
- A System is specific to this game. It may lean on other Systems and Services - waiting on
  a signal they raise, or working from data they share - which is exactly what a Service
  may not do. What it may not do is reference another module's assembly.
- A module that exists to provide a Service is named for what it does, and its Root and Context
  keep the `Service` suffix: `CounterModule` holds `Modules.Counter`, and inside it sit
  `CounterServiceRoot` and `CounterServiceContext`. The suffix is what the inspector reads.
- A View holds scene references and raw input, and translates raw input into the action it already
  offers - a swipe left calls the same `NextMapClicked()` the button does - so the Mediator never
  learns which one it was.
- A Mediator drives exactly one View. It listens to signals and dispatches them. It acts on the
  view itself only where nothing is being decided, and dispatches wherever something is.
- **A screen is pooled**, so the same instance opens many times. Everything that follows from that
  - where a View wires its buttons, where a Mediator subscribes, what resets a screen between
  openings, and why an animation has to report that it finished - is in the screens skill, and
  getting any of it wrong fails silently.
- A screen module belongs to the module whose feature it shows, so it lives in the
  `zScreenModules` of a main or a sub module and never under another screen module or a test
  module.
- A screen dispatches its module's **Outgoing** for what leaves it and its **internal** holder for
  what stays.
- A Signal is a name and a payload. `Incoming` is what the module accepts, `Outgoing` is
  what it announces. A module's signals are its public surface - together with the
  interface of a Service, which is the one thing another module may reference directly.
- The public signal holder lives in `Scripts/Signals/`, an assembly of its own -
  `Modules.Player.Signals` beside `Modules.Player` and `Modules.Player.Shared`. **Only a Connector
  references another module's `.Signals` assembly**, and a test module, which may reference
  anything. The holder is not in Shared, because a System or a screen legitimately references a
  neighbour's Shared assembly to read a published enum, and that reference would otherwise put the
  neighbour's signal holder in scope with it - `[InjectSignal] private GameplaySignals` would
  compile. Now it does not, and the compiler is what keeps a cross-module `Dispatch` inside a
  Connector rather than the reader's memory.
- `Scripts/Runtime/Signals/` holds the module's **internal** holder, `PlayerInternalSignals`: what
  the module says to its own commands, dispatched by nothing outside its own assembly. It has no
  `Incoming` and no `Outgoing` - those two halves describe a boundary, and an internal signal never
  crosses one.
- **A shared asset is filed once, on one Root.** A ScriptableObject other modules read goes in the
  *Shared Scriptables* of one Root's adapter - a scene component in its *Shared Monos* - and a
  reader injects `ISharedDataModel` and calls `GetScriptable<RD_Match>()` or
  `GetMonoBehaviour<Canvas>("OverlayCanvas")` - never a second copy dragged onto its own adapter,
  which reads an asset nobody fills when the producer is not in the scene and reports nothing. The
  slot says the asset is common, not who produces it: a test module's Root files a ready-made
  `RD_Match` when the producer is not in the scene. Filing happens when the Root registers, at
  `Awake`, before any binding phase, so a `PostConstruct` may read one. A second filing of the same
  name is reported at the Root that made it and the first filing answers. The reader still
  references the `.Shared` assembly the type lives in - that one line is the record of who reads
  whose data, and the compiler is what keeps a Runtime type out of reach. The module's own two
  slots are read by its Model off the adapter, and a Command asks the Model - a Command never
  reaches for the adapter.
- A module adds nothing to the `Tools/FlowIoC` menu. Whatever it hands the reader ships
  inside the module instead: a test module brings the scene it runs in, already built, so
  installing the module is the only step there is. That holds for the modules FlowIoC ships
  and for the ones a game writes.
- **A diagnostic points at the code that caused it, never at the guard that noticed.** A warning or
  an error is read by double-clicking it, and what has to open is the Command that released without
  retaining or the View with no Context above it - not the framework's `if`.
- **A tool unwires a thing from everywhere it is referenced, then deletes.** Warning and leaving a
  broken project behind is not the answer. The one line a tool never crosses is saving somebody's
  open scene: a scene that is open is changed and left dirty, because whatever else is unsaved in
  it belongs to whoever opened it.
- **Read `Assets/Plugins/FlowIoC/MODULES.md` before working in a module**, and find there which
  module the work belongs to. Then open only that module's `MODULE.md` - not all of them. If that
  file is absent the project has not been opened in Unity since it was cloned; read the cards
  directly with `find . -name MODULE.md` instead, because the directory is a generated cache that
  the next compile rebuilds and that is deliberately not committed.
- A `MODULE.md` is a module's card. Above the `FLOWIOC` block it says what the module is for,
  the words to search for when work belongs to it, the decisions whose reasons are not in the
  code, and the gaps its author knows about. Inside the block everything is generated - never
  edit there, and if a merge conflicts inside it take either side, because the next compile
  writes the correct one.
- Items under a card's `Known gaps` are information. Do not act on them unless asked.
- A GameObject a module needs in the scene goes under that module's Root. The Root is the
  module's one presence in the scene, so an EventSystem, an adapter, or anything else the
  module owns hangs off it rather than sitting loose beside it.
- **A View sits on a child of its Root, never on the Root's own GameObject.** A view finds its
  Context by bubbling up the hierarchy, and the walk starts at `view.transform.parent` - so a view
  authored on the Root itself is invisible to it. Nothing is logged: the view registers against
  nothing and its Mediator never runs.
- **A view that found no Context is a hierarchy to fix, not a `ContextSource` to change.** The
  injector also offers a selected Root and a Root named by string, and those are the escape hatch
  for a view that genuinely cannot be authored under its Root - a screen the screen service
  instantiates under a layer. Reaching for one because bubbling up came back empty hides the
  authoring mistake instead of mending it.

### Injection targets properties, never fields

`[Inject]`, `[InjectSignal]` and `[SignalParam]` all resolve **properties**. A plain field
is silently skipped - no error, no warning, just null at runtime. Always write:

```csharp
[Inject]       private IPlayerModel  _playerModel { get; set; }
[InjectSignal] private PlayerSignals _signals     { get; set; }
[SignalParam]  private double        _amount      { get; set; }
```

A signal's payload arrives through `[SignalParam]` and not through `Execute`. A `Command<int>`
bound to a `Signal<int>` reports `Execute signature mismatch` and does not run.

### Where code goes

Do not create module folders by hand. Use `Tools/FlowIoC/Create Module`; the code
generators and the namespace tools both depend on the exact shape it produces:

```
Modules/
└── PlayerModule/
    ├── MODULE.md                # the module's card - what it is for, and its generated block
    ├── Modules.Player.asmdef
    ├── Prefabs/
    ├── Resources/
    ├── Scenes/
    └── Scripts/
        ├── Editor/
        ├── Runtime/
        │   ├── Constants/
        │   ├── Controllers/        # commands and functions
        │   ├── Data/
        │   │   ├── UnityObjects/   # ScriptableObjects: CD_, RD_, PD_, ED_, DD_
        │   │   └── ValueObjects/   # plain data: VO, CVO, RVO, PVO, EVO, DVO
        │   ├── Entities/
        │   ├── Enums/
        │   ├── Models/
        │   ├── RootsContexts/
        │   ├── Services/            # optional - self-contained, reusable; Sub/ when it splits
        │   ├── Signals/             # PlayerInternalSignals - the module's own traffic
        │   ├── Systems/             # optional - specific to this game; Sub/ when it splits
        │   └── ViewsMediators/
        ├── Shared/                  # Modules.Player.Shared.asmdef - optional, unticked
        │   ├── Constants/
        │   ├── Data/
        │   │   ├── UnityObjects/
        │   │   └── ValueObjects/
        │   └── Enums/
        └── Signals/                 # Modules.Player.Signals.asmdef - optional, ticked
            PlayerSignals            # the module's public surface
```

`Create Command`, `Create Function`, `Create Model` and `Create View` place their files correctly on
their own. Prefer them over writing files by hand.

### A module's three assemblies

A module carves itself into three, and which of the three a reader references is what the
architecture actually enforces:

| Folder | Assembly | Holds | Who references it |
|---|---|---|---|
| `Scripts/Runtime/` | `Modules.Player` | Models, Commands, Views, Systems, the internal signal holder | the module itself, and its test module |
| `Scripts/Shared/` | `Modules.Player.Shared` | the data the module publishes, and the enums and constants that data needs | anyone who reads that data |
| `Scripts/Signals/` | `Modules.Player.Signals` | `PlayerSignals`, and nothing else | a Connector, and the module's own test module |

Everything a module offers the rest of the project goes in `Scripts/Shared/`: the value objects and
ScriptableObjects it publishes, and the enums and constants those need. No Model, no Command, no
View - and not the signal holder. Whoever reads that data references `Modules.Player.Shared` and
never `Modules.Player`.

Both are ticks in `Create Module`. **`Scripts/Signals/` is ticked**, because most modules have a
public surface; **`Scripts/Shared/` is unticked**, because a module pays for that assembly on the
day it actually publishes something. A screen module is the one exception: its Signals tick is on
and cannot be turned off, because a screen generates no Context of its own and the holder is the
only way anything reaches it. A test module is offered neither.

**A module without the folder is finished, not unfinished.** A Service that answers the caller it
was given rather than announcing, and a Connector that wires other modules and owns no signals at
all, both leave the tick off and never have a `Scripts/Signals/`. No tool asks about a folder that
is not there and nothing puts it back. Where the folder is there, the assembly, the namespace
settings and the references that follow from it are checked like everything else.

For a module that already exists, use `Tools/FlowIoC/Add Shared or Signals` rather than making the
folders by hand.

### Data types

Data lives in two folders, and its name says which kind it is. `Data/UnityObjects/` holds the
ScriptableObject assets, prefixed by where their contents come from. `Data/ValueObjects/` holds
the plain `[Serializable]` classes those assets are built out of, suffixed to match.

| Prefix | Means | Value objects inside |
|---|---|---|
| `CD_` | Config data. Authored in the Editor, constant at runtime. | `MapCVO` |
| `RD_` | Runtime data. Produced during play, not persisted. | `MapRVO` |
| `PD_` | Player data. Loaded at startup, saved again whenever it changes. | `MapPVO` |
| `ED_` | Editor data. Read by editor tooling only. | `MapEVO` |
| `DD_` | Database data. Filled from an external backend. | `MapDVO` |

A plain `MapVO` is the right name when the data belongs to no one kind in particular. Which
prefixes and suffixes are legal is declared in `<Solution>.sln.DotSettings`.

### Naming

| Thing | Name |
|---|---|
| Signal container | `PlayerSignals`, with nested `PlayerSignalsIncoming` and `PlayerSignalsOutgoing` |
| Command | `AddCurrencyCommand` |
| Model | `IPlayerModel` and `PlayerModel` |
| Service | `ICounterService` and `CounterService` |
| System | `IMapSystem` and `MapSystem` |
| Sub service | `AssetLoadSubService`, `ScreenBuilderSubService` |
| Sub system | `MapLevelSubSystem` |
| Data asset | `CD_Maps`, `RD_Maps`, `PD_Maps` |
| Value object | `PlayerStateVO`, `MapCVO`, `MapRVO` |
| View and Mediator | `HudView` and `HudMediator` |
| Function | `CalculateDamageFunction` |
| Connector sub-context | `HeroConnectorSubContext` |
| Screen context | `SettingsScreenContext`, deriving from `ScreenSubContext<SettingsScreenView, SettingsScreenMediator>` |
| Assembly definition | `Modules.Player.asmdef` |

Namespaces follow the folder. A value object under `Scripts/Shared/Data/ValueObjects/` is in
`Modules.PlayerModule.Shared.Data.ValueObjects`, which is why it cannot collide with the Runtime
type of the same name.

### The smallest complete flow

Signals - the module's whole public surface, in `Scripts/Signals/`:

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

Command - one unit of work, triggered by a signal:

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

Context - bindings only - and the Root, normally an empty class:

```csharp
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

    public override void CommandBindings()
    {
        base.CommandBindings();

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

public class PlayerRoot : Root<PlayerContext> { }
```

### Crossing between modules

Two modules meet in a Connector sub-context and nowhere else. It takes both signal holders as the
modules bound them and wires one module's `Outgoing` to another's `Incoming`:

```csharp
public class HeroConnectorSubContext : Context
{
    private HeroSignals          _heroSignals;
    private PlayerProfileSignals _playerProfileSignals;

    public override void Setup()
    {
        _heroSignals          = InjectionBinderCrossContext.GetInstance<HeroSignals>();
        _playerProfileSignals = InjectionBinderCrossContext.GetInstance<PlayerProfileSignals>();

        _heroSignals.Outgoing.DecreaseCurrency
            .Connect(_playerProfileSignals.Incoming.DecreaseCurrency);
    }
}
```

A sub-context is named after the one counterpart module, never after the pair, and its wiring is
split by direction: `IncomingSignals()` and `OutgoingSignals()`, undone by
`UnbindIncomingSignals()` and `UnbindOutgoingSignals()` in `DestroyContext`.

A `.Signals` assembly is not dependency free. A public signal generic over a published type means
the holder's assembly references the Shared assembly that type lives in, and a Connector needs
those same references because `Connect<T>` has to infer `T`; without them the compiler reports
**CS0012**. A Connector's reference list is therefore the longest in the project, and that is
correct: the Connector is the one place allowed to know the game's shape.

### Code style

The code style is declared in `<Solution>.sln.DotSettings` at the project root - naming rules,
prefixes and suffixes, spacing. Read it before writing C# and follow what it says.

**Methods that are alternatives to each other share a verb prefix.** Pressing `.` and typing the
verb then offers the whole set, and a reader who knows one of them finds the others without opening
the documentation. The function provider's three terminators are the worked example: `Execute()`,
`ExecuteAsync()` and `ExecuteAndGetResult<T>()` rather than `Run()`, `RunAsync()` and
`GetResult<T>()`, which reads better one name at a time and breaks the family in half. Weigh
discoverability above the prettiest individual name whenever a caller has to choose between the
members of a set.

**Keep `static` to what the engine forces.** Static state cannot be reset between domain reloads,
cannot be substituted in a test, and hides the lifetime of whatever it caches. Write an ordinary
class with instance members and give it an owner that holds the instance. Unity forces a few entry
points - `[InitializeOnLoad]`, `[InitializeOnLoadMethod]`, `[MenuItem]`, and the
`ScriptableObject`/`EditorWindow` factory calls - and those stay as thin as they can be.

**Every enum value carries its number.** Write `Folder = 0, ViewsAndMediators = 1` rather than
letting the compiler count, because Unity serializes an enum as an int: a value inserted in the
middle, or a value deleted, silently renumbers everything below it and every asset already on disk
then reads back as the wrong thing. Numbered, a value can be deleted outright and a new one takes
the next free number. A number a deleted value used is never reused.

### Never hand-edit

`FlowLogType` is generated from the modules present in the project. Change the modules, not the
file. A module's channel is a `const string` in a part of its own, at
`<Module>/Scripts/Generated/FlowLogType.<Module>.cs`, with a `FlowIoC.Generated.asmref` beside it
that compiles the part into FlowIoC's assembly rather than the module's;
`Assets/Plugins/FlowIoC/Generated/FlowLogType.cs` holds only what belongs to no module.

`<Solution>.sln.DotSettings` and the `*.csproj.DotSettings` files beside it are written by
`Tools/FlowIoC/Module Scanner`. Run the menu item rather than editing them.

### Logging

```csharp
FlowLogger.Log(FlowLogType.PlayerModule, "Execute - AddCurrencyCommand");
FlowLogger.LogError(FlowLogType.PlayerModule, "Currency went negative.");
```

**A log message spells names out. Never `nameof` inside one.**
`$"{nameof(Execute)} - {nameof(AddCurrencyCommand)}"` written inside `AddCurrencyCommand` is a real
reference to the type, so Find Usages and a plain search both answer the question *where is this
Command used* with the command's own logging lines. The trade is that a rename leaves the literal
stale, and a stale word in a log line is cheaper than a search that cannot be trusted. This is about
the message text alone - `nameof` stays correct everywhere else.

Logging compiles out unless the `ENABLE_LOG` scripting define is set. The framework already
logs its own contexts, injections, signals and commands on built-in channels, so watching a
flow does not require adding log lines.

**An error is the exception, and it is logged exactly once.** `FlowLogger.LogError` carries no
`[Conditional]`, so an error reaches the console whether or not `ENABLE_LOG` is defined - a project
with logging switched off is exactly the one that most needs to be told something is broken. Never
put a `Debug.LogError` beside a `FlowLogger.LogError` for the same fault: `FlowLogger` forwards to
`Debug` itself, so the pair prints the error twice whenever logging is on.

### Where the detail lives

The rules above are the whole of what is always true. Everything else FlowIoC has to say is in a
skill, installed under `.claude/skills`, and each one carries the worked examples, the failure modes
and the reasons this block leaves out. Read the one that covers the work in front of you.

| Working on | Skill |
|---|---|
| Creating, extending or deleting a module; where a file or an assembly goes | `flowioc-scaffolding` |
| A Command or a Function - which of the two, binding a sequence, resolving a `Retain` | `flowioc-controllers` |
| A UI screen - its context and `ScreenCVO`, opening and filling it, animations, a Mediator's guard | `flowioc-screens` |
| Wiring two modules - a Connector sub-context, a signal that never arrives | `flowioc-connectors` |
| A System or a Service - which of the two, sub systems, what a Service announces | `flowioc-systems-services` |
| Where a Root sits, its Initialize Order, making it persistent, what its context takes back | `flowioc-root-order` |
| Naming a ScriptableObject or a value object, and which prefix it takes | `flowioc-data-types` |

The editor tooling explains itself in **Tools ▸ FlowIoC ▸ Help**: what each panel decides on your
behalf, and the setup steps that fail silently when skipped.

### Deeper documentation

| Topic | Where |
|---|---|
| Everything | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/README.md |
| Screens and popups | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Runtime/ScreenModule/Documentation/ScreenModule.md |
| Object pooling | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Runtime/PoolModule/Documentation/PoolModule.md |
| Addressables | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Runtime/AssetModule/Documentation/AssetModule.md |
| Flow Console | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Runtime/ConsoleModule/Documentation/FlowConsole.md |
| Code generators | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Editor/CodeGenerator/Documentation.md |
