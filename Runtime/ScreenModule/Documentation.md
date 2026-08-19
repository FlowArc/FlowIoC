# FlowIoC Screen System

## Table of Contents
1. [Introduction](#introduction)
2. [Architecture Overview](#architecture-overview)
   - [Component Relationships](#component-relationships)
   - [Dependency Injection](#dependency-injection)
   - [Signal System](#signal-system)
3. [Core Components](#core-components)
   - [ScreenService](#screenservice)
   - [SubServices](#subservices)
   - [Screen Models](#screen-models)
   - [Screen States](#screen-states)
4. [Screen Configuration](#screen-configuration)
   - [ScreenConfig](#screenconfig)
   - [Loading Types](#loading-types)
   - [Tags and Organization](#tags-and-organization)
5. [Manager & Layer System](#manager--layer-system)
   - [Manager Structure](#manager-structure) 
   - [Layer Management](#layer-management)
   - [History Navigation](#history-navigation)
6. [Memory Management](#memory-management)
   - [Pool System](#pool-system)
   - [Reference Counting](#reference-counting)
   - [Auto Cleanup](#auto-cleanup)
7. [Animation System](#animation-system)
   - [Open/Close Animations](#openclose-animations)
   - [Animation Control](#animation-control)
8. [Error Handling](#error-handling)
   - [State Validation](#state-validation)
   - [Error States](#error-states)
9. [Practical Examples](#practical-examples)
   - [Basic Screen Operations](#basic-screen-operations)
   - [Advanced Configurations](#advanced-configurations)
   - [Error Handling](#handling-errors)
10. [Performance Optimization](#performance-optimization)
    - [Best Practices](#best-practices)
    - [Memory Management Tips](#memory-management-tips)
11. [Internal Implementation Details](#internal-implementation-details)
    - [Service Registration Flow](#service-registration-flow)
    - [Screen Lifecycle](#screen-lifecycle)
    - [Memory Management Implementation](#memory-management-implementation)

## Introduction

The FlowIoC Screen System is a comprehensive, high-performance solution for managing UI screens in Unity projects. It provides a robust architecture for handling screen lifecycles, state management, animations, and memory optimization.

The system is designed with a focus on:
- **Performance**: Efficient memory usage through pooling
- **Flexibility**: Multiple loading methods and organization approaches
- **Reliability**: Robust error handling and state validation
- **Maintainability**: Clean separation of concerns through specialized services
- **Developer Experience**: Fluent API for intuitive usage

<!-- [VISUAL RECOMMENDATION: System overview diagram showing the relationship between key components] -->

## Architecture Overview

The Screen System follows a modular architecture with clear separation of concerns:

```
┌─────────────────────────────────────────┐
│              ScreenService              │
└───────────────┬────────────────────────┬┘
                │                        │
┌───────────────▼──┐       ┌─────────────▼──────────┐
│    SubServices   │◄──────┤     Screen Models      │
└──┬──────────┬────┘       └──────────────┬─────────┘
   │          │                           │
┌──▼───┐  ┌───▼──┐                 ┌──────▼──────┐
│States│  │Config│                 │Screen Views │
└──────┘  └──────┘                 └─────────────┘
```

The system is composed of three primary layers:
1. **Service Layer**: Coordinates all screen operations
2. **SubServices Layer**: Handles specialized tasks
3. **Model Layer**: Manages data and state

### Component Relationships

The screen system employs a highly modular architecture with several key components:

1. **ScreenService**: The central hub that orchestrates all operations
   - Holds references to specialized subservices
   - Provides the public API for screen operations
   - Coordinates the screen lifecycle

2. **Configuration Layer**:
   - **ScreenConfig**: ScriptableObject that defines screen properties
   - **ScreenConfigModel**: Manages the configuration data
   - **ScreenManager**: Registers and organizes screens in the hierarchy

3. **Runtime Layer**:
   - **ScreenRuntimeModel**: Manages active and pooled screen instances
   - **ScreenVO**: Value object that stores the current state of a screen

4. **Implementation Layer**:
   - **ScreenBody**: Base MonoBehaviour implementation for screens
   - **SubServices**: Specialized services for specific tasks

<!-- [VISUAL RECOMMENDATION: Component interaction diagram showing how data flows between components] -->

### Dependency Injection

The system uses dependency injection to maintain loose coupling between components:

```csharp
// From ScreenServiceContext.cs
protected override void CoreBindings()
{
    _screenServiceInternalSignals = InjectionBinderCrossContext.Bind<ScreenServiceInternalSignals>();
    InjectionBinderCrossContext.Bind<IScreenService, ScreenService>();
    InjectionBinder.Bind<IScreenConfigModel, ScreenConfigModel>();
    InjectionBinder.Bind<IScreenRuntimeModel, ScreenRuntimeModel>();
    
    // SubServices
    InjectionBinder.Bind<AddressableLoadSubService>();
    InjectionBinder.Bind<ResourceLoadSubService>();
    InjectionBinder.Bind<DirectPrefabLoadSubService>();
    InjectionBinder.Bind<DisposeSubService>();
    
    // More bindings...
}
```

### Signal System

The system uses signals for internal communication between components:

```csharp
// From ScreenServiceInternalSignals.cs
internal class ScreenServiceInternalSignals : ISignalHolder
{
    public Signal<ScreenManagerVO, IContext> RegisterManager = new();
    public Signal<int, List<ScreenConfig>> RegisterConfigs = new();
    public Signal<int, List<ScreenConfig>> UnRegisterConfigs = new();
}
```

Signal bindings are set up in the context:

```csharp
// From ScreenServiceContext.cs
CommandBinder.Bind(_screenServiceInternalSignals.RegisterManager).To<RegisterScreenManagerCommand>();
CommandBinder.Bind(_screenServiceInternalSignals.RegisterConfigs).To<RegisterScreenConfigCommand>();
CommandBinder.Bind(_screenServiceInternalSignals.UnRegisterConfigs).To<UnRegisterScreenConfigCommand>();
```

<!-- [VISUAL RECOMMENDATION: Signal flow diagram showing how components communicate] -->

## Core Components

### ScreenService

The central hub that coordinates all screen operations through a fluent API:

```csharp
// Basic usage
_screenService.Open<MainMenuView>()
             .SetLayer(1)
             .Show();

// Advanced usage
_screenService.Open<PopupScreenView>()
             .SetLayer(2)
             .SetManagerIndex(1)
             .AddToHistory()
             .SetOpenAnimation(fadeAnimation)
             .SetCloseAnimation(closeAnimation)
             .Show();
```

The ScreenService implementation orchestrates the entire system:

```csharp
internal class ScreenService : IScreenService
{
    [Inject] private IScreenConfigModel _screenConfigModel { get; set; }
    [Inject] private IScreenRuntimeModel _screenRuntimeModel { get; set; }
    [Inject] private ScreenSetupSubService _setupService { get; set; }
    [Inject] private ScreenLifecycleSubService _lifecycleService { get; set; }
    
    [Inject] public LoadSubService Load { get; set; }
    [Inject] public CheckSubService Check { get; set; }
    [Inject] public HideSubService Hide { get; set; }
    [Inject] public UnloadSubService Unload { get; set; }
    [Inject] private IScreenBuilderSubService _builder { get; set; }
    
    // Implementation details...
}
```

### SubServices

Specialized services that handle specific aspects of screen management:

| Service | Responsibility | Key Methods |
|---------|----------------|-------------|
| LoadSubService | Handles screen loading from different sources | `All()`, `ScreensAtManager()`, `ByTag()`, `Screen()` |
| UnloadSubService | Manages screen unloading and resource cleanup | `AllScreens()`, `ScreensByManager()`, `Screen()` |
| ScreenBuilderSubService | Configures screen instances | `Open<T>()`, `SetLayer()`, `Show()` |
| ScreenLifecycleSubService | Manages screen lifecycle events | `RegisterCallbacks()`, `InvokeCallback()` |
| ScreenSetupSubService | Configures screen setup and initialization | `SetupScreen()`, `SetupRectTransform()` |
| HideSubService | Controls hiding screens | `AllScreens()`, `ScreenInLayer()`, `Screen()` |
| CheckSubService | Validates screen states and conditions | `IsScreenActive()`, `IsLayerFull()` |

The builder service implements the fluent API pattern:

```csharp
// From ScreenBuilderSubService.cs
public ScreenBuilderSubService SetLayer(int layerIndex)
{
    _currentScreenData.LayerIndex = layerIndex;
    return this;
}

public ScreenBuilderSubService SetForceOpen(bool forceOpen)
{
    _currentScreenData.ForceOpen = forceOpen;
    return this;
}

public async Task<T> Show<T>() where T : IScreenBody
{
    if (BeforeShowScreen()) return default;
    if (_currentScreenBody == null)
    {
        return await _screenService.ShowNewScreen<T>();
    }
    else
        return await _screenService.ShowPooledScreen<T>();
}
```

### Screen Models

Data management components:

- **ScreenConfigModel**: Manages screen configurations and manager data
  ```csharp
  // From ScreenConfigModel.cs
  internal class ScreenConfigModel : IScreenConfigModel
  {
      private readonly Dictionary<int, ScreenManagerVO> _screenManagers = new();
      private readonly Dictionary<int, IContext> _screenManagerContextMap = new();
      private readonly Dictionary<ScreenConfig, IScreenBody> _configMap = new();
      private readonly Dictionary<ScreenTag, List<ScreenConfig>> _tagConfigs = new();
      
      // Implementation details...
  }
  ```

- **ScreenRuntimeModel**: Manages active and pooled screens
  ```csharp
  // From ScreenRuntimeModel.cs
  internal class ScreenRuntimeModel : IScreenRuntimeModel
  {
      private readonly Dictionary<Type, List<IScreenBody>> _passiveScreens = new();
      private readonly Dictionary<int, Dictionary<Type, IScreenBody>> _activeScreens = new();
      private readonly Dictionary<int, Dictionary<int, IScreenBody>> _activeLayerScreens = new();
      private readonly Dictionary<int, Dictionary<ScreenTag, List<IScreenBody>>> _activeTagScreens = new();
      
      // Implementation details...
  }
  ```

### Screen States

Screens transition through a well-defined state machine:

```
None ──► Loading ──► InShowAnimation ──► InUse ──► InHideAnimation ──► InPool
```

States are implemented as flags, allowing for combinations like `InUse | InShowAnimation`:

```csharp
// From ScreenState.cs
[Flags]
public enum ScreenState
{
    None =                  0,
    Loading =               1 << 0,
    Closing =               1 << 1,
    InPool =                1 << 2,
    InUse =                 1 << 3,
    InShowAnimation =       1 << 4,
    InHideAnimation =       1 << 5,
    
    AvailableToSendSignal = InUse,
}
```

State manipulation is handled through extension methods:

```csharp
// From ScreenDataExtensions.cs
public static void AddState(this ScreenVO data, ScreenState value)
{
    data.State = data.State | value;
}

public static void RemoveState(this ScreenVO data, ScreenState value)
{
    data.State = data.State &~ value;
}

public static bool HasState(this ScreenVO data, ScreenState value)
{
    return data.State.HasFlag(value);
}
```

<!-- [VISUAL RECOMMENDATION: State machine diagram showing transitions] -->

## Screen Configuration

### ScreenConfig

Each screen is configured using a `ScreenConfig` ScriptableObject:

```csharp
[CreateAssetMenu(fileName = "ScreenConfig", menuName = "FlowIoC/Screen/Screen Config")]
public class ScreenConfig : ScriptableObject
{
    // Screen configuration properties
    [SerializeField] private int _defaultLayer;
    [SerializeField] private ScreenLoadType _loadType;
    [SerializeField] private GameObject _directPrefab;
    [SerializeField] private ScreenTag _screenTag;
    [SerializeField] private string _resourcePath;
    [SerializeField] private string _addressableKey;
    [SerializeField] private bool _hasShowAnimation;
    [SerializeField] private bool _hasHideAnimation;
    // ...
}
```

### Loading Types

Three loading methods are supported:

1. **Addressable**: Using Unity's Addressable Asset System
   ```csharp
   // Config setup
   screenConfig.LoadType = ScreenLoadType.Addressable;
   screenConfig.AddressableKey = "UI/MainMenu";
   ```

2. **Resource**: Using Unity's Resources system
   ```csharp
   // Config setup
   screenConfig.LoadType = ScreenLoadType.Resource;
   screenConfig.ResourcePath = "UI/MainMenu";
   ```

3. **DirectPrefab**: Using direct prefab references
   ```csharp
   // Config setup
   screenConfig.LoadType = ScreenLoadType.DirectPrefab;
   screenConfig.DirectPrefab = mainMenuPrefab;
   ```

The loading process is handled by specialized services:

```csharp
// From LoadSubService.cs
private async Task<IScreenBody> LoadScreenByType(ScreenConfig config)
{
    switch (config.LoadType)
    {
        case ScreenLoadType.Addressable:
            FlowConsole.Log(ConsoleLogType.Screen, $"[ScreenService.Load] Attempting Addressable load for {config.AddressableKey}");
            return await _addressableLoadService.LoadScreen(config);

        case ScreenLoadType.Resource:
            FlowConsole.Log(ConsoleLogType.Screen, $"[ScreenService.Load] Attempting Resource load for {config.AddressableKey}");
            return await _resourceLoadSubService.LoadScreen(config);

        case ScreenLoadType.DirectPrefab:
            FlowConsole.Log(ConsoleLogType.Screen, $"[ScreenService.Load] Attempting DirectPrefab load for {config.AddressableKey}");
            return await _directPrefabLoadSubService.LoadScreen(config);

        default:
            FlowConsole.LogError(ConsoleLogType.Screen, $"[ScreenService.Load][LoadScreenByType] Unknown load type {config.LoadType} for screen {config.AddressableKey}");
            return default;
    }
}
```

### Tags and Organization

Screens can be organized using tags:

```csharp
public enum ScreenTag
{
    Default,
    GroupA,
    GroupB,
    GroupC,
    GroupD,
    GroupE,
    GroupF,
    GroupG,
    GroupH
}
```

Tags allow for batch operations:

```csharp
// Load all screens with a specific tag
_screenService.Load.ByTag(ScreenTag.GroupA);

// Close all screens with a specific tag
if (_screenRuntimeModel.GetActiveTagScreens(ScreenTag.GroupA, managerId, out var screens))
{
    foreach (var screen in screens)
        _screenService.Close(screen);
}
```

## Manager & Layer System

### Manager Structure

Each manager maintains its own screen hierarchy and layer organization:

```csharp
// Show screen in specific manager
_screenService.Open<ExampleScreenView>()
             .SetManagerIndex(1) // Using manager with ID 1
             .Show();
```

Managers are defined using `ScreenManagerVO`:

```csharp
internal class ScreenManagerVO
{
    public int ManagerID;
    public List<ScreenLayer> ScreenLayerList = new();
    public Dictionary<Type, ScreenConfig> ScreenConfigs = new();
}
```

Managers are registered via signals:

```csharp
// From ScreenManagerMediator.cs
private void Setup()
{
    ScreenManagerVO manager = _view.ManagerData;

    FlowConsole.Log(ConsoleLogType.Screen, $"Registering screen manager with ID: {manager.ManagerID}");
    var viewInjector = _view.GetComponent<ViewInjector>();
    var context = viewInjector.GetContextOfView(_view);
    _screenServiceInternalSignals.RegisterManager.Dispatch(manager, context);

    List<ScreenConfig> configs = _view.GetScreenConfigs();
    if (configs != null && configs.Count > 0)
    {
        _screenServiceInternalSignals.RegisterConfigs.Dispatch(manager.ManagerID, configs);
    }
}
```

<!-- [VISUAL RECOMMENDATION: Manager hierarchy diagram showing layers and screens] -->

### Layer Management

Screens are organized in layers, with layer 0 being the lowest (background) and higher layers in the foreground:

```csharp
// Set layer (higher numbers are in front)
_screenService.Open<ExampleScreenView>()
             .SetLayer(2)
             .Show();

// Layer operations
_screenService.Hide.ScreensInLayer(2);
_screenService.Unload.ScreensInLayer(2);
```

The layer system is implemented in `ScreenSetupSubService`:

```csharp
// From ScreenSetupSubService.cs
public void SetupScreen(IScreenBody screenBody)
{
    var manager = _screenConfigModel.GetScreenManager(screenBody.Data.ManagerId);
    
    if (screenBody.Data.LayerIndex < 0 || screenBody.Data.LayerIndex >= manager.ScreenLayerList.Count)
    {
        FlowConsole.LogError(ConsoleLogType.Screen,$"[ScreenService] Invalid layer index: {screenBody.Data.LayerIndex} for screen: {screenBody.Data.ScreenType.Name}");
        return;
    }

    var layer = manager.ScreenLayerList[screenBody.Data.LayerIndex];
    if (layer == null)
    {
        FlowConsole.LogError(ConsoleLogType.Screen,$"[ScreenService] Layer is null at index: {screenBody.Data.LayerIndex}");
        return;
    }
    screenBody.gameObject.SetActive(true);
    SetupRectTransform(screenBody, layer);
}
```

### History Navigation

Screen transition history allows for navigation:

```csharp
// Add to history
_screenService.Open<ExampleScreenView>()
             .AddToHistory()
             .Show();

// Navigate history
await _screenService.BackToHistory();
```

## Memory Management

### Pool System

The system automatically pools screens for better performance:

```csharp
// From ScreenRuntimeModel.cs
public void AddToPassivePool(IScreenBody screenBody)
{
    FlowConsole.Log(ConsoleLogType.Screen, $"[ScreenRuntimeModel][AddToPassivePool] {screenBody.Data.ScreenType.Name}");

    screenBody.Data.AddState(ScreenState.InPool);

    var screenType = screenBody.Data.ScreenType;

    if (!_passiveScreens.ContainsKey(screenType))
        _passiveScreens[screenType] = new List<IScreenBody>();

    _passiveScreens[screenType].Add(screenBody);
    screenBody.transform.SetParent(_poolParent);
    screenBody.gameObject.SetActive(false);
}

public bool GetScreen<T>(out T screen) where T : IScreenBody
{
    Type screenType = typeof(T);

    if (!_passiveScreens.ContainsKey(screenType) || _passiveScreens[screenType].Count == 0)
    {
        screen = default;
        return false;
    }

    var pooledScreen = _passiveScreens[screenType][0];
    _passiveScreens[screenType].RemoveAt(0);

    screen = (T) pooledScreen;
    return true;
}
```

<!-- [VISUAL RECOMMENDATION: Pool lifecycle diagram showing states and transitions] -->

### Reference Counting

The system determines screen eligibility for cleanup by tracking its usage state. Although described conceptually like reference counting, the implementation relies on managing active and pooled states:

- A screen is considered "referenced" or "in use" when it's actively displayed (`ScreenState.InUse`).
- When hidden, it moves to the pool (`ScreenState.InPool`) and is no longer considered actively referenced.
- Screens that remain in the pool beyond a certain threshold without being reused become eligible for automatic cleanup (see Auto Cleanup section).

The following methods manage the transition between active and pooled states, effectively tracking usage:

```csharp
// When activating a screen (from ScreenRuntimeModel.cs)
public void AddToActivePools(IScreenBody screenBody)
{
    screenBody.Data.RemoveState(ScreenState.InPool);
    screenBody.Data.AddState(ScreenState.InUse);
    var managerId = screenBody.Data.ManagerId;

    if (!_activeScreens.ContainsKey(managerId))
        _activeScreens[managerId] = new Dictionary<Type, IScreenBody>();
    _activeScreens[managerId][screenBody.Data.ScreenType] = screenBody;

    if (!_activeLayerScreens.ContainsKey(managerId))
        _activeLayerScreens[managerId] = new Dictionary<int, IScreenBody>();
    _activeLayerScreens[managerId][screenBody.Data.LayerIndex] = screenBody;

    if (!_activeTagScreens.ContainsKey(managerId))
        _activeTagScreens[managerId] = new Dictionary<ScreenTag, List<IScreenBody>>();

    if (!_activeTagScreens[managerId].ContainsKey(screenBody.Data.Tag))
        _activeTagScreens[managerId].Add(screenBody.Data.Tag, new List<IScreenBody>());

    _activeTagScreens[managerId][screenBody.Data.Tag].Add(screenBody);
}

// When deactivating (from ScreenRuntimeModel.cs)
public void RemoveFromActivePools(IScreenBody screenBody)
{
    FlowConsole.Log(ConsoleLogType.Screen, $"[ScreenRuntimeModel][RemoveFromActivePools] {screenBody.Data.ScreenType.Name}");

    screenBody.Data.RemoveState(ScreenState.InUse);

    var managerId = screenBody.Data.ManagerId;

    _activeScreens[managerId].Remove(screenBody.Data.ScreenType);
    _activeLayerScreens[managerId].Remove(screenBody.Data.LayerIndex);
    _activeTagScreens[managerId][screenBody.Data.Tag].Remove(screenBody);
}
```

### Auto Cleanup

The system automatically cleans up unused screens:

- Cleanup interval: 300 seconds (5 minutes)
- Unused threshold: 600 seconds (10 minutes)
- Screens with zero references are eligible for cleanup

## Animation System

### Open/Close Animations

Screens can have custom animations:

```csharp
_screenService.Open<ExampleScreenView>()
             .SetOpenAnimation(animationData)
             .SetCloseAnimation(closeAnimData)
             .Show();
```

Animation implementation in screen class:

```csharp
// From ScreenBody.cs
public class ScreenBody : MonoBehaviour, IScreenBody
{
    public Action<IScreenBody> ShowAnimationCompleted { get; set; }
    public Action<IScreenBody> HideAnimationCompleted { get; set; }
    public bool IsRegistered { get; set; }
    public ScreenVO Data { get; set; } = new();
    
    public void Show()
    {
        if (!Data.HasShowAnimation)
            return;

        Data.AddState(ScreenState.InShowAnimation);
        PlayShowAnimation();
    }

    public void Hide()
    {
        Data.RemoveState(ScreenState.InHideAnimation);

        if (!Data.HasHideAnimation)
        {
            return;
        }

        FlowConsole.Log(ConsoleLogType.Screen, "Screen HidingAnimation started! id: " + this.GetType().Name);
        Data.AddState(ScreenState.InHideAnimation);
        PlayHideAnimation();
    }

    protected virtual void PlayShowAnimation()
    {
        // Override in derived classes
    }

    protected virtual void PlayHideAnimation()
    {
        // Override in derived classes
    }
}
```

### Animation Control

The system provides methods to control animations:

```csharp
// Skip animations
_screenService.SkipOpenAnimation(screenBody);
_screenService.SkipCloseAnimation(screenBody);

// Restart animations
_screenService.RestartOpenAnimation(screenBody);
_screenService.RestartCloseAnimation(screenBody);

// Check animation status
bool isAnimating = _screenService.IsScreenAnimating(screenBody);
```

## Error Handling

### State Validation

All state transitions are validated:

```csharp
private bool IsValidStateTransition(ScreenState currentState, ScreenState newState)
{
    // State transition validation logic
    if (currentState == ScreenState.None && newState == ScreenState.Loading)
        return true;
    
    if (currentState == ScreenState.Loading && newState == ScreenState.InShowAnimation)
        return true;
    
    // etc...
    
    return false;
}
```

### Error States

Special states for error handling:

```csharp
// Loading error
if (loadedScreen == null)
{
    // Set error state
    screenData.AddState(ScreenState.LoadError);
    FlowConsole.LogError(ConsoleLogType.Screen, $"Failed to load screen: {screenName}");
    return null;
}

// Animation error
try
{
    screenBody.Show(); // Start animation
}
catch (Exception e)
{
    // Set error state
    screenData.AddState(ScreenState.OpenAnimationError);
    FlowConsole.LogError(ConsoleLogType.Screen, $"Animation error: {e.Message}");
}
```

Error checking:

```csharp
if (_screenService.HasScreenError(screen))
{
    // Handle error
    var state = _screenService.GetScreenState(screen);
    // ...
}
```

## Practical Examples

### Basic Screen Operations

```csharp
public class BasicExample : BaseUIContext
{
    [Inject] private IScreenService _screenService { get; set; }

    public override void Launch()
    {
        // Show screen
        _screenService.Open<MainScreenView>()
                     .SetLayer(1)
                     .Show();

        // Hide screen (keeps in pool)
        _screenService.Hide.Screen<MainScreenView>();

        // Close screen (unloads resources)
        _screenService.Unload.Screen<MainScreenView>();
    }
}
```

### Advanced Configurations

```csharp
public class AdvancedExample : BaseUIContext
{
    [Inject] private IScreenService _screenService { get; set; }

    public async void ShowPopup()
    {
        // Animated screen with history and parameters
        var popup = await _screenService.Open<PopupScreenView>()
                          .SetLayer(5)
                          .SetManagerIndex(1)
                          .AddToHistory()
                          .SetOpenAnimation(fadeInData)
                          .SetCloseAnimation(fadeOutData)
                          .SetParameters("Title", 100, playerData)
                          .Show();

        // Layer operations
        _screenService.Hide.ScreensInLayer(2);

        // History navigation
        await _screenService.BackToHistory();
    }
}
```

### Handling Errors

```csharp
public class ErrorHandlingExample : BaseUIContext
{
    [Inject] private IScreenService _screenService { get; set; }

    public async void ShowWithErrorHandling()
    {
        try
        {
            var screen = await _screenService
                .Open<ErrorScreenView>()
                .Show();

            if (_screenService.HasScreenError(screen))
            {
                // Handle specific error types
                var state = _screenService.GetScreenState(screen);
                if (state.HasFlag(ScreenState.LoadError))
                {
                    // Handle load error
                    ShowErrorMessage("Failed to load screen");
                }
                else if (state.HasFlag(ScreenState.OpenAnimationError))
                {
                    // Handle animation error
                    _screenService.SkipOpenAnimation(screen);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Screen error: {e.Message}");
            // Fall back to alternative UI
            ShowFallbackUI();
        }
    }
}
```

## Performance Optimization

### Best Practices

1. **Screen Configuration**
   - Use appropriate loading type based on screen complexity
   - Preload commonly used screens during initialization
   - Organize screens in logical layer groupings

2. **Pooling Optimization**
   - Adjust pool cleanup parameters based on memory constraints
   - Consider manually unloading rarely used screens
   - Monitor the pool size during development

3. **Animation Efficiency**
   - Keep animations lightweight and optimized
   - Consider skipping animations on low-end devices
   - Implement proper completion callbacks

### Memory Management Tips

1. **Resource Cleanup**
   - Ensure Addressables are properly released when no longer needed
   - Monitor memory usage with Unity Profiler
   - Consider manual pool cleanup during scene transitions

2. **Screen Lifecycle**
   - Implement OnDestroy handling in screen components
   - Unsubscribe from events when screens are pooled
   - Cache and reuse resources where possible

<!-- [VISUAL RECOMMENDATION: Performance comparison chart showing pooled vs. non-pooled memory usage] -->

## Internal Implementation Details

### Service Registration Flow

The screen system initialization follows this flow:

1. **Context Registration**
   - ScreenServiceContext registers all services and binds signals

2. **Manager Registration**
   - ScreenManager components dispatch RegisterManager signal on OnRegister
   - RegisterScreenManagerCommand adds the manager to ScreenConfigModel

3. **Config Registration**
   - RegisterConfigs signal is dispatched with screen configurations
   - RegisterScreenConfigCommand adds them to the manager's configuration

### Screen Lifecycle

The complete screen lifecycle follows these steps:

1. **Creation/Retrieval**
   ```
   Open<T>() → Check Pool → Create New or Get from Pool
   ```

2. **Configuration**
   ```
   SetLayer() → SetForceOpen() → SetParameters() → AddToHistory()
   ```

3. **Display**
   ```
   Show() → BeforeShowScreen() → ShowNewScreen() or ShowPooledScreen()
   ```

4. **Setup**
   ```
   Load Screen → AddToActivePools() → SetupScreen() → Show()
   ```

5. **Animation**
   ```
   PlayShowAnimation() → ShowAnimationCompleted
   ```

6. **Hiding**
   ```
   Hide.Screen() → PlayHideAnimation() → HideAnimationCompleted → ReturnToPassivePool()
   ```

7. **Unloading**
   ```
   Unload.Screen() → AddState(Closing) → Hide.Screen() → AfterHide() → RemoveFromPools() → Dispose()
   ```

### Memory Management Implementation

The memory management system is implemented across several components:

1. **Pool Creation**
   ```csharp
   // From ScreenRuntimeModel.cs
   private void CreatePoolParent()
   {
       _poolParent = new GameObject("[Screen_Pool]").transform;
       UnityEngine.Object.DontDestroyOnLoad(_poolParent.gameObject);
   }
   ```

2. **Pool Addition**
   ```csharp
   // From ScreenRuntimeModel.cs
   public void AddToPassivePool(IScreenBody screenBody)
   {
       screenBody.Data.AddState(ScreenState.InPool);
       var screenType = screenBody.Data.ScreenType;
       if (!_passiveScreens.ContainsKey(screenType))
           _passiveScreens[screenType] = new List<IScreenBody>();
       _passiveScreens[screenType].Add(screenBody);
       screenBody.transform.SetParent(_poolParent);
       screenBody.gameObject.SetActive(false);
   }
   ```

3. **Reference Management**
   ```csharp
   // From ScreenRuntimeModel.cs
   public void AddToActivePools(IScreenBody screenBody)
   {
       screenBody.Data.RemoveState(ScreenState.InPool);
       screenBody.Data.AddState(ScreenState.InUse);
       // Add to active collections...
   }
   
   public void RemoveFromActivePools(IScreenBody screenBody)
   {
       screenBody.Data.RemoveState(ScreenState.InUse);
       // Remove from active collections...
   }
   ```

4. **Cleanup Process**
   ```csharp
   // From UnloadSubService.cs
   internal void AfterHide(IScreenBody screenBody)
   {
       screenBody.Data.RemoveState(ScreenState.Closing);
       RemoveFromPools(screenBody);
       _dispose.Screen(screenBody);
   }

   private void RemoveFromPools(IScreenBody screenBody)
   {
       _runtimeModel.RemoveFromActivePools(screenBody);
       _runtimeModel.RemoveFromPassivePool(screenBody);
   }
   ```