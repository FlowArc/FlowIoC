# FlowIoC Base Module - User Manual

## Table of Contents
1. [Getting Started](#getting-started)
   - [Installation](#installation)
   - [Initial Setup](#initial-setup)
   - [Basic Concepts](#basic-concepts)
2. [Working with Contexts](#working-with-contexts)
   - [Creating Contexts](#creating-contexts)
   - [Context Hierarchy](#context-hierarchy)
   - [Context Lifecycle](#context-lifecycle)
3. [Dependency Injection](#dependency-injection)
   - [Injection Binding](#injection-binding)
   - [Using Injected Dependencies](#using-injected-dependencies)
   - [Named Bindings](#named-bindings)
   - [Cross-Context Injection](#cross-context-injection)
4. [Signals and Commands](#signals-and-commands)
   - [Creating Signals](#creating-signals)
   - [Signal Parameters](#signal-parameters)
   - [Binding Commands to Signals](#binding-commands-to-signals)
   - [Creating and Executing Commands](#creating-and-executing-commands)
5. [View-Mediator Pattern](#view-mediator-pattern)
   - [Setting Up Views](#setting-up-views)
   - [Creating Mediators](#creating-mediators)
   - [Binding Views to Mediators](#binding-views-to-mediators)
6. [Advanced Features](#advanced-features)
   - [Sequential Commands](#sequential-commands)
   - [Command Pooling](#command-pooling)
   - [Testing with Mocks](#testing-with-mocks)
   - [Signal Handling Best Practices](#signal-handling-best-practices)
7. [Memory Management](#memory-management)
   - [Understanding Binding Pools](#understanding-binding-pools)
   - [Cleaning Up Resources](#cleaning-up-resources)
   - [Avoiding Memory Leaks](#avoiding-memory-leaks)
8. [Error Handling](#error-handling)
   - [Common Errors](#common-errors)
   - [Debugging Tips](#debugging-tips)
9. [Performance Optimization](#performance-optimization)
   - [Best Practices](#best-practices)
   - [Context Organization](#context-organization)
10. [Examples](#examples)
    - [Basic Application](#basic-application)
    - [Game with Multiple Contexts](#game-with-multiple-contexts)
    - [UI System Integration](#ui-system-integration)

## Getting Started

### Installation

1. Add the FlowIoC package to your Unity project:
   - Via Package Manager: Add package from git URL `https://github.com/your-repository/FlowIoC.git`
   - Or import the FlowIoC unitypackage directly

2. After installation, verify the following folders exist in your project:
   - `Packages/FlowIoC/Runtime/BaseModule`
   - `Packages/FlowIoC/Editor/BaseModule`

### Initial Setup

To use the Base Module in your project, you need to initialize it properly:

1. Create a root MonoBehaviour that will serve as the entry point:

```csharp
using FlowIoC.BaseModule.Contexts;
using UnityEngine;
using System.Collections.Generic;

public class AppRoot : MonoBehaviour
{
    private AppContext _appContext;
    
    private void Awake()
    {
        // Create and initialize your main context
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
        // Clean up when the app is destroyed
        _appContext.DestroyContext();
    }
}
```

2. Create your main application context:

```csharp
using FlowIoC.BaseModule.Contexts;

public class AppContext : Context
{
    protected override void CoreBindings()
    {
        base.CoreBindings();
        
        // Add your core bindings here
    }
    
    public override void Launch()
    {
        // This is where your application starts
        Debug.Log("Application launched!");
    }
}
```

### Basic Concepts

Before diving deeper, understand these key concepts:

- **Context**: The main organizational unit that manages dependencies and lifecycle
- **Injection**: The process of providing dependencies to objects that need them
- **Signal**: A type-safe message that can be dispatched to notify other parts of the application
- **Command**: A self-contained piece of logic that can be executed when a signal is dispatched
- **Mediator**: A component that connects UI views with the application logic
- **View**: A MonoBehaviour that represents a UI component

## Working with Contexts

### Creating Contexts

Contexts are the backbone of your application structure. Create a context by extending the base `Context` class:

```csharp
using FlowIoC.BaseModule.Contexts;

public class GameContext : Context
{
    // Override lifecycle methods as needed
    protected override void CoreBindings()
    {
        base.CoreBindings();
        // Add core bindings specific to the game
    }
    
    public override void InjectionBindings()
    {
        // Register your game's dependencies
        InjectionBinder.Bind<IGameManager, GameManager>();
        InjectionBinder.Bind<IPlayerController, PlayerController>();
    }
    
    public override void SignalBindings()
    {
        // Register your signal containers
        InjectionBinder.Bind<GameSignals>();
    }
    
    public override void CommandBindings()
    {
        // Bind signals to commands
        GameSignals signals = InjectionBinder.GetInstance<GameSignals>();
        CommandBinder.Bind(signals.GameStarted).To<StartGameCommand>();
    }
    
    public override void Launch()
    {
        // Start your game logic
        GameSignals signals = InjectionBinder.GetInstance<GameSignals>();
        signals.GameStarted.Dispatch();
    }
}
```

### Context Hierarchy

For more complex applications, organize contexts in a hierarchy:

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

To add a context as a sub-context:

```csharp
private void AddContext<T>() where T : IContext, new()
{
    T context = new T();
    context.Initialize(gameObject, SubContexts.Count + 1, InjectionBinderCrossContext, SubContexts);
    context.Start();
    
    // Run binding methods
    context.SignalBindings();
    context.InjectionBindings();
    context.MediationBindings();
    context.CommandBindings();
    context.PostBindings();
    
    // Setup and launch
    context.Setup();
    context.Launch();
    
    SubContexts.Add(context);
}
```

### Context Lifecycle

Contexts follow a specific lifecycle:

1. **Initialize**: Set up the context with its dependencies
   ```csharp
   context.Initialize(gameObject, 0, injectionBinderCrossContext, subContexts);
   ```

2. **Start**: Perform core bindings
   ```csharp
   context.Start();
   ```

3. **Binding Methods**: Register components in a specific order
   ```csharp
   context.SignalBindings();
   context.InjectionBindings();
   context.MediationBindings();
   context.CommandBindings();
   context.PostBindings();
   ```

4. **Setup**: Prepare the context for use
   ```csharp
   context.Setup();
   ```

5. **Launch**: Begin the application flow
   ```csharp
   context.Launch();
   ```

6. **DestroyContext**: Clean up when the context is no longer needed
   ```csharp
   context.DestroyContext();
   ```

## Dependency Injection

### Injection Binding

Register dependencies in the `InjectionBindings` method of your context:

```csharp
public override void InjectionBindings()
{
    // Bind a concrete type
    InjectionBinder.Bind<GameManager>();
    
    // Bind an interface to a concrete implementation
    InjectionBinder.Bind<IScoreManager, ScoreManager>();
    
    // Bind with a name for multiple implementations
    InjectionBinder.Bind<IWeapon, Sword>("melee");
    InjectionBinder.Bind<IWeapon, Bow>("ranged");
    
    // Bind an existing instance
    PlayerController playerController = new PlayerController();
    InjectionBinder.BindInstance(playerController);
    
    // Bind a MonoBehaviour
    InjectionBinder.BindMonoBehaviorInstance<IGameUI, GameUIImplementation>();
}
```

### Using Injected Dependencies

Use the `[Inject]` attribute to receive dependencies:

```csharp
using FlowIoC.BaseModule.Injectable.Attributes;

public class PlayerController
{
    [Inject] private IInputManager _inputManager;
    [Inject] private IPlayerModel _playerModel;
    [Inject("melee")] private IWeapon _meleeWeapon;
    [Inject("ranged")] private IWeapon _rangedWeapon;
    
    // Optional: Method called after injection
    [PostConstruct]
    public void Initialize()
    {
        Debug.Log("PlayerController initialized with all dependencies");
    }
    
    // Optional: Method called before object is destroyed
    [Deconstruct]
    public void Cleanup()
    {
        Debug.Log("PlayerController being destroyed");
    }
}
```

### Named Bindings

For multiple implementations of the same interface, use named bindings:

```csharp
// In your context
InjectionBinder.Bind<IWeapon, Sword>("melee");
InjectionBinder.Bind<IWeapon, Bow>("ranged");

// In your class
[Inject("melee")] private IWeapon _meleeWeapon;
[Inject("ranged")] private IWeapon _rangedWeapon;
```

### Cross-Context Injection

For dependencies that should be available across multiple contexts, use cross-context injection:

```csharp
// In your root context
InjectionBinderCrossContext.Bind<IAudioService, AudioService>();
InjectionBinderCrossContext.Bind<IInputManager, InputManager>();

// In any class in any context
[Inject] private IAudioService _audioService;
[Inject] private IInputManager _inputManager;
```

## Signals and Commands

### Creating Signals

Create a signal container class:

```csharp
using FlowIoC.BaseModule.Signals;

public class GameSignals : ISignalHolder
{
    // Simple signal with no parameters
    public Signal GameStarted = new();
    
    // Signal with one parameter
    public Signal<int> ScoreChanged = new();
    
    // Signal with two parameters
    public Signal<Player, Enemy> PlayerHit = new();
    
    // Signal with three parameters
    public Signal<Vector3, float, string> ObjectSpawned = new();
    
    // Signal with four parameters
    public Signal<int, string, bool, float> ComplexEvent = new();
}
```

Register your signals in the context:

```csharp
public override void SignalBindings()
{
    InjectionBinder.Bind<GameSignals>();
}
```

### Signal Parameters

Signals support up to four parameters:

```csharp
// Dispatch signals with different parameter counts
_gameSignals.GameStarted.Dispatch();
_gameSignals.ScoreChanged.Dispatch(100);
_gameSignals.PlayerHit.Dispatch(player, enemy);
_gameSignals.ObjectSpawned.Dispatch(position, size, "Enemy");
_gameSignals.ComplexEvent.Dispatch(1, "Player", true, 0.5f);
```

### Binding Commands to Signals

Bind commands to signals in your context's `CommandBindings` method:

```csharp
public override void CommandBindings()
{
    GameSignals signals = InjectionBinder.GetInstance<GameSignals>();
    
    // Basic binding
    CommandBinder.Bind(signals.GameStarted).To<StartGameCommand>();
    
    // Multiple commands for one signal
    CommandBinder.Bind(signals.PlayerHit)
        .To<LogPlayerHitCommand>()
        .To<UpdateHealthCommand>()
        .To<CheckGameOverCommand>();
    
    // Sequential execution
    CommandBinder.Bind(signals.LevelCompleted)
        .To<SaveProgressCommand>()
        .To<CalculateRewardsCommand>()
        .To<ShowLevelCompleteScreenCommand>()
        .InSequence();
}
```

### Creating and Executing Commands

Create commands by extending the `Command` class:

```csharp
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

public class StartGameCommand : Command
{
    [Inject] private IGameManager _gameManager;
    
    public override void Execute()
    {
        // Execute your command logic
        _gameManager.StartGame();
        
        // Important: Call Release when the command is complete
        Release();
    }
}

// Command with parameters
public class UpdateHealthCommand : Command<Player, Enemy>
{
    [Inject] private IHealthSystem _healthSystem;
    
    public override void Execute(Player player, Enemy enemy)
    {
        int damage = enemy.AttackPower;
        _healthSystem.ApplyDamage(player, damage);
        
        Release();
    }
}
```

For asynchronous commands:

```csharp
public class LoadLevelCommand : Command<string>
{
    [Inject] private ILevelLoader _levelLoader;
    
    public override async void Execute(string levelName)
    {
        // Asynchronous operation
        await _levelLoader.LoadLevelAsync(levelName);
        
        // Important: Only release after the async operation completes
        Release();
    }
}
```

## View-Mediator Pattern

### Setting Up Views

Create view components that inherit from `MonoBehaviour` and implement `IView`:

```csharp
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthView : MonoBehaviour, IView
{
    [SerializeField] private Image _healthBar;
    [SerializeField] private Text _healthText;
    
    public void UpdateHealth(float healthPercentage, int currentHealth, int maxHealth)
    {
        _healthBar.fillAmount = healthPercentage;
        _healthText.text = $"{currentHealth}/{maxHealth}";
    }
}
```

Add a `ViewInjector` component to your view GameObject in the Unity Editor.

### Creating Mediators

Create mediators that connect views to your application logic:

```csharp
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;

public class PlayerHealthMediator : Mediator
{
    [Inject] private IPlayerModel _playerModel;
    [Inject] private GameSignals _gameSignals;
    
    private PlayerHealthView _view;
    
    public override void OnRegister()
    {
        _view = View as PlayerHealthView;
        
        // Set up initial state
        UpdateHealthDisplay();
        
        // Listen for changes
        _gameSignals.PlayerHealthChanged.AddListener(OnPlayerHealthChanged);
    }
    
    private void OnPlayerHealthChanged(int newHealth)
    {
        UpdateHealthDisplay();
    }
    
    private void UpdateHealthDisplay()
    {
        float healthPercentage = (float)_playerModel.CurrentHealth / _playerModel.MaxHealth;
        _view.UpdateHealth(healthPercentage, _playerModel.CurrentHealth, _playerModel.MaxHealth);
    }
    
    public override void OnRemove()
    {
        // Clean up listeners to prevent memory leaks
        _gameSignals.PlayerHealthChanged.RemoveListener(OnPlayerHealthChanged);
    }
}
```

### Binding Views to Mediators

In your context, bind views to mediators:

```csharp
public override void MediationBindings()
{
    MediationBinder.Bind<PlayerHealthView>().To<PlayerHealthMediator>();
    MediationBinder.Bind<InventoryView>().To<InventoryMediator>();
    MediationBinder.Bind<MainMenuView>().To<MainMenuMediator>();
}
```

## Advanced Features

### Sequential Commands

To execute commands in sequence, use the `InSequence` method:

```csharp
CommandBinder.Bind(_signals.StartLevel)
    .To<LoadLevelDataCommand>()
    .To<InitializePlayersCommand>()
    .To<StartCountdownCommand>()
    .To<EnablePlayerControlsCommand>()
    .InSequence();
```

In sequential commands, the next command only starts after the current one calls `Release()`:

```csharp
public class LoadLevelDataCommand : Command<string>
{
    [Inject] private ILevelLoader _levelLoader;
    
    public override async void Execute(string levelName)
    {
        await _levelLoader.LoadLevelAsync(levelName);
        Release(); // Next command starts after this
    }
}
```

### Command Pooling

Commands are automatically pooled for better performance. Ensure proper cleanup:

```csharp
public class SpawnObjectsCommand : Command
{
    private List<GameObject> _tempObjects = new List<GameObject>();
    
    public override void Execute()
    {
        // Use temporary variables as needed
        _tempObjects.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
        
        // Complete the command
        Release();
    }
    
    // Clean up when returning to the pool
    public override void Clean()
    {
        // Clear any temporary variables
        _tempObjects.Clear();
    }
}
```

### Testing with Mocks

For testing, create mock implementations and use the special binding method:

```csharp
// Production binding
InjectionBinder.Bind<INetworkManager, RealNetworkManager>();

// Test binding with mock
InjectionBinder.Bind<INetworkManager, RealNetworkManager, MockNetworkManager>();
```

When running in test mode, the mock implementation will be used:

```csharp
// Initialize with test flag
context.Initialize(gameObject, 0, new InjectionBinderCrossContext(), new List<IContext>(), true);
```

### Signal Handling Best Practices

Follow these best practices for signal handling:

1. **Keep signals organized** in signal holder classes:
   ```csharp
   public class UISignals : ISignalHolder
   {
       public Signal MenuOpened = new();
       public Signal<string> DialogShown = new();
   }
   
   public class GameplaySignals : ISignalHolder
   {
       public Signal GameStarted = new();
       public Signal GamePaused = new();
   }
   ```

2. **Clean up signal listeners** to prevent memory leaks:
   ```csharp
   public override void OnRemove()
   {
       _signals.PlayerHealthChanged.RemoveListener(OnPlayerHealthChanged);
       _signals.GameOver.RemoveListener(OnGameOver);
   }
   ```

3. **Use AddListenerOnce** for one-time events:
   ```csharp
   _signals.LevelCompleted.AddListenerOnce(OnLevelCompletedFirstTime);
   ```

## Memory Management

### Understanding Binding Pools

The FlowIoC system pools various objects for better performance:

1. **Command Pooling**: Commands are automatically pooled and reused
2. **Binding Pooling**: Binding objects are pooled to reduce allocation

These pools are managed automatically, but you should follow best practices to take full advantage of them.

### Cleaning Up Resources

Properly clean up resources when your application or context is destroyed:

```csharp
// In your MonoBehaviour
private void OnDestroy()
{
    _appContext.DestroyContext();
}

// In your custom objects, implement the Deconstruct method
[Deconstruct]
public void Cleanup()
{
    // Release any resources
    _disposables.ForEach(d => d.Dispose());
    _disposables.Clear();
}
```

### Avoiding Memory Leaks

Common sources of memory leaks and how to avoid them:

1. **Signal Listeners**: Always remove signal listeners when they're no longer needed
   ```csharp
   public override void OnRemove()
   {
       _signals.PlayerHealthChanged.RemoveListener(OnPlayerHealthChanged);
   }
   ```

2. **Circular References**: Avoid creating circular references between objects
   ```csharp
   // Bad - circular reference
   class A { B b; }
   class B { A a; }
   
   // Better - one-way reference
   class A { B b; }
   class B { }
   ```

3. **Unmanaged Resources**: Always dispose of unmanaged resources
   ```csharp
   public class ResourceLoader : IDisposable
   {
       private IntPtr _nativeResource;
       
       [Deconstruct]
       public void Dispose()
       {
           if (_nativeResource != IntPtr.Zero)
           {
               ReleaseNativeResource(_nativeResource);
               _nativeResource = IntPtr.Zero;
           }
       }
   }
   ```

## Error Handling

### Common Errors

1. **Missing Bindings**: Dependencies that haven't been registered
   ```
   Error: No binding found for type IPlayerManager
   ```
   
   Solution: Ensure all dependencies are properly bound in the context
   ```csharp
   InjectionBinder.Bind<IPlayerManager, PlayerManager>();
   ```

2. **Circular Dependencies**: Classes that depend on each other
   ```
   Error: Circular dependency detected between A and B
   ```
   
   Solution: Restructure your classes to avoid circular dependencies

3. **Missing Release Call**: Commands that don't call Release
   ```
   Warning: Command has been executing for 10 seconds without calling Release
   ```
   
   Solution: Always call Release when a command is complete
   ```csharp
   public override void Execute()
   {
       DoSomething();
       Release(); // Don't forget this!
   }
   ```

### Debugging Tips

1. **Enable Console Logging**:
   ```csharp
   // In development builds
   FlowConsole.SetLogLevel(ConsoleLogType.Injection, LogLevel.Verbose);
   FlowConsole.SetLogLevel(ConsoleLogType.Command, LogLevel.Verbose);
   ```

2. **Check Binding Status**:
   ```csharp
   bool hasBinding = InjectionBinder.HasInstanceExist<IPlayerManager>();
   Debug.Log($"PlayerManager binding exists: {hasBinding}");
   ```

3. **Inspect Active Commands**:
   The CommandBinder keeps track of active commands. You can add custom code to debug this:
   ```csharp
   void DebugActiveCommands()
   {
       foreach (var sequence in _activeSequences)
       {
           Debug.Log($"Active command: {sequence.CurrentCommand.GetType().Name}");
       }
   }
   ```

## Performance Optimization

### Best Practices

1. **Minimize Signal Parameters**: 
   ```csharp
   // Instead of this
   _signals.ComplexEvent.Dispatch(player, position, rotation, scale, health, mana, stamina);
   
   // Consider this
   _signals.PlayerUpdated.Dispatch(playerData); // Single parameter containing all data
   ```

2. **Use Command Pooling Effectively**:
   ```csharp
   public override void Clean()
   {
       // Clear lists instead of creating new ones
       _temporaryList.Clear();
       
       // Reset values to defaults
       _counter = 0;
       _isActive = false;
   }
   ```

3. **Optimize Binding Operations**:
   ```csharp
   // Preload commonly used objects
   protected override void CoreBindings()
   {
       base.CoreBindings();
       
       // Pre-create common objects
       InjectionBinder.Bind<GameManager>();
       InjectionBinder.Bind<PlayerManager>();
   }
   ```

### Context Organization

For optimal performance, organize your contexts efficiently:

1. **Group Related Functionality**:
   ```
   AppContext
   ├── GameContext (gameplay systems)
   ├── UIContext (all UI elements)
   └── AudioContext (sound system)
   ```

2. **Use Cross-Context Injection Sparingly**:
   ```csharp
   // Only share what's truly needed across contexts
   InjectionBinderCrossContext.Bind<IInputManager, InputManager>();
   InjectionBinderCrossContext.Bind<IAudioService, AudioService>();
   ```

3. **Separate Stable and Volatile Systems**:
   ```
   AppContext (stable)
   ├── CoreContext (stable systems like input, audio)
   └── GameContext (volatile, changes between game modes)
       ├── CampaignContext
       └── MultiplayerContext
   ```

## Examples

### Basic Application

```csharp
// AppRoot.cs
using FlowIoC.BaseModule.Contexts;
using UnityEngine;
using System.Collections.Generic;

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

// AppContext.cs
using FlowIoC.BaseModule.Contexts;

public class AppContext : Context
{
    protected override void InjectionBindings()
    {
        // Core systems
        InjectionBinder.Bind<IGameManager, GameManager>();
        InjectionBinder.Bind<IUserInterface, UserInterface>();
    }
    
    protected override void SignalBindings()
    {
        InjectionBinder.Bind<AppSignals>();
    }
    
    protected override void CommandBindings()
    {
        AppSignals signals = InjectionBinder.GetInstance<AppSignals>();
        
        CommandBinder.Bind(signals.AppStarted).To<InitializeAppCommand>();
        CommandBinder.Bind(signals.GameRequested).To<StartGameCommand>();
    }
    
    public override void Launch()
    {
        // Start the application
        AppSignals signals = InjectionBinder.GetInstance<AppSignals>();
        signals.AppStarted.Dispatch();
    }
}

// AppSignals.cs
using FlowIoC.BaseModule.Signals;

public class AppSignals : ISignalHolder
{
    public Signal AppStarted = new();
    public Signal GameRequested = new();
    public Signal<string> ErrorOccurred = new();
}

// InitializeAppCommand.cs
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

public class InitializeAppCommand : Command
{
    [Inject] private IGameManager _gameManager;
    
    public override void Execute()
    {
        _gameManager.Initialize();
        Release();
    }
}
```

### Game with Multiple Contexts

```csharp
// GameContext.cs
using FlowIoC.BaseModule.Contexts;

public class GameContext : Context
{
    protected override void CoreBindings()
    {
        base.CoreBindings();
        
        // Add gameplay-related sub-contexts
        AddContext<LevelContext>();
        AddContext<CharacterContext>();
    }
    
    protected override void InjectionBindings()
    {
        // Game systems
        InjectionBinder.Bind<IGameState, GameState>();
        InjectionBinder.Bind<ILevelManager, LevelManager>();
        InjectionBinder.Bind<ICharacterManager, CharacterManager>();
    }
    
    protected override void SignalBindings()
    {
        InjectionBinder.Bind<GameSignals>();
    }
    
    protected override void CommandBindings()
    {
        GameSignals signals = InjectionBinder.GetInstance<GameSignals>();
        
        // Basic gameplay flow
        CommandBinder.Bind(signals.GameStarted).To<InitializeGameCommand>();
        
        // Level management
        CommandBinder.Bind(signals.LevelRequested)
            .To<LoadLevelCommand>()
            .To<SpawnPlayerCommand>()
            .To<StartGameplayCommand>()
            .InSequence();
            
        // Character actions
        CommandBinder.Bind(signals.CharacterDamaged)
            .To<UpdateHealthCommand>()
            .To<PlayDamageEffectsCommand>()
            .To<CheckGameOverCommand>();
    }
    
    public override void Launch()
    {
        // Start the game
        GameSignals signals = InjectionBinder.GetInstance<GameSignals>();
        signals.GameStarted.Dispatch();
    }
}

// Character system with mediators
public class CharacterView : MonoBehaviour, IView
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Image _healthBar;
    
    public void UpdateHealth(float healthPercentage)
    {
        _healthBar.fillAmount = healthPercentage;
    }
    
    public void PlayAnimation(string animationTrigger)
    {
        _animator.SetTrigger(animationTrigger);
    }
}

public class CharacterMediator : Mediator
{
    [Inject] private ICharacterModel _characterModel;
    [Inject] private GameSignals _gameSignals;
    
    private CharacterView _view;
    
    public override void OnRegister()
    {
        _view = View as CharacterView;
        
        // Set up initial state
        UpdateView();
        
        // Listen for changes
        _gameSignals.CharacterUpdated.AddListener(OnCharacterUpdated);
    }
    
    private void OnCharacterUpdated(ICharacterModel character)
    {
        if (character == _characterModel)
        {
            UpdateView();
        }
    }
    
    private void UpdateView()
    {
        float healthPercentage = (float)_characterModel.CurrentHealth / _characterModel.MaxHealth;
        _view.UpdateHealth(healthPercentage);
    }
    
    public override void OnRemove()
    {
        _gameSignals.CharacterUpdated.RemoveListener(OnCharacterUpdated);
    }
}
```

### UI System Integration

```csharp
// UIContext.cs
using FlowIoC.BaseModule.Contexts;

public class UIContext : Context
{
    protected override void InjectionBindings()
    {
        // UI systems
        InjectionBinder.Bind<IUIManager, UIManager>();
        InjectionBinder.Bind<IWindowManager, WindowManager>();
    }
    
    protected override void SignalBindings()
    {
        InjectionBinder.Bind<UISignals>();
    }
    
    protected override void MediationBindings()
    {
        // Bind UI views to mediators
        MediationBinder.Bind<MainMenuView>().To<MainMenuMediator>();
        MediationBinder.Bind<GameHUDView>().To<GameHUDMediator>();
        MediationBinder.Bind<InventoryView>().To<InventoryMediator>();
    }
    
    protected override void CommandBindings()
    {
        UISignals signals = InjectionBinder.GetInstance<UISignals>();
        
        // UI flow
        CommandBinder.Bind(signals.OpenWindow)
            .To<PrepareWindowCommand>()
            .To<AnimateWindowCommand>();
            
        CommandBinder.Bind(signals.CloseWindow)
            .To<SaveWindowStateCommand>()
            .To<AnimateWindowCloseCommand>()
            .To<CleanupWindowCommand>()
            .InSequence();
    }
}

// MainMenuMediator.cs
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;

public class MainMenuMediator : Mediator
{
    [Inject] private UISignals _uiSignals;
    [Inject] private GameSignals _gameSignals;
    
    private MainMenuView _view;
    
    public override void OnRegister()
    {
        _view = View as MainMenuView;
        
        // Set up button listeners
        _view.PlayButton.onClick.AddListener(OnPlayClicked);
        _view.OptionsButton.onClick.AddListener(OnOptionsClicked);
        _view.QuitButton.onClick.AddListener(OnQuitClicked);
    }
    
    private void OnPlayClicked()
    {
        _uiSignals.CloseWindow.Dispatch("MainMenu");
        _gameSignals.GameRequested.Dispatch();
    }
    
    private void OnOptionsClicked()
    {
        _uiSignals.OpenWindow.Dispatch("Options");
    }
    
    private void OnQuitClicked()
    {
        _gameSignals.QuitRequested.Dispatch();
    }
    
    public override void OnRemove()
    {
        // Clean up listeners
        _view.PlayButton.onClick.RemoveListener(OnPlayClicked);
        _view.OptionsButton.onClick.RemoveListener(OnOptionsClicked);
        _view.QuitButton.onClick.RemoveListener(OnQuitClicked);
    }
}
``` 