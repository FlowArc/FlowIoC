# FlowIoC Screen System - User Manual

## Table of Contents
1. [Getting Started](#getting-started)
   - [Installation](#installation)
   - [Initial Setup](#initial-setup)
   - [Basic Concepts](#basic-concepts)
2. [Creating Screens](#creating-screens)
   - [Screen Prefabs](#screen-prefabs)
   - [Screen Configuration](#screen-configuration)
   - [Implementing Views](#implementing-views)
3. [Using Screen Service](#using-screen-service)
   - [Showing Screens](#showing-screens)
   - [Hiding Screens](#hiding-screens)
   - [Closing Screens](#closing-screens)
   - [Passing Parameters](#passing-parameters)
4. [Organizing Screens](#organizing-screens)
   - [Using Managers](#using-managers)
   - [Working with Layers](#working-with-layers)
   - [Screen Tags](#screen-tags)
5. [Animation System](#animation-system)
   - [Adding Open Animations](#adding-open-animations)
   - [Adding Close Animations](#adding-close-animations)
   - [Animation Controls](#animation-controls)
6. [Memory Management](#memory-management)
   - [Understanding the Pool System](#understanding-the-pool-system)
   - [Manual Pool Management](#manual-pool-management)
   - [Resource Cleanup](#resource-cleanup)
7. [Navigation History](#navigation-history)
   - [History Configuration](#history-configuration)
   - [Navigation Methods](#navigation-methods)
8. [Error Handling](#error-handling)
   - [Validating Screens](#validating-screens)
   - [Error Recovery](#error-recovery)
9. [Performance Tips](#performance-tips)
   - [Optimizing Screen Lifecycle](#optimizing-screen-lifecycle)
   - [Memory Footprint](#memory-footprint)
10. [Troubleshooting](#troubleshooting)
    - [Common Issues](#common-issues)
    - [Debug Tools](#debug-tools)

## Getting Started

### Installation

1. Add the FlowIoC package to your Unity project:
   - Via Package Manager: Add package from git URL `https://github.com/your-repository/FlowIoC.git`
   - Or import the FlowIoC unitypackage directly

2. After installation, verify the following folders exist in your project:
   - `Packages/FlowIoC/Runtime/ScreenModule`
   - `Packages/FlowIoC/Editor/ScreenModule`

### Initial Setup

To use the Screen System in your project, you need to initialize it properly:

1. Create a root context that includes the ScreenServiceContext:

```csharp
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.RootsContexts;

public class YourRootContext : Context
{
    protected override void CoreBindings()
    {
        base.CoreBindings();
        
        // Add the Screen System context
        AddContext<ScreenServiceContext>();
        
        // Your other contexts...
    }
}
```

2. Add a Screen Manager GameObject to your main scene:
   - Create an empty GameObject
   - Add the `ScreenManager` component
   - Set a unique Manager ID (typically 0 for the first/main manager)

### Basic Concepts

Before using the Screen System, understand these key concepts:

- **Screen**: A UI element that can be shown, hidden, and managed by the Screen System
- **ScreenBody**: The component implementation of a screen
- **ScreenService**: The central service to interact with screens
- **ScreenConfig**: Configuration data for each screen
- **Manager**: A container for screens, allowing multiple UI hierarchies
- **Layer**: Controls the Z-order of screens within a manager

## Creating Screens

### Screen Prefabs

1. Create a new UI prefab for your screen:
   - Right-click in Project window > Create > UI > Canvas
   - Design your UI elements
   - Add a script that inherits from `ScreenBody`

```csharp
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;

public class MainMenuScreen : ScreenBody
{
    // Override if you have custom animation
    protected override void PlayShowAnimation()
    {
        // Implement your animation logic
        // When done, invoke completion callback:
        ShowAnimationCompleted?.Invoke(this);
    }

    protected override void PlayHideAnimation()
    {
        // Implement your animation logic
        // When done, invoke completion callback:
        HideAnimationCompleted?.Invoke(this);
    }
}
```

### Screen Configuration

1. Create a ScreenConfig asset for each screen:
   - Right-click in Project window > Create > FlowIoC > Screen > Screen Config
   - Configure the following properties:
     - Default Layer: The default layer index for this screen
     - Load Type: How the screen is loaded (Addressable/Resource/DirectPrefab)
     - Direct Prefab: The screen prefab (if using DirectPrefab)
     - Screen Tag: Organizational tag
     - Resource Path: Path to screen prefab (if using Resource)
     - Addressable Key: Addressable key (if using Addressable)
     - Has Show Animation: Whether screen has a show animation
     - Has Hide Animation: Whether screen has a hide animation

2. Add the ScreenConfig to your ScreenManager:
   - Select the ScreenManager GameObject
   - Add your ScreenConfig to the "Screen Configs" list

### Implementing Views

Implement your screen's View class:

```csharp
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
using UnityEngine.UI;

public class LoginScreenView : ScreenBody
{
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private InputField _usernameField;
    [SerializeField] private InputField _passwordField;

    // Optional: Called when the screen is shown
    private void OnEnable()
    {
        _loginButton.onClick.AddListener(OnLoginClicked);
        _cancelButton.onClick.AddListener(OnCancelClicked);
    }

    // Optional: Called when the screen is hidden
    private void OnDisable()
    {
        _loginButton.onClick.RemoveListener(OnLoginClicked);
        _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }

    private void OnLoginClicked()
    {
        // Your login logic
    }

    private void OnCancelClicked()
    {
        // Your cancel logic
    }
}
```

## Using Screen Service

### Showing Screens

To show a screen, use the ScreenService with the fluent API:

```csharp
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Service;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [Inject] private IScreenService _screenService;

    public void ShowMainMenu()
    {
        // Basic usage
        _screenService.Open<MainMenuScreen>()
                     .SetLayer(1)
                     .Show();
    }

    public async void ShowSettingsScreen()
    {
        // Advanced usage with await
        var settingsScreen = await _screenService.Open<SettingsScreen>()
                                               .SetLayer(2)
                                               .AddToHistory()
                                               .Show();
                                               
        // You can now work with the returned screen reference
        Debug.Log($"Settings screen shown: {settingsScreen != null}");
    }
}
```

### Hiding Screens

To hide a screen (which sends it to the pool for later reuse):

```csharp
// Hide by type
_screenService.Hide.Screen<MainMenuScreen>();

// Hide screen in a specific layer
_screenService.Hide.ScreensInLayer(1);

// Hide by screen reference
_screenService.Hide.Screen(screenReference);
```

### Closing Screens

To completely close and unload a screen (removing it from the pool):

```csharp
// Close by type
_screenService.Unload.Screen<MainMenuScreen>();

// Close screen in a specific layer
_screenService.Unload.ScreensInLayer(1);

// Close by screen reference
_screenService.Unload.Screen(screenReference);
```

### Passing Parameters

You can pass parameters to screens when showing them:

```csharp
// Passing parameters
_screenService.Open<PlayerProfileScreen>()
             .SetParameters("PlayerName", playerScore, playerData)
             .Show();
```

Receiving parameters in your screen:

```csharp
public class PlayerProfileScreen : ScreenBody
{
    private string _playerName;
    private int _playerScore;
    private PlayerData _playerData;

    public override void SetupScreen(params object[] parameters)
    {
        if (parameters.Length >= 3)
        {
            _playerName = parameters[0] as string;
            _playerScore = (int)parameters[1];
            _playerData = parameters[2] as PlayerData;
            
            // Update UI with the parameters
            UpdateUI();
        }
    }
    
    private void UpdateUI()
    {
        // Your logic to update UI with parameters
    }
}
```

## Organizing Screens

### Using Managers

For complex UIs, you can use multiple managers:

```csharp
// Show screen in a specific manager
_screenService.Open<HUDScreen>()
             .SetManagerIndex(1) // Using manager with ID 1
             .Show();
```

Setting up multiple managers:
1. Create multiple ScreenManager GameObjects
2. Assign unique Manager IDs to each
3. Configure screen configs for each manager

### Working with Layers

Layers control the Z-order of screens within a manager:

```csharp
// Basic layer usage
_screenService.Open<BackgroundScreen>()
             .SetLayer(0) // Background
             .Show();

_screenService.Open<GameplayScreen>()
             .SetLayer(1) // Middle
             .Show();

_screenService.Open<PopupScreen>()
             .SetLayer(2) // Foreground
             .Show();
```

Layer operations:

```csharp
// Hide all screens in a layer
_screenService.Hide.ScreensInLayer(1);

// Close all screens in a layer
_screenService.Unload.ScreensInLayer(1);
```

### Screen Tags

Tags provide another way to organize and operate on groups of screens:

```csharp
// Load all screens with a specific tag
_screenService.Load.ByTag(ScreenTag.GroupA);

// Custom operations on screens with a specific tag
if (_screenRuntimeModel.GetActiveTagScreens(ScreenTag.GroupA, managerId, out var screens))
{
    foreach (var screen in screens)
    {
        // Custom operations on each screen
    }
}
```

## Animation System

### Adding Open Animations

1. Set "Has Show Animation" to true in your ScreenConfig
2. Implement the animation in your ScreenBody:

```csharp
public class CustomScreen : ScreenBody
{
    [SerializeField] private Animator _animator;

    protected override void PlayShowAnimation()
    {
        // Play animation
        _animator.SetTrigger("Show");
        
        // For non-Animator animations (like tweens or custom animations),
        // make sure to call ShowAnimationCompleted when done
        StartCoroutine(WaitForShowAnimation());
    }
    
    private IEnumerator WaitForShowAnimation()
    {
        // Wait for animation to complete
        yield return new WaitForSeconds(1.0f); // Or use AnimatorEvents
        
        // Notify system that animation is complete
        ShowAnimationCompleted?.Invoke(this);
    }
}
```

### Adding Close Animations

1. Set "Has Hide Animation" to true in your ScreenConfig
2. Implement the animation in your ScreenBody:

```csharp
public class CustomScreen : ScreenBody
{
    [SerializeField] private Animator _animator;

    protected override void PlayHideAnimation()
    {
        // Play animation
        _animator.SetTrigger("Hide");
        
        // For non-Animator animations, make sure to call HideAnimationCompleted when done
        StartCoroutine(WaitForHideAnimation());
    }
    
    private IEnumerator WaitForHideAnimation()
    {
        // Wait for animation to complete
        yield return new WaitForSeconds(1.0f); // Or use AnimatorEvents
        
        // Notify system that animation is complete
        HideAnimationCompleted?.Invoke(this);
    }
}
```

### Animation Controls

Control animations with the following methods:

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

## Memory Management

### Understanding the Pool System

The Screen System uses a pool to manage memory efficiently:

1. When you show a screen, the system checks if it's in the pool
2. If found, it reuses the pooled instance
3. If not, it creates a new instance
4. When you hide a screen, it's added to the pool
5. Periodically, unused screens are removed from the pool

### Manual Pool Management

For better control, you can manually manage the pool:

```csharp
// Preload screens (e.g., during loading screen)
_screenService.Load.All();

// Preload screens for a specific manager
_screenService.Load.ScreensAtManager(0);

// Preload screens with a specific tag
_screenService.Load.ByTag(ScreenTag.Common);
```

### Resource Cleanup

To clean up resources and prevent memory leaks:

```csharp
// Unload all screens
foreach (var screen in _screenService.GetAllActiveScreens())
{
    _screenService.Unload.Screen(screen);
}

// On scene transition, consider manual cleanup
private void OnSceneUnloaded(Scene scene)
{
    // Clean up all screens in this scene
    _screenService.Unload.AllScreens();
}
```

## Navigation History

### History Configuration

Enable history tracking for screens:

```csharp
// Add screen to history
_screenService.Open<MainMenuScreen>()
             .AddToHistory()
             .Show();
```

### Navigation Methods

Navigate through screen history:

```csharp
// Go back to previous screen
await _screenService.BackToHistory();

// Reset history for a specific manager
_screenService.ResetHistory(0);

// Reset all history
_screenService.ResetAllHistory();
```

Implementing a back button:

```csharp
public class UIController : MonoBehaviour
{
    [Inject] private IScreenService _screenService;
    [SerializeField] private Button _backButton;
    
    private void OnEnable()
    {
        _backButton.onClick.AddListener(OnBackButtonClicked);
    }
    
    private void OnDisable()
    {
        _backButton.onClick.RemoveListener(OnBackButtonClicked);
    }
    
    private async void OnBackButtonClicked()
    {
        await _screenService.BackToHistory();
    }
}
```

## Error Handling

### Validating Screens

Check for screen errors:

```csharp
public async void ShowScreenWithValidation()
{
    try
    {
        var screen = await _screenService.Open<MyScreenView>()
                                        .Show();
                                        
        if (_screenService.HasScreenError(screen))
        {
            // Handle error
            Debug.LogError("Screen has an error!");
            
            // Check specific error type
            var state = _screenService.GetScreenState(screen);
            
            if (state.HasFlag(ScreenState.LoadError))
            {
                // Handle load error
                ShowErrorMessage("Failed to load screen");
            }
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"Screen error: {e.Message}");
    }
}
```

### Error Recovery

Strategies for recovering from screen errors:

```csharp
// For animation errors
if (state.HasFlag(ScreenState.OpenAnimationError))
{
    // Skip the errored animation
    _screenService.SkipOpenAnimation(screen);
}

// For loading errors
if (state.HasFlag(ScreenState.LoadError))
{
    // Try alternative screen
    _screenService.Open<FallbackScreen>()
                 .Show();
}
```

## Performance Tips

### Optimizing Screen Lifecycle

1. **Preload common screens**:
   ```csharp
   // During game initialization
   _screenService.Load.ByTag(ScreenTag.Common);
   ```

2. **Reuse screens** instead of destroying them:
   ```csharp
   // Hide instead of close when you'll need the screen again soon
   _screenService.Hide.Screen<CommonScreen>();
   ```

3. **Optimize animations**:
   - Keep animations simple
   - Avoid expensive operations during animations
   - Consider skipping animations on low-end devices

### Memory Footprint

1. **Monitor pool size**:
   ```csharp
   // Get count of screens in pool
   int screenCount = _screenService.GetAllLoadedScreens().Count;
   Debug.Log($"Screens in pool: {screenCount}");
   ```

2. **Adjust cleanup parameters**:
   ```csharp
   // If implemented in your project
   _screenService.SetPoolCleanupInterval(180); // 3 minutes
   _screenService.SetUnusedThreshold(300);    // 5 minutes
   ```

3. **Clear unused resources**:
   ```csharp
   // After scene transitions or gameplay phases
   Resources.UnloadUnusedAssets();
   System.GC.Collect();
   ```

## Troubleshooting

### Common Issues

1. **Screen not showing**:
   - Check if ScreenConfig is properly added to ScreenManager
   - Verify the loading type and paths/references
   - Check console for errors

2. **Animation not working**:
   - Ensure "Has Show Animation" or "Has Hide Animation" is checked in ScreenConfig
   - Verify your PlayShowAnimation/PlayHideAnimation implementation
   - Make sure you're calling ShowAnimationCompleted/HideAnimationCompleted

3. **Memory leaks**:
   - Check for missed Addressable releases
   - Ensure screens are being properly returned to pool
   - Monitor active screen count over time

### Debug Tools

Enable debug logging:

```csharp
// In development build
FlowConsole.SetLogLevel(ConsoleLogType.Screen, LogLevel.Verbose);

// Check screen state
var state = _screenService.GetScreenState(screen);
Debug.Log($"Screen state: {state}");

// View active screens
var activeScreens = _screenService.GetAllActiveScreens();
foreach (var screen in activeScreens)
{
    Debug.Log($"Active screen: {screen.GetType().Name}");
}
``` 