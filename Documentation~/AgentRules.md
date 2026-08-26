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
  interface, the way `OpenMatchBoardScreenCommand` injects `ICountdownService`. Being
  usable this way is the point of a Service.
- **A sub-module reaches the module it lives in.** A screen or sub module may use its
  parent's types. The direction is one way: a module never knows what sits in its own
  `zScreenModules` or `zSubModules`.
- **A test module reaches anything.** Everything under `zTestModules` is test code, so it
  may reference any module in the project. In exchange, every script in it is wrapped in
  `#if UNITY_EDITOR`.
- Systems are never added to one another's assemblies. Two Systems in separate modules
  talk through signals wired in a Connector, like any other cross-module traffic.
- A Context declares bindings and nothing else. If a Context needs an `if`, that decision
  belongs in a Command.
- A Command does one unit of work, holds no state between runs, and returns no value. It
  injects models and services, mutates state, and dispatches outgoing signals.
- A Function returns a value and does not orchestrate. If you want the step visible in the
  Flow Console, write a Command, not a Function.
- A Model owns state and the rules that keep it valid. It knows nothing about Views,
  Commands, or any other module.
- A Model never subscribes to a signal. Nothing reaches in and changes its state: an
  incoming signal runs a Command, and the Command calls the Model.
- A Model may dispatch its own module's outgoing signals to announce that a value it
  holds has changed. Announcing is allowed; listening is not.
- A Service is a self-contained unit of work. It is not specific to the game it sits in and
  depends on nothing outside itself: it answers the input it is given. A countdown, a
  parser, a storage wrapper. A Service that more than one module needs gets its own module.
- A System is specific to this game. It may lean on other Systems and Services - waiting on
  a signal they raise, or working from data they share - which is exactly what a Service
  may not do.
- Systems and Services both dispatch outgoing signals when they have something to announce,
  and a Command drives their work the same way it drives a Model's.
- A Service lives in `Services/`, a System in `Systems/`. Both are an interface and an
  implementation, the way a Model is: `ICountdownService` and `CountdownService`.
- A View holds scene references and raw input. A View with an `if` about game rules is
  doing the Mediator's job.
- A Mediator drives exactly one View. It listens to signals and dispatches them, and holds
  no game rules either.
- A Signal is a name and a payload. `Incoming` is what the module accepts, `Outgoing` is
  what it announces. A module's signals are its public surface - together with the
  interface of a Service, which is the one thing another module may reference directly.

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
        │   ├── Services/            # self-contained, reusable
        │   ├── Signals/
        │   ├── Systems/             # specific to this game
        │   └── ViewsMediators/
        └── Shared/                  # optional - Modules.Player.Shared.asmdef
            ├── Constants/
            ├── Data/
            │   ├── UnityObjects/
            │   └── ValueObjects/
            └── Enums/
```

`Create Command`, `Create Model` and `Create View` place their files correctly on their
own. Prefer them over writing files by hand.

### Publishing data through Shared

A module that has to hand data to another module puts it in `Scripts/Shared/`, which is
an assembly of its own - `Modules.Player.Shared` beside `Modules.Player`. Only data lives
there: value objects, the ScriptableObjects built out of them, and the enums and constants
those need. No Model, no Command, no View.

Whoever reads that data references `Modules.Player.Shared` and never `Modules.Player`. So
`PlayerScreenModule` can read `CD_PlayerRules` without gaining access to `PlayerModel` or
`AddCurrencyCommand`. `Create Module` writes the reference for you: tick Shared on a main
module, and every screen, sub and test module created under it afterwards points at it.

For a module that already exists, use `Tools/FlowIoC/Add Shared Data` rather than making
the folders by hand. It lays down the same folders, writes the assembly and its settings
file, and adds the reference to the module and to every screen, sub and test module already
under it.

The parent module references its own Shared assembly too. The asmdef inside
`Scripts/Shared/` takes that folder out of `Modules.Player`, so without the reference a
module could not read the data it publishes.

Shared is offered on main modules only. A screen or test module holds nothing another
module reads; if two modules need the same data and neither owns it, that data belongs in
a module of its own, the way a shared Service does.

Namespaces follow the folder, the way they already do for a module: a value object under
`Scripts/Shared/Data/ValueObjects/` is in `Modules.PlayerModule.Shared.Data.ValueObjects`,
which is why it cannot collide with the Runtime type of the same name in
`Modules.PlayerModule.Data.ValueObjects`. `Create Module` writes
`Modules.Player.Shared.csproj.DotSettings` for this: a `.csproj.DotSettings` only applies to
the project it is named after, so the module's own file cannot tell Rider to skip `Scripts`
on the Shared assembly's behalf.

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
| Service | `ICountdownService` and `CountdownService` |
| System | `IMapSystem` and `MapSystem` |
| Data asset | `CD_Maps`, `RD_Maps`, `PD_Maps` |
| Value object | `PlayerStateVO`, `MapCVO`, `MapRVO` |
| View and Mediator | `HudView` and `HudMediator` |
| Function | `CalculateDamageFunction` |
| Connector sub-context | `HeroConnectorSubContext` |
| Assembly definition | `Modules.Player.asmdef` |

### The smallest complete flow

Signals - the module's whole public surface:

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

Two modules meet in a Connector sub-context and nowhere else. It binds both signal
holders and wires one module's `Outgoing` to another's `Incoming`:

```csharp
public class HeroConnectorSubContext : Context
{
    private HeroSignals          _heroSignals;
    private PlayerProfileSignals _playerProfileSignals;

    public override void Setup()
    {
        _heroSignals          = InjectionBinderCrossContext.Bind<HeroSignals>();
        _playerProfileSignals = InjectionBinderCrossContext.Bind<PlayerProfileSignals>();

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

### Never hand-edit

`Assets/Plugins/FlowIoC/Generated/FlowLogType.cs` is generated from the modules present in the
project. Change the modules, not the file.

`<Solution>.sln.DotSettings` and the `*.csproj.DotSettings` files beside it are written by
`Tools/FlowIoC/Module Configuration/Update Namespace Settings`. Run the menu item rather than
editing them.

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
