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
  `Modules.B`. The only crossing point is a Connector.
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
  what it announces. A module's signals are its entire public surface.

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
        └── Runtime/
            ├── Constants/
            ├── Controllers/        # commands
            ├── Datas/
            │   ├── UnityObjects/   # ScriptableObject configs
            │   └── ValueObjects/   # plain data, suffixed VO
            ├── Entities/
            ├── Enums/
            ├── Functions/
            ├── Models/
            ├── RootsContexts/
            ├── Services/            # self-contained, reusable
            ├── Signals/
            ├── Systems/             # specific to this game
            └── ViewsMediators/
```

`Create Command`, `Create Model` and `Create View` place their files correctly on their
own. Prefer them over writing files by hand.

### Naming

| Thing | Name |
|---|---|
| Signal container | `PlayerSignals`, with nested `PlayerSignalsIncoming` and `PlayerSignalsOutgoing` |
| Command | `AddCurrencyCommand` |
| Model | `IPlayerModel` and `PlayerModel` |
| Service | `ICountdownService` and `CountdownService` |
| System | `IMapSystem` and `MapSystem` |
| Value object | `PlayerStateVO` |
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
public class PlayerRoot : SingletonRoot<PlayerContext> { }
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

### Never hand-edit

`Assets/FlowIoC/Generated/FlowLogType.cs` is generated from the modules present in the
project. Change the modules, not the file.

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
