## FlowIoC

These rules apply only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, ignore this block and delete it
from `AGENTS.md`.

FlowIoC is a signal-driven IoC framework for Unity. A game is split into modules that own
their state, logic and presentation. Modules never reference one another; they are wired
together declaratively by Connectors. Nothing in the C# type system enforces any of this,
so follow the rules below deliberately.

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
- The Root inspector's *Add Sub Context* offers what may honestly be added there, and three rules
  decide it. A context that some Root declares as its `Root<T>` is built by that Root already, so
  it is not offered - adding it to a second Root would build a second instance and run the same
  bindings twice. A module meant to be hosted on another module's Root says so with
  `[AllowAsSubContext]` on its context and is offered again, and `[ExcludeFromContextWindow]` says
  the opposite and wins over it. A Connector sub-context is offered on the Connector Root and
  nowhere else, and every other Root is offered everything but those, so the wiring between two
  modules stays in the one place it belongs; a context counts as a Connector's when its name says
  so, the way `HeroConnectorSubContext` does, or when it carries
  `[FlowHeader(FlowRole.Connector)]`. `Create Module` offers `[AllowAsSubContext]` as a toggle on a
  main module that gets a Root, unticked, because a module with a Root of its own is the ordinary
  case.
- **A decision belongs in a Command, wherever it would otherwise be taken.** That is the rule the
  next several are instances of: a Context declares bindings and nothing else, a View holds no `if`
  about game rules, a Mediator holds none either, and a System type holds no method that does work.
  Each of them is somewhere a decision tries to settle, and the answer is the same every time -
  dispatch, and let a Command read the conditions and decide. What needs no decision is not a
  decision: a close button that always closes is the Mediator calling `_view.Hide()`, and routing
  that out to a Command and back answers a question nobody asked.
- A Context declares bindings and nothing else.
- The binding phases declare, `Setup` initialises, `Launch` starts. `SignalBindings`,
  `InjectionBindings`, `MediationBindings` and `CommandBindings` only say what the module is
  made of. `Setup` runs once **every** Root in the scene has finished binding, so that is where
  a module readies its Models if they need readying, and the only phase that may reach across
  modules - which is what a Connector does there. `Launch` runs after every `Setup` and
  dispatches the module's first signal.
- A Command does one unit of work, holds no state between runs, and returns no value. It
  injects models and services, mutates state, and dispatches outgoing signals.
- **A Command is a step in a flow; a Function is called from inside one.** A sequence is read in
  order, one Command after another, and that reading is what a Command is for. A Function does its
  work without depending on where it sits, so it is what a Command reaches for mid-`Execute` - to
  go somewhere, do something and come back - or what several Commands share. It returns a value
  when it has one to return and derives from `FunctionVoid` when it does not; the return type is
  not what makes it a Function.
- Reach for a Function when the work happens more than once inside one `Execute`, or when more than
  one Command needs it. The same work is often a Command instead, and that is the right answer when
  it is a step somebody should be able to read in the sequence.
- The trade a Function makes is the Flow Console: it is not a step there. **A step you want to see
  in the console is a Command.**
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
- A Service knows nobody - not another Service, not a System, not a Screen - and **a finished
  Service's assembly gets no later additions.** Work that arrives afterwards goes somewhere else,
  or the Service turns into a module of the game it happens to sit in. What FlowIoC embeds in
  itself is the one exception: the package's own services may lean on each other.
- **A Service is driven in two ways and answers in two ways.** In: a caller injects its interface
  and calls it, or dispatches one of the Commands it ships. It ships a Command for the case where
  the caller needs the work to be a step in a sequence with the next step waiting on it -
  `ToSequence<DispatchSignalCommand>` starts the same work but the sequence carries on without it.
  Out: a signal when what happened may concern the whole game, and a callback the caller handed in
  when the answer is only for whoever asked - which is what every `Action` on `ICounterService` is.
  So a Service gets a signal holder when something outside it has to be told and no subscription
  already carries that, and not before.
- A System is specific to this game. It may lean on other Systems and Services - waiting on
  a signal they raise, or working from data they share - which is exactly what a Service
  may not do.
- Systems and Services both dispatch outgoing signals when they have something to announce,
  and a Command drives their work the same way it drives a Model's.
- A Service is an interface and an implementation, the way a Model is: `ICounterService` and
  `CounterService`, under `Services/`.
- **A System needs no `System.cs`.** The work is followed through the command flow its Context
  declares, and a module can be a complete System without a type named `...System`. One arrives
  when the module wants a surface: to collapse many injections into one, so a Command injects
  `IMapSystem` once and writes `_map.Grid.Build(...)`, or to make what is available discoverable.
  It lives under `Systems/` and is an interface and an implementation like the rest.
- **A System type holds injected members and no method that does work.** The moment a method on it
  does something, that work belongs in a Command - which is what keeps the step in the Flow Console
  and lets a sequence wait for it. `PostConstruct` may assemble the surface, and that is the whole
  allowance.
- A System is built out of sub systems. Models appear among its members where the module's own
  state lives, but the sub system is the unit it is assembled from. When a module has a System, a
  Command reaches that module's own Models **through it** rather than injecting a Model directly; a
  module without a System is the ordinary case and its Commands inject Models as they always have.
- **A Model owns the module's state and data; a sub system computes**, and may read what other
  modules publish. A sub system may hold data of its own, and how far its scope reaches decides
  where that data lives: its own working data stays in it, the module's state belongs to a Model,
  and data that is large or reaches past this module belongs to a module of its own.
- A Service and a System both split into `Services/Sub/` and `Systems/Sub/`, for two reasons and
  not one: the unit grew, or a chained surface is wanted so that pressing `.` offers a short list
  rather than thirty verbs. `IScreenService` is the worked example of the second - `Load`, `Check`,
  `Hide` group the verbs under nouns, and `Open<T>()` returns a builder where nothing happens until
  `Show()`. A sub service or sub system is reached through the Service or System that owns it and
  is never bound across contexts on its own.
- `Services/` and `Systems/` are optional folders on a main module. `Create Module` ticks them from
  *Role* - **System** arrives with `Systems/` ticked, **Service** with `Services/` ticked - and
  either tick may be changed before the module is written.
- A module that exists to provide a Service is named for what it does, and its Root and Context
  keep the `Service` suffix: `CounterModule` holds `Modules.Counter`, and inside it sit
  `CounterServiceRoot` and `CounterServiceContext`. The suffix is what the inspector reads - a
  Root takes the colour of whatever it roots, and it decides that from its own name - so
  `CounterRoot` would be drawn as a plain Root while `CounterServiceRoot` is drawn as a Service.
  The module name has no such job, so it says what the module counts, parses or stores rather
  than repeating the word. `Create Module` asks for this as *Role* on a main module that gets a
  Root: **System** writes `PlayerSystemRoot` and `PlayerSystemContext`, **Service** writes
  `CounterServiceRoot` and `CounterServiceContext`, and **Core** writes the plain `PlayerRoot` and
  `PlayerContext`. System is what it starts on, because a module written for the game at hand is a
  System; the module folder, its assembly and its namespaces are untouched whichever is picked.
- **Core is the one role a name cannot carry.** A Core module is part of the project's frame rather
  than of its game: the project holds exactly one, its Initialize Order is reserved rather than
  chosen, and a game extends it instead of authoring it - a screen module listed on a Root, a
  Connector sub-context added to the Connector. `MainRoot` and `ScreenRoot` are what it means. There
  being one Main and one Screen in a project, the name has nothing to disambiguate from and takes no
  suffix, so the Root says it with `[FlowHeader(FlowRole.Core)]` instead and `Create Module` writes
  that line for Role = Core. The attribute wins over every other reading, which is also how a
  context that is not named for the job declares itself a Connector's.
- A View holds scene references and raw input.
- A Mediator drives exactly one View. It listens to signals and dispatches them. It acts on the
  view itself only where nothing is being decided - hiding a screen whose close button always
  closes - and dispatches wherever something is.
- A `ScreenView` wires its buttons in `OnEnable` and drops them in `OnDisable`, never in
  `Awake` or `Start`. A screen is pooled: hiding it deactivates the object and reopening it
  shows the same instance, so `Awake` runs once while the screen opens many times.
- A screen module belongs to the module whose feature it shows, so it lives in the
  `zScreenModules` of a main or a sub module and never under another screen module or a test
  module. `Create Module` offers exactly those parents.
- A screen module's context declares the screen. It derives from
  `ScreenSubContext<TView, TMediator>`, which binds the View to the Mediator, and it declares
  where the prefab lives and how the screen behaves by default in a `ScreenCVO`:
  `Load = ScreenLoadCVO.Addressable("SettingsScreen")`, `Layer`, `Tag`, `ManagerId` and the two
  animation flags. There is no screen config asset.
- The Root that lists a screen context may override that default. Ticking *Override Screen* on
  the entry makes `ManagerId`, `Layer`, `Tag` and the two animation flags editable in the Root's
  inspector, seeded from what the context declares; with the override off the same five are shown
  read-only, so a Root always says how the screens it lists are configured. `Load` is never
  overridable, because where a prefab lives is the module's business and not the scene's. Listing
  the same screen context on two Roots with two `ManagerId`s registers it twice, and the two
  registrations are opened, pooled and unregistered independently.
- A screen context is a sub-context of the module the screen belongs to, listed in that
  module's Root with Auto Setup on - never in `ScreenRoot`. It registers the screen in `Setup`,
  so a screen whose module's Root is not in the scene is not registered, and `Open` reports
  that instead of opening it.
- A screen's test Root lists the production screen context as a sub-context rather than
  re-declaring the screen; the test context only opens the screen in `Launch`.
- A Signal is a name and a payload. `Incoming` is what the module accepts, `Outgoing` is
  what it announces. A module's signals are its public surface - together with the
  interface of a Service, which is the one thing another module may reference directly.
- The public signal holder lives in `Scripts/Signals/`, an assembly of its own -
  `Modules.Player.Signals` beside `Modules.Player` and `Modules.Player.Shared`. A Connector
  reaches a module's signals through `Modules.Player.Signals` and through nothing else. The
  holder is not in Shared, because a System or a screen legitimately references a neighbour's
  Shared assembly to read a published enum, and that reference would otherwise put the
  neighbour's signal holder in scope with it - `[InjectSignal] private GameplaySignals` would
  compile. Now it does not, and the compiler is what keeps a cross-module `Dispatch` inside a
  Connector rather than the reader's memory.
- **Only a Connector references another module's `.Signals` assembly.** A test module is the
  one exception, because it drives the module it sits under and may reference anything.
- A `.Signals` assembly is not dependency free. A public signal generic over a published type
  means the holder's assembly references the Shared assembly that type lives in - its own
  module's, and sometimes another module's: `Signal<DifficultyType>` on a screen's holder
  makes `Modules.Main.Screen.Signals` reference `Modules.Gameplay.Shared`. A Connector needs
  those same Shared references for the same reason, because `Connect<T>` has to infer `T`;
  without them the compiler reports **CS0012**. A Connector's reference list is therefore the
  longest in the project, and that is correct: the Connector is the one place allowed to know
  the game's shape.
- `Scripts/Runtime/Signals/` holds the module's **internal** holder,
  `PlayerInternalSignals`: what the module says to its own commands, dispatched by nothing
  outside its own assembly. It has no `Incoming` and no `Outgoing` - those two halves
  describe a boundary, and an internal signal never crosses one.
- A module adds nothing to the `Tools/FlowIoC` menu. Whatever it hands the reader ships
  inside the module instead: a test module brings the scene it runs in, already built, so
  installing the module is the only step there is. That holds for the modules FlowIoC ships
  and for the ones a game writes.
- A GameObject a module needs in the scene goes under that module's Root. The Root is the
  module's one presence in the scene, so an EventSystem, an adapter, or anything else the
  module owns hangs off it rather than sitting loose beside it.
- A module whose work outlives a scene makes its Root persistent in `BeforeCreateContext`,
  which runs just before the context is built:

  ```csharp
  protected override void BeforeCreateContext()
  {
      transform.SetParent(null);
      DontDestroyOnLoad(gameObject);
  }
  ```

  The `SetParent` is not decoration: Unity marks only root level objects as do not destroy,
  so a Root authored under something else has to detach itself before it can survive.
- **Initialize Order runs from `-100` to `100`, and nothing needs to sit outside that range.**
  `-100` is the earliest a Root can be and `100` is the latest, so the two ends are taken by the
  module that must finish before anything reads its data - local save - and by `MainRoot`, whose
  `Launch` is the entry point. Services take the rest of the negative band because they depend on
  nothing, the game's own modules and Systems take `0` to `97`, `ConnectorRoot` takes `98` so the
  wiring reads after the modules it wires, and `ScreenRoot` takes `99`. Author the Hierarchy in the
  same order. The number decides binding order and the order of the `Setup` and `Launch` passes; it
  is not what makes crossing modules safe. `Setup` runs a frame after every Root has finished
  binding, and that barrier is.
- A module that has to put data in place before anything reads it takes `-100`. This works because
  `PostConstruct` runs during the **binding** pass, not after `Setup`: `RootsManager` calls
  `StartContext()` on every Root in Initialize Order, synchronously, and each one runs its own
  `PostConstruct` before the next begins. `Setup` would already be a frame too late, and a Command
  later still.

### Injection targets properties, never fields

`[Inject]`, `[InjectSignal]` and `[SignalParam]` all resolve **properties**. A plain field
is silently skipped - no error, no warning, just null at runtime. Always write:

```csharp
[Inject]       private IPlayerModel  _playerModel { get; set; }
[InjectSignal] private PlayerSignals _signals     { get; set; }
[SignalParam]  private double        _amount      { get; set; }
```

### Where code goes

Do not create module folders by hand. Use `Tools/FlowIoC/Create Module`; the code
generators and the namespace tools both depend on the exact shape it produces:

```
Modules/
└── PlayerModule/
    ├── Modules.Player.asmdef
    ├── Prefabs/
    ├── Resources/
    ├── Scenes/
    └── Scripts/
        ├── Editor/
        ├── Runtime/
        │   ├── Constants/
        │   ├── Controllers/        # commands
        │   ├── Data/
        │   │   ├── UnityObjects/   # ScriptableObjects: CD_, RD_, PD_, ED_, DD_
        │   │   └── ValueObjects/   # plain data: VO, CVO, RVO, PVO, EVO, DVO
        │   ├── Entities/
        │   ├── Enums/
        │   ├── Functions/
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
        └── Signals/                 # Modules.Player.Signals.asmdef - always
            PlayerSignals            # the module's public surface
```

`Create Command`, `Create Model` and `Create View` place their files correctly on their
own. Prefer them over writing files by hand.

### A module's three assemblies

A module carves itself into three, and which of the three a reader references is what the
architecture actually enforces:

| Folder | Assembly | Holds | Who references it |
|---|---|---|---|
| `Scripts/Runtime/` | `Modules.Player` | Models, Commands, Views, Systems, the internal signal holder | the module itself, and its test module |
| `Scripts/Shared/` | `Modules.Player.Shared` | the data the module publishes, and the enums and constants that data needs | anyone who reads that data |
| `Scripts/Signals/` | `Modules.Player.Signals` | `PlayerSignals`, and nothing else | a Connector, and the module's own test module |

`Scripts/Signals/` is always there; `Scripts/Shared/` is a tick in `Create Module`, unticked,
because a module pays for that assembly on the day it actually publishes something. Both are
carved out by an asmdef sitting in the folder - Unity gives every file to the nearest asmdef
above it - so the module references both to reach its own published data and its own holder.

**The folder is mandatory, the assembly is not.** A module with no public signals leaves
`Scripts/Signals/` empty and writes no asmdef in it, and Module Scanner reads that as Ok rather
than as a finding - demanding one would ship a DLL with nothing in it. `ConnectorModule` is the
case this exists for: it wires other modules' signals and announces none of its own.

### Publishing data through Shared

Everything a module offers the rest of the project goes in `Scripts/Shared/`: the value
objects and ScriptableObjects it publishes, and the enums and constants those need. No Model,
no Command, no View - and not the signal holder, which is the whole point of the split above.

A public signal may only carry a type another module can see. A `Signal<CameraCVO>` means
`CameraCVO` belongs in `Shared/Data/ValueObjects/`, not in the Runtime folder of the same
name, and `Modules.Player.Signals` references `Modules.Player.Shared` to see it.

Whoever reads that data references `Modules.Player.Shared` and never `Modules.Player`. So
`PlayerScreenModule` can read `CD_PlayerRules` without gaining access to `PlayerModel`,
`AddCurrencyCommand` **or `PlayerSignals`**. `Create Module` writes the reference for you: tick
Shared on a main module, and every screen, sub and test module created under it afterwards
points at it.

For a module that already exists, use `Tools/FlowIoC/Add Shared Data` rather than making
the folders by hand. It lays down the same folders, writes the assembly and its settings
file, and adds the reference to the module and to every screen, sub and test module already
under it.

Shared is offered on main, sub and screen modules, and starts unticked. A test module is the
one kind without it: it holds nothing another module reads, and it is allowed to reference
anything directly anyway. If two modules need the same data and neither owns it, that data
belongs in a module of its own, the way a shared Service does.

Namespaces follow the folder, the way they already do for a module: a value object under
`Scripts/Shared/Data/ValueObjects/` is in `Modules.PlayerModule.Shared.Data.ValueObjects`,
which is why it cannot collide with the Runtime type of the same name in
`Modules.PlayerModule.Data.ValueObjects`. The public holder under `Scripts/Signals/` lands in
`Modules.PlayerModule.Signals`, which is the namespace the internal holder is already in - one
`using` reaches both, and the assembly boundary is what tells them apart. `Create Module`
writes `Modules.Player.Shared.csproj.DotSettings` and `Modules.Player.Signals.csproj.DotSettings`
for this: a `.csproj.DotSettings` only applies to the project it is named after, so the
module's own file cannot tell Rider to skip `Scripts` on their behalf.

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

So `CD_Maps` is the config asset and its entries are `MapCVO`; `PD_Maps` is the saved asset and its
entries are `MapPVO`. A plain `MapVO` is the right name when the data belongs to no one kind in
particular, and a project may add a family of its own the same way.

One value object may carry two kinds at once, and is then named after neither of them. A
`GameHexVO` that holds a `GameHexCVO` for what the level author placed and a `GameHexRVO` for
what play produced is named for the hex, because both halves are wanted in the same place.

Which prefixes and suffixes are legal is declared in `<Solution>.sln.DotSettings`; what each one
means is the table above.

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

Internal signals - what the module says to itself, in `Scripts/Runtime/Signals/`. No
`Incoming`, no `Outgoing`, and internal so nothing outside the assembly can dispatch them:

```csharp
internal class PlayerInternalSignals : ISignalHolder
{
    public Signal Tick = new(hideCommandLog: true);
    public Signal<double> RecalculateInterest = new();
}
```

Model - state, and the rules that keep it valid:

```csharp
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

Context - bindings only:

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
```

Root - the module's presence in the scene, normally an empty class:

```csharp
public class PlayerRoot : Root<PlayerContext> { }
```

### Crossing between modules

Two modules meet in a Connector sub-context and nowhere else. It takes both signal
holders as the modules bound them and wires one module's `Outgoing` to another's
`Incoming`:

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

`Connect` also accepts a plain delegate, and can adapt between signals whose parameter
types differ by taking a converter as its second argument.

### Code style

The code style is declared in `<Solution>.sln.DotSettings` at the project root - naming rules,
prefixes and suffixes, spacing. Read it before writing C# and follow what it says.

**Every enum value carries its number.** Write `Folder = 0, ViewsAndMediators = 1` rather than
letting the compiler count, because Unity serializes an enum as an int: a value inserted in the
middle, or a value deleted, silently renumbers everything below it and every asset already on disk
then reads back as the wrong thing. Numbered, a value can be deleted outright and a new one takes
the next free number rather than whatever position it was typed into. A number a deleted value used
is never reused - `FolderEVO.RetiredFolderTypes` is what that looks like, a list of the numbers that
are gone so a heal can take their folders out of a config written while they existed.

### Never hand-edit

`Assets/Plugins/FlowIoC/Generated/FlowLogType.cs` is generated from the modules present in the
project. Change the modules, not the file.

`<Solution>.sln.DotSettings` and the `*.csproj.DotSettings` files beside it are written by
`Tools/FlowIoC/Module Scanner`. Run the menu item rather than editing them.

### Logging

```csharp
FlowLogger.Log(FlowLogType.PlayerModule, $"{nameof(Execute)} - {nameof(AddCurrencyCommand)}");
FlowLogger.LogError(FlowLogType.PlayerModule, "Currency went negative.");
```

Logging compiles out unless the `ENABLE_LOG` scripting define is set. The framework already
logs its own contexts, injections, signals and commands on built-in channels, so watching a
flow does not require adding log lines.

### Deeper documentation

| Topic | Where |
|---|---|
| Everything | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/README.md |
| Screens and popups | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Runtime/ScreenModule/Documentation/ScreenModule.md |
| Object pooling | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Runtime/PoolModule/Documentation/PoolModule.md |
| Addressables | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Runtime/AssetModule/Documentation/AssetModule.md |
| Flow Console | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Runtime/ConsoleModule/Documentation/FlowConsole.md |
| Code generators | https://github.com/FlowArc/FlowIoC/blob/{VERSION}/Editor/CodeGenerator/Documentation.md |
