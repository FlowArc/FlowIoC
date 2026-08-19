# FlowIoC Base Module

## Table of Contents
1. [Introduction](#introduction)
2. [Architecture Overview](#architecture-overview)
   - [Component Relationships](#component-relationships)
   - [Dependency Injection](#dependency-injection)
   - [Signal System](#signal-system)
3. [Core Components](#core-components)
   - [Context System](#context-system)
   - [Injection System](#injection-system)
   - [Signal System](#signal-system-1)
   - [Command System](#command-system)
4. [Dependency Injection](#dependency-injection-1)
   - [InjectionBinder](#injectionbinder)
   - [Binding Types](#binding-types)
   - [Cross-Context Injection](#cross-context-injection)
5. [Signals and Commands](#signals-and-commands)
   - [Signal Implementation](#signal-implementation)
   - [Command Binding](#command-binding)
   - [Command Sequencing](#command-sequencing)
6. [View-Mediator Pattern](#view-mediator-pattern)
   - [Views](#views)
   - [Mediators](#mediators)
   - [Mediation Binding](#mediation-binding)
7. [Context Management](#context-management)
   - [Context Hierarchy](#context-hierarchy)
   - [Context Lifecycle](#context-lifecycle)
   - [Multi-Context Applications](#multi-context-applications)
8. [Memory Management](#memory-management)
   - [Pool System](#pool-system)
   - [Binding Disposal](#binding-disposal)
   - [Automatic Cleanup](#automatic-cleanup)
9. [Practical Examples](#practical-examples)
   - [Basic Application Setup](#basic-application-setup)
   - [Advanced Configurations](#advanced-configurations)
   - [Testing with Mocks](#testing-with-mocks)
10. [Performance Optimization](#performance-optimization)
    - [Best Practices](#best-practices)
    - [Memory Management Tips](#memory-management-tips)
11. [Internal Implementation Details](#internal-implementation-details)
    - [Binding Process](#binding-process)
    - [Command Execution Flow](#command-execution-flow)
    - [Memory Management Implementation](#memory-management-implementation)

## Introduction

The FlowIoC Base Module is a lightweight, flexible dependency injection framework designed specifically for Unity applications. It provides a robust architecture for managing dependencies, handling communication between components, and organizing application logic.

The system is designed with a focus on:
- **Performance**: Optimized for Unity with efficient memory pooling
- **Flexibility**: Multiple binding methods for different use cases
- **Testability**: Easy mocking and dependency substitution
- **Maintainability**: Clear separation of concerns through contexts
- **Developer Experience**: Intuitive API for easy adoption

<!-- [VISUAL RECOMMENDATION: System overview diagram showing the relationship between Contexts, Injection, Signals, and Commands] -->

## Architecture Overview

The Base Module follows a modular architecture with clear separation of concerns:

```
┌───────────────────────────────────────┐
│               Context                 │
└─┬────────────────┬─────────────────┬──┘
  │                │                 │
┌─▼──────────┐  ┌──▼────────┐  ┌────▼─────┐
│InjectionBinder│  │CommandBinder│  │MediationBinder│
└─┬──────────┘  └──┬────────┘  └────┬─────┘
  │                │                 │
┌─▼──────────┐  ┌──▼────────┐  ┌────▼─────┐
│ Injectable │  │  Commands  │  │ View-Mediator │
│  Objects   │  │  & Signals │  │    Pairs     │
└────────────┘  └───────────┘  └──────────────┘
```

The system is composed of three primary layers:
1. **Context Layer**: Manages the application lifecycle and component registration
2. **Binder Layer**: Handles specific binding tasks (injection, commands, mediation)
3. **Component Layer**: The actual objects that make up the application

### Component Relationships

The Base Module employs a highly modular architecture with several key components:

1. **Context**: The central organizing unit that manages the lifecycle of your application
   - Holds references to all binders
   - Provides lifecycle hooks (Start, Launch, Setup, etc.)
   - Manages sub-contexts

2. **Binders**:
   - **InjectionBinder**: Manages dependency injection
   - **CommandBinder**: Binds signals to commands
   - **MediationBinder**: Connects views with mediators

3. **Signals**: Type-safe messaging system
   - Allows communication between components without direct references
   - Supports up to 4 parameters
   - Can be bound to commands for automatic execution

4. **Commands**: Encapsulated pieces of logic
   - Can be sequenced
   - Support both synchronous and asynchronous execution
   - Automatically pooled for memory efficiency

<!-- [VISUAL RECOMMENDATION: Component interaction diagram showing data flow between components] -->

### Dependency Injection

The system uses constructor and property injection to maintain loose coupling between components:

```csharp
// From Context.cs
void IContext.InjectAllInstances()
{
    List<InjectionBinding> injectionBindings = InjectionBinder.GetAllInjectionBindings();
    List<InjectionBinding> crossContextInjectedBindings = InjectionBinderCrossContext.GetAllInjectionBindings();

    injectionBindings = injectionBindings.Concat(crossContextInjectedBindings).ToList();

    foreach (InjectionBinding binding in injectionBindings)
    {
        if (binding == null)
            continue;

        if (binding.BindedContext == null)
            this.TryToInjectObject(binding.Value);
        else
            binding.TryToInjectObject();
    }
}
```

### Signal System

The system uses signals for communication between components:

```csharp
// Signal declaration
public class GameSignals : ISignalHolder
{
    public Signal GameStarted = new();
    public Signal<int> ScoreChanged = new();
    public Signal<Player, Enemy> PlayerHit = new();
}

// Signal binding
CommandBinder.Bind(_gameSignals.GameStarted).To<StartGameCommand>();
CommandBinder.Bind(_gameSignals.ScoreChanged).To<UpdateScoreCommand>();
CommandBinder.Bind(_gameSignals.PlayerHit).To<HandlePlayerHitCommand>();

// Signal dispatch
_gameSignals.GameStarted.Dispatch();
_gameSignals.ScoreChanged.Dispatch(100);
_gameSignals.PlayerHit.Dispatch(player, enemy);
```

<!-- [VISUAL RECOMMENDATION: Signal flow diagram showing how components communicate] -->

## Core Components

### Context System

The Context is the core organizational unit of the FlowIoC system:

```csharp
public class Context : IContext
{
    public MediationBinder MediationBinder { get; set; }
    public InjectionBinder InjectionBinder { get; set; }
    public InjectionBinderCrossContext InjectionBinderCrossContext { get; set; }
    public ICommandBinder CommandBinder { get; set; }
    public List<IContext> SubContexts { get; set; }
    
    // Lifecycle methods
    public virtual void Start() { /* ... */ }
    public virtual void SignalBindings() { /* ... */ }
    public virtual void InjectionBindings() { /* ... */ }
    public virtual void MediationBindings() { /* ... */ }
    public virtual void CommandBindings() { /* ... */ }
    public virtual void PostBindings() { /* ... */ }
    public virtual void Setup() { /* ... */ }
    public virtual void Launch() { /* ... */ }
    public virtual void DestroyContext() { /* ... */ }
    
    // Implementation details...
}
```

The context lifecycle follows a predictable sequence:
1. **Initialize**: Set up the context with its dependencies
2. **Start**: Perform core bindings
3. **Binding Methods**: Register specific components (signals, injections, etc.)
4. **Setup**: Prepare the context for use
5. **Launch**: Begin the application flow
6. **DestroyContext**: Clean up when the context is no longer needed

### Injection System

The `InjectionBinder` manages dependency registration and resolution:

```csharp
// From InjectionBinder.cs
public TBindingType Bind<TBindingType>(string name = "")
    where TBindingType : new()
{
    FlowConsole.Log(ConsoleLogType.Injection, _bindedContext.GetType().Name + " | Binding: " + typeof(TBindingType).Name + (name != "" ? (" Name: " + name) : ""));
    return GetOrCreateInstance<TBindingType>(name);
}

public TAbstract Bind<TAbstract, TConcrete>(string name = "")
    where TConcrete : TAbstract, new()
{
    FlowConsole.Log(ConsoleLogType.Injection, _bindedContext.GetType().Name + " | Binding: " + typeof(TAbstract).Name + (name != "" ? (" Name: " + name) : ""));
    return GetOrCreateInstance<TAbstract, TConcrete>(name);
}
```

### Signal System

The signal system provides a type-safe messaging mechanism:

```csharp
// From Signal.cs
public class Signal : SignalBody, ISignal
{
    private event Action _callbackOnce;
    private event Action _callback;
    
    public void AddListenerOnce(Action listener)
    {
        _callbackOnce += listener;
    }
    
    public void AddListener(Action listener)
    {
        _callback += listener;
    }

    public void RemoveListener(Action listener)
    {
        _callback -= listener;
    }

    public void Dispatch()
    {
        _callbackOnce?.Invoke();
        _callbackOnce = null;
        
        _internalCallback?.Invoke(this, null);
        _callback?.Invoke();
    }
}
```

### Command System

The command system allows for encapsulated logic execution:

```csharp
// From CommandBinder.cs
public virtual CommandBinding Bind<TSignal>(TSignal key)
    where TSignal : ISignalBody
{
    key.InternalCallback = null;
    key.InternalCallback += SignalDispatcher;
    CommandBinding binding = base.Bind(key);
    binding.SetContext(Context);
    return binding;
}

private void SignalDispatcher(ISignalBody signal, params object[] commandParameters)
{
    ICommandBinding binding = GetBinding(signal);
    if (binding == null)
        return;

    ICommandSequencer sequence = GetAvailableSequence();
    sequence.Initialize(binding, this, commandParameters);

    sequence.SequenceFinished += ReturnSequenceToPool;
    
    _activeSequences.Add(sequence);
    FlowConsole.Log(ConsoleLogType.Command, $"Signal Dispatched: {signal.Name}");
    sequence.RunCommands();
}
```

## Dependency Injection

### InjectionBinder

The `InjectionBinder` is responsible for creating, storing, and injecting dependencies:

```csharp
// Basic usage
public class GameContext : Context
{
    protected override void InjectionBindings()
    {
        // Bind a concrete implementation
        InjectionBinder.Bind<GameManager>();
        
        // Bind an interface to a concrete implementation
        InjectionBinder.Bind<IScoreManager, ScoreManager>();
        
        // Bind an existing instance
        InjectionBinder.BindInstance(_existingManager);
        
        // Bind a MonoBehaviour instance
        InjectionBinder.BindMonoBehaviorInstance<IGameUI, GameUIImplementation>();
    }
}
```

### Binding Types

The system supports multiple binding types to handle different scenarios:

1. **Type Binding**: Bind a concrete type for injection
   ```csharp
   InjectionBinder.Bind<PlayerManager>();
   ```

2. **Interface Binding**: Bind an interface to a concrete implementation
   ```csharp
   InjectionBinder.Bind<IPlayerManager, PlayerManager>();
   ```

3. **Mock Binding**: Bind an interface to a concrete or mock implementation based on runtime conditions
   ```csharp
   InjectionBinder.Bind<INetworkManager, NetworkManager, MockNetworkManager>();
   ```

4. **Named Binding**: Bind multiple implementations of the same interface with different names
   ```csharp
   InjectionBinder.Bind<IWeapon, Sword>("melee");
   InjectionBinder.Bind<IWeapon, Bow>("ranged");
   ```

5. **Instance Binding**: Bind an existing instance
   ```csharp
   InjectionBinder.BindInstance(existingManager);
   ```

### Cross-Context Injection

The system supports injection across multiple contexts:

```csharp
// From Context.cs CoreBindings
InjectionBinderCrossContext = new InjectionBinderCrossContext();
InjectionBinderCrossContext.SetBindedContext(this);
InjectionBinderCrossContext.BindInstance(InjectionBinderCrossContext);

// Binding a cross-context dependency
InjectionBinderCrossContext.Bind<ISharedService, SharedService>();

// Using a cross-context dependency
public class SomeComponent
{
    [Inject] private ISharedService _sharedService;
    
    // Use _sharedService...
}
```

## Signals and Commands

### Signal Implementation

Signals are a type-safe messaging system:

```csharp
// Signal with no parameters
public Signal GameStarted = new();

// Signal with parameters
public Signal<int> ScoreChanged = new();
public Signal<Player, Enemy> PlayerHit = new();
public Signal<Vector3, float, bool, string> ComplexEvent = new();

// Usage
_signals.GameStarted.Dispatch();
_signals.ScoreChanged.Dispatch(100);
_signals.PlayerHit.Dispatch(player, enemy);
_signals.ComplexEvent.Dispatch(position, duration, isEnabled, "data");
```

### Command Binding

Commands can be bound to signals for automatic execution:

```csharp
// Command implementation
public class StartGameCommand : Command
{
    [Inject] private IGameManager _gameManager;
    
    public override void Execute()
    {
        _gameManager.StartGame();
        // When command is complete
        Release();
    }
}

// Command binding
CommandBinder.Bind(_signals.GameStarted).To<StartGameCommand>();

// Multi-command binding
CommandBinder.Bind(_signals.PlayerHit)
    .To<LogHitCommand>()
    .To<UpdateHealthCommand>()
    .To<CheckGameOverCommand>();
```

### Command Sequencing

Commands can be executed in sequence:

```csharp
// Sequential command execution
CommandBinder.Bind(_signals.StartLevel)
    .To<LoadLevelDataCommand>()
    .To<InitializePlayersCommand>()
    .To<StartCountdownCommand>()
    .To<EnablePlayerControlsCommand>()
    .InSequence();

// Asynchronous commands in sequence
public class LoadLevelDataCommand : Command
{
    [Inject] private ILevelLoader _levelLoader;
    
    public override async void Execute()
    {
        await _levelLoader.LoadLevelDataAsync("level1");
        Release();
    }
}
```

## View-Mediator Pattern

### Views

Views are Unity components that represent the visual elements:

```csharp
public class PlayerView : MonoBehaviour, IView
{
    [SerializeField] private Image _healthBar;
    [SerializeField] private Text _nameText;
    
    public void SetHealth(float healthPercentage)
    {
        _healthBar.fillAmount = healthPercentage;
    }
    
    public void SetName(string playerName)
    {
        _nameText.text = playerName;
    }
}
```

### Mediators

Mediators connect views to the application logic:

```csharp
public class PlayerMediator : Mediator
{
    [Inject] private IPlayerModel _playerModel;
    [Inject] private GameSignals _signals;
    
    private PlayerView _view;
    
    public override void OnRegister()
    {
        _view = View as PlayerView;
        
        // Set up view with model data
        _view.SetName(_playerModel.PlayerName);
        _view.SetHealth(_playerModel.HealthPercentage);
        
        // Listen for model changes
        _signals.PlayerHealthChanged.AddListener(OnPlayerHealthChanged);
    }
    
    private void OnPlayerHealthChanged(float healthPercentage)
    {
        _view.SetHealth(healthPercentage);
    }
    
    public override void OnRemove()
    {
        _signals.PlayerHealthChanged.RemoveListener(OnPlayerHealthChanged);
    }
}
```

### Mediation Binding

The MediationBinder connects views to their mediators:

```csharp
// In your context
protected override void MediationBindings()
{
    MediationBinder.Bind<PlayerView>().To<PlayerMediator>();
    MediationBinder.Bind<InventoryView>().To<InventoryMediator>();
    MediationBinder.Bind<MainMenuView>().To<MainMenuMediator>();
}
```

## Context Management

### Context Hierarchy

Contexts can be organized in a hierarchy:

```csharp
public class AppContext : Context
{
    protected override void CoreBindings()
    {
        base.CoreBindings();
        
        // Add sub-contexts
        AddContext<GameContext>();
        AddContext<UIContext>();
        AddContext<AudioContext>();
    }
}
```

### Context Lifecycle

Context lifecycle methods are called in a specific order:

```
Initialize → Start → CoreBindings → SignalBindings → InjectionBindings →
MediationBindings → CommandBindings → PostBindings → Setup → Launch
```

Each method has a specific purpose:

```csharp
public class GameContext : Context
{
    protected override void CoreBindings()
    {
        base.CoreBindings();
        // Set up core systems
    }
    
    public override void SignalBindings()
    {
        // Register signals
    }
    
    public override void InjectionBindings()
    {
        // Register dependencies
    }
    
    public override void MediationBindings()
    {
        // Bind views to mediators
    }
    
    public override void CommandBindings()
    {
        // Bind signals to commands
    }
    
    public override void PostBindings()
    {
        // Final setup after all bindings
    }
    
    public override void Setup()
    {
        // Prepare for launch
    }
    
    public override void Launch()
    {
        // Start the application flow
    }
}
```

### Multi-Context Applications

Complex applications can use multiple contexts for different areas:

```csharp
// App structure
AppRoot
  ├── MainContext
  │     ├── UIContext
  │     └── AudioContext
  ├── GameContext
  │     ├── LevelContext
  │     └── CharacterContext
  └── NetworkContext
```

Communication between contexts is handled through cross-context injection and signals.

## Memory Management

### Pool System

The system automatically pools commands and other objects for better performance:

```csharp
// From CommandBinder.cs
internal ICommandBody GetCommand(Type commandType)
{
    if (_commandPool.TryGetValue(commandType, out var stack) && stack.Count > 0)
    {
        return stack.Pop();
    }
    return (ICommandBody)Activator.CreateInstance(commandType);
}

internal void ReturnCommandToPool(ICommandBody commandBody)
{
    commandBody.Clean();
    Type commandType = commandBody.GetType();

    if (!_commandPool.TryGetValue(commandType, out Stack<ICommandBody> stack))
    {
        stack = new Stack<ICommandBody>();
        _commandPool.Add(commandType, stack);
    }

    stack.Push(commandBody);
}
```

### Binding Disposal

The system properly cleans up bindings when they are no longer needed:

```csharp
// From InjectionBinder.cs
public virtual void UnBind<TBindingType>(string name = "")
{
    bool hasBindingExist = HasInstanceExist<TBindingType>(name);
    if (!hasBindingExist)
        return;

    UnBind(typeof(TBindingType), name);
}

protected void UnBind(Type key, string name = "")
{
    InjectionBinding injectionBinding = GetInjectionBinding(key, name);
    DeconstructUtils.ExecuteDeconstructMethod(injectionBinding.Value);
    _container[key].Remove(injectionBinding);
    _bindingPoolController.ReturnBindingToPool(injectionBinding);

    FlowConsole.Log(ConsoleLogType.Injection, "Unbinding: " + key.Name + (name != "" ? (" Name: " + name) : ""));
}
```

### Automatic Cleanup

When a context is destroyed, all its bindings are automatically cleaned up:

```csharp
// From Context.cs
public virtual void DestroyContext()
{
    ContextStarted = false;

    MediationBinder?.UnBindAll();
    CommandBinder?.UnBindAll();
    InjectionBinder?.UnBindAll();
}
```

## Practical Examples

### Basic Application Setup

```csharp
// App entry point
public class AppRoot : MonoBehaviour
{
    private AppContext _appContext;
    
    private void Awake()
    {
        _appContext = new AppContext();
        _appContext.Initialize(gameObject, 0, new InjectionBinderCrossContext(), new List<IContext>());
        _appContext.Start();
        
        // Run binding methods
        _appContext.SignalBindings();
        _appContext.InjectionBindings();
        _appContext.MediationBindings();
        _appContext.CommandBindings();
        _appContext.PostBindings();
        
        // Start the application
        _appContext.Setup();
        _appContext.Launch();
    }
    
    private void OnDestroy()
    {
        _appContext.DestroyContext();
    }
}

// App context
public class AppContext : Context
{
    protected override void InjectionBindings()
    {
        InjectionBinder.Bind<IGameManager, GameManager>();
        InjectionBinder.Bind<IUserManager, UserManager>();
        InjectionBinder.Bind<ISettingsManager, SettingsManager>();
    }
    
    protected override void SignalBindings()
    {
        InjectionBinder.Bind<AppSignals>();
    }
    
    protected override void CommandBindings()
    {
        AppSignals signals = InjectionBinder.GetInstance<AppSignals>();
        
        CommandBinder.Bind(signals.AppStarted).To<InitializeAppCommand>();
        CommandBinder.Bind(signals.LoginRequested).To<LoginCommand>();
    }
    
    public override void Launch()
    {
        AppSignals signals = InjectionBinder.GetInstance<AppSignals>();
        signals.AppStarted.Dispatch();
    }
}
```

### Advanced Configurations

```csharp
// Game context with multiple sub-systems
public class GameContext : Context
{
    protected override void CoreBindings()
    {
        base.CoreBindings();
        
        AddContext<LevelContext>();
        AddContext<CharacterContext>();
        AddContext<AIContext>();
    }
    
    protected override void InjectionBindings()
    {
        // Game systems
        InjectionBinder.Bind<IGameState, GameState>();
        InjectionBinder.Bind<ILevelManager, LevelManager>();
        
        // Register services
        InjectionBinderCrossContext.Bind<ISaveSystem, SaveSystem>();
        InjectionBinderCrossContext.Bind<IInputManager, InputManager>();
        
        // Create a named binding for different difficulty settings
        InjectionBinder.Bind<IDifficultySettings, EasyDifficultySettings>("easy");
        InjectionBinder.Bind<IDifficultySettings, HardDifficultySettings>("hard");
    }
    
    protected override void CommandBindings()
    {
        GameSignals signals = InjectionBinder.GetInstance<GameSignals>();
        
        // Basic command binding
        CommandBinder.Bind(signals.GameStarted).To<StartGameCommand>();
        
        // Sequential commands
        CommandBinder.Bind(signals.LevelCompleted)
            .To<SaveProgressCommand>()
            .To<CalculateRewardsCommand>()
            .To<ShowLevelCompleteScreenCommand>()
            .InSequence();
            
        // Parallel commands
        CommandBinder.Bind(signals.GamePaused)
            .To<PauseGameTimeCommand>()
            .To<PausePhysicsCommand>()
            .To<ShowPauseMenuCommand>();
    }
}
```

### Testing with Mocks

```csharp
// Mock binding for testing
public class TestGameContext : GameContext
{
    public override void InjectionBindings()
    {
        // Core game systems
        InjectionBinder.Bind<IGameState, GameState>();
        
        // Mock implementations for testing
        InjectionBinder.Bind<INetworkManager, MockNetworkManager>();
        InjectionBinder.Bind<ISaveSystem, MockSaveSystem>();
        InjectionBinder.Bind<ILevelLoader, MockLevelLoader>();
        
        // Named test configuration
        InjectionBinder.BindInstance<ITestConfig>(new TestConfig { RunHeadless = true }, "main");
    }
}

// Using the test context
public class TestRunner
{
    public void RunTest()
    {
        TestGameContext context = new TestGameContext();
        context.Initialize(new GameObject(), 0, new InjectionBinderCrossContext(), new List<IContext>(), true);
        context.Start();
        
        // Run bindings and setup
        
        // Run test
        GameSignals signals = context.InjectionBinder.GetInstance<GameSignals>();
        signals.GameStarted.Dispatch();
        
        // Verify results
        
        // Clean up
        context.DestroyContext();
    }
}
```

## Performance Optimization

### Best Practices

1. **Context Organization**
   - Group related functionality in the same context
   - Use sub-contexts for modular components
   - Keep cross-context dependencies to a minimum

2. **Binding Optimization**
   - Bind only what you need
   - Use appropriate binding types (concrete, interface, named)
   - Consider the lifetime of your bindings

3. **Signal Efficiency**
   - Keep signal parameters minimal
   - Remove listeners when they're no longer needed
   - Use AddListenerOnce for one-time events

### Memory Management Tips

1. **Command Pooling**
   - Commands are automatically pooled
   - Clean up resources in the Clean method
   - Don't store persistent state in commands

2. **Context Cleanup**
   - Always call DestroyContext when a context is no longer needed
   - Remove signal listeners in OnRemove
   - Implement IDeconstruct for custom cleanup

3. **Avoiding Memory Leaks**
   - Don't create circular references
   - Use weak references for long-lived objects
   - Monitor memory usage with the Unity Profiler

## Internal Implementation Details

### Binding Process

The binding process follows these steps:

1. **Creation/Registration**
   ```
   Bind<T>() → Check for existing → Create new instance → Register in container
   ```

2. **Injection**
   ```
   InjectAllInstances() → For each binding → Find injectable fields → Inject dependencies
   ```

3. **Post-Construction**
   ```
   ExecutePostConstructMethods() → For each binding → Find [PostConstruct] methods → Execute
   ```

### Command Execution Flow

The command execution flow:

1. **Signal Dispatch**
   ```
   Dispatch() → SignalDispatcher() → GetBinding() → CreateSequence()
   ```

2. **Command Creation/Retrieval**
   ```
   GetCommand() → Check pool → Create or reuse → Inject → Execute
   ```

3. **Command Completion**
   ```
   Execute() → ... → Release() → Next command or finish sequence
   ```

4. **Cleanup**
   ```
   ReturnCommandToPool() → Clean → Return to pool
   ```

### Memory Management Implementation

The memory management system is implemented across several components:

1. **Binding Pool**
   ```csharp
   // From BindingPoolController
   public T GetAvailableBinding<T>() where T : IBinding, new()
   {
       if (!_bindingPools.TryGetValue(typeof(T), out Stack<IBinding> availableBindings))
       {
           availableBindings = new Stack<IBinding>();
           _bindingPools.Add(typeof(T), availableBindings);
       }

       if (availableBindings.Count > 0)
           return (T)availableBindings.Pop();

       return new T();
   }

   public void ReturnBindingToPool(IBinding binding)
   {
       if (binding == null)
           return;

       Type bindingType = binding.GetType();
           
       if (!_bindingPools.TryGetValue(bindingType, out Stack<IBinding> availableBindings))
       {
           availableBindings = new Stack<IBinding>();
           _bindingPools.Add(bindingType, availableBindings);
       }

       binding.Clean();
       availableBindings.Push(binding);
   }
   ```

2. **Command Pool**
   ```csharp
   // From CommandBinder.cs
   internal ICommandBody GetCommand(Type commandType)
   {
       if (_commandPool.TryGetValue(commandType, out var stack) && stack.Count > 0)
       {
           return stack.Pop();
       }
       return (ICommandBody)Activator.CreateInstance(commandType);
   }

   internal void ReturnCommandToPool(ICommandBody commandBody)
   {
       commandBody.Clean();
       Type commandType = commandBody.GetType();

       if (!_commandPool.TryGetValue(commandType, out Stack<ICommandBody> stack))
       {
           stack = new Stack<ICommandBody>();
           _commandPool.Add(commandType, stack);
       }

       stack.Push(commandBody);
   }
   ```

3. **Context Cleanup**
   ```csharp
   // From Context.cs
   public virtual void DestroyContext()
   {
       ContextStarted = false;

       MediationBinder?.UnBindAll();
       CommandBinder?.UnBindAll();
       InjectionBinder?.UnBindAll();
   }
   