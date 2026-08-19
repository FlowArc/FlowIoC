# FlowIoC PoolModule Documentation (V2)

## Table of Contents
1. [Introduction](#introduction)
2. [Architecture Overview](#architecture-overview)
   - [System Components](#system-components)
   - [Data Flow](#data-flow)
   - [Dependency Injection & Signals](#dependency-injection--signals)
3. [Core Components](#core-components)
   - [PoolService](#poolservice)
   - [Sub-Services](#sub-services)
   - [Pool Models](#pool-models)
   - [Pool Entities](#pool-entities)
   - [Data Structures](#data-structures)
4. [Pool Configuration](#pool-configuration)
   - [CD_PoolGroup](#cd_poolgroup)
   - [PoolItemVO](#poolitemvo)
   - [PoolGroupEntry](#poolgroupentry)
   - [Configuration Options](#configuration-options)
5. [Addressable Assets Support](#addressable-assets-support)
   - [AssetReferenceSpawnableObject](#assetreferencespawnableobject)
   - [ComponentReference](#componentreference)
   - [Addressable Integration](#addressable-integration)
6. [Pool Lifecycle](#pool-lifecycle)
   - [Initialization Flow](#initialization-flow)
   - [Group Management](#group-management)
   - [Item Lifecycle](#item-lifecycle)
7. [Memory Management](#memory-management)
   - [Pool Organization](#pool-organization)
   - [Active/Passive Tracking](#activepassive-tracking)
   - [Cleanup Strategies](#cleanup-strategies)
8. [Async Operations](#async-operations)
   - [Lazy Loading](#lazy-loading)
   - [Async Item Retrieval](#async-item-retrieval)
   - [Addressable Loading](#addressable-loading)
9. [API Reference](#api-reference)
   - [PoolService Methods](#poolservice-methods)
   - [Builder API](#builder-api)
   - [Sub-Service APIs](#sub-service-apis)
10. [Practical Implementation](#practical-implementation)
    - [Basic Usage](#basic-usage)
    - [Advanced Scenarios](#advanced-scenarios)
    - [Performance Tips](#performance-tips)
11. [Integration with FlowIoC](#integration-with-flowioc)
    - [Context Setup](#context-setup)
    - [Signal System](#signal-system)
    - [View-Mediator Pattern](#view-mediator-pattern)
12. [Best Practices](#best-practices)
13. [Troubleshooting](#troubleshooting)

---

## Introduction

The FlowIoC PoolModule is a comprehensive object pooling system designed for Unity applications. Object pooling is a crucial optimization technique that reuses objects instead of creating and destroying them repeatedly, significantly improving performance by reducing memory fragmentation and garbage collection overhead.

This V2 architecture emphasizes clean separation of concerns, fluent API design, and robust, decoupled communication patterns inspired by the ScreenModule architecture.

**Key Features:**
- **Fluent Builder API**: Intuitive method chaining for item retrieval
- **Addressable Assets Support**: Full integration with Unity's Addressable system
- **Async Operations**: Non-blocking loading and instantiation
- **Lazy Loading**: On-demand pool creation with configurable pre-warming
- **Memory Efficient**: Intelligent active/passive item tracking
- **Configurable**: ScriptableObject-based configuration system
- **Extensible**: Easy to extend with custom pool behaviors
- **Performance Optimized**: Minimal allocation during runtime operations

**Architecture Benefits:**
- **Decoupled & Maintainable**: Specialized sub-services handle distinct responsibilities
- **Signal-Driven**: Robust initialization through command/signal pattern
- **Centralized State**: Clear separation between configuration and runtime state
- **Injectable**: Full dependency injection support throughout the system

## Architecture Overview

The PoolModule follows a layered, service-oriented architecture that promotes modularity and testability.

```
┌─────────────────────────────────────────────────────────────────┐
│                        PoolService                              │
│                    (Main Entry Point)                           │
└─────────────────────┬───────────────────────────────────────────┘
                      │
      ┌───────────────┼───────────────┐
      ▼               ▼               ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│Sub-Services │ │Pool Models  │ │Context/Root │
│             │ │             │ │             │
│• Builder    │ │• Config     │ │• Context    │
│• Create     │ │• Runtime    │ │• Root       │
│• Destroy    │ │             │ │• Signals    │
│• Load       │ │             │ │• Commands   │
│• Return     │ │             │ │             │
│• Check      │ │             │ │             │
└─────────────┘ └─────────────┘ └─────────────┘
      │               │               │
      └───────────────┼───────────────┘
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Data Layer                                   │
│                                                                 │
│ • PoolItemVO          • CD_PoolGroup       • IPoolableItem      │
│ • PoolGroupEntry      • PoolRootAdapter    • PoolableItem       │
│ • ComponentReference  • AssetReference...  • Pool Entities      │
└─────────────────────────────────────────────────────────────────┘
```

### System Components

The system consists of several key layers:

1. **Service Layer**: Main entry point and orchestration
2. **Sub-Service Layer**: Specialized services for specific operations
3. **Model Layer**: Data management and state tracking
4. **Entity Layer**: Poolable objects and their interfaces
5. **Configuration Layer**: ScriptableObject-based setup
6. **Context Layer**: Dependency injection and initialization

### Data Flow

```
Configuration → Registration → Pool Creation → Item Retrieval → Return
      ↓              ↓             ↓              ↓            ↓
ScriptableObject → Signal → Sub-Services → Builder API → Auto-Return
```

### Dependency Injection & Signals

The system uses FlowIoC's dependency injection extensively:

- **Service Injection**: All services are injected where needed
- **Signal System**: Decoupled communication for initialization
- **Command Pattern**: Robust configuration registration
- **Mediator Pattern**: View-to-logic separation

## Core Components

### PoolService

The `PoolService` acts as the main facade and orchestrator for the entire pooling system.

```csharp
public interface IPoolService
{
    void AutoInitialize();
    void ManualInitialize();
    void InitializeTag(string tag);
    IPoolBuilderSubService Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null);
    CreateSubService Create { get; }
    ReturnSubService Return { get; }
    CheckSubService Check { get; }
}
```

**Key Methods:**
- `AutoInitialize()`: Initializes all pools marked for auto-initialization
- `ManualInitialize()`: Initializes all configured pools
- `InitializeTag(string tag)`: Initializes a specific pool group
- `Get(string itemKey)`: Returns a builder for fluent item retrieval

### Sub-Services

The system is built around specialized sub-services, each handling a specific aspect of pool management.

#### IPoolBuilderSubService

Provides the fluent API for item retrieval:

```csharp
public interface IPoolBuilderSubService
{
    IPoolBuilderSubService Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null);
    IPoolBuilderSubService WithParent(Transform parent);
    IPoolBuilderSubService WithCallback(Action<IPoolableItem> callback);
    IPoolableItem Execute();
    T Execute<T>() where T : class, IPoolableItem;
    Task<IPoolableItem> ExecuteAsync();
    Task<T> ExecuteAsync<T>() where T : class, IPoolableItem;
}
```

#### CreateSubService

Handles pool and group creation:

```csharp
public void Group(string tag)           // Creates all pools in a group
public void CreatePool(string itemKey)  // Creates a specific pool
```

#### DestroySubService

Manages cleanup and destruction:

```csharp
public void Group(string tag)           // Destroys entire group
public void Pool(string itemKey)        // Destroys specific pool
public void Item(IPoolableItem item)    // Destroys individual item
```

#### LoadSubService

Handles item instantiation:

```csharp
public async Task Item(PoolItemBaseVO itemConfig, string tag)
public async Task<IPoolableItem> CreateItem(PoolItemBaseVO itemConfig, string tag)
```

#### ReturnSubService

Manages item return to pools:

```csharp
public void Item(IPoolableItem item)    // Returns single item
public void Group(string tag)           // Returns all active items in group
```

#### CheckSubService

Provides pool state queries:

```csharp
public bool IsTagReady(string tag)      // Checks if group is initialized
```

### Pool Models

#### IPoolConfigModel

Manages pool configuration data:

```csharp
public interface IPoolConfigModel
{
    void AddConfig(string tag, CD_PoolGroup config, bool autoInitialize);
    bool TryGetItemConfig(string itemKey, out PoolItemBaseVO itemConfig);
    string GetTagOfItem(string itemKey);
    IEnumerable<PoolItemBaseVO> GetAllItemConfigs();
    IEnumerable<string> GetAutoInitializeTags();
    IEnumerable<PoolItemBaseVO> GetItemConfigsByTag(string tag);
    IEnumerable<string> GetAllTags();
    bool IsConfigExist(string tag);
}
```

#### IPoolRuntimeModel

Manages live pool state:

```csharp
public interface IPoolRuntimeModel
{
    void AddToPassivePool(IPoolableItem item, string itemKey, string tag);
    bool GetFromPassivePool<T>(string itemKey, string tag, out T item) where T : class, IPoolableItem;
    void AddToActivePool(IPoolableItem item, string itemKey, string tag);
    void RemoveFromActivePool(IPoolableItem item, string itemKey, string tag);
    
    int GetActiveItemCount(string itemKey, string tag);
    int GetPassiveItemCount(string itemKey, string tag);
    
    IEnumerable<IPoolableItem> GetAllActiveItemsByTag(string tag);
    IEnumerable<IPoolableItem> GetAllPassiveItemsByTag(string tag);
    
    void RegisterPool(string itemKey, string tag);
    void UnregisterPool(string itemKey, string tag);
    bool IsTagRegistered(string tag);
    bool PoolExists(string itemKey, string tag);
    void ClearPoolByTag(string tag);
    void ClearPoolByItemKey(string itemKey, string tag);
}
```

### Pool Entities

#### IPoolableItem

The core interface for all poolable objects:

```csharp
public interface IPoolableItem
{
    string ItemKey { get; set; }
    Action<IPoolableItem> ReturnToPoolAction { get; set; }
    Transform transform { get; }
    void SetActive(bool value = true);
    void OnInitialized();
    void OnGetFromPool();
    void OnReturnToPool();
    void Dismiss();  // Shortcut for returning to pool
}
```

#### PoolableItem

Base implementation of IPoolableItem:

```csharp
public class PoolableItem : MonoBehaviour, IPoolableItem
{
    public string ItemKey { get; set; }
    public Action<IPoolableItem> ReturnToPoolAction { get; set; }
    
    public virtual void SetActive(bool value = true) => gameObject.SetActive(value);
    public virtual void Dismiss() => ReturnToPoolAction?.Invoke(this);
    public virtual void OnInitialized() { }
    public virtual void OnGetFromPool() { }
    public virtual void OnReturnToPool() { }
}
```

### Data Structures

#### PoolGroupEntry

Represents a pool group configuration:

```csharp
[System.Serializable]
public class PoolGroupEntry
{
    public CD_PoolGroup Group;
    public bool AutoInitialize;
}
```

#### PoolRootAdapter

Scene component that holds pool configurations:

```csharp
public class PoolRootAdapter : MonoBehaviour
{
    [SerializeField] private SerializedDictionary<string, PoolGroupEntry> _poolGroups;
    public SerializedDictionary<string, PoolGroupEntry> PoolGroups => _poolGroups;
}
```

## Pool Configuration

### CD_PoolGroup

A ScriptableObject that defines a group of pools:

```csharp
[CreateAssetMenu(fileName = "PoolGroup", menuName = "FlowIoC/PoolModule/Data/CD_PoolGroup", order = 1)]
public class CD_PoolGroup : ScriptableObject
{
    [SerializeField] private List<PoolItemVO> _items = new();
    public List<PoolItemVO> Items => _items;
}
```

### PoolItemVO

Defines configuration for a single pool item:

```csharp
[Serializable]
public class PoolItemVO : PoolItemBaseVO
{
    [Header("Asset Config - Direct Prefab")]
    public GameObject Prefab;

    [Header("Asset Config - Addressable Prefab")]
    public AssetReferenceSpawnableObject AddressablePrefab;

    public override object Asset => IsAddressable ? (object)AddressablePrefab : Prefab;
}
```

### PoolItemBaseVO

Base configuration class:

```csharp
[Serializable]
public abstract class PoolItemBaseVO
{
    [Header("Identification")]
    public string PoolKey;

    [Header("Pool Config")]
    public int InitialCreateCount = 10;
    public bool IsExtendable = true;
    public bool LazyLoad = false;
    public bool IsAddressable = false;

    public abstract object Asset { get; }
}
```

### Configuration Options

| Property | Description | Default |
|----------|-------------|---------|
| `PoolKey` | Unique identifier for the pool item | Required |
| `InitialCreateCount` | Number of items to create initially | 10 |
| `IsExtendable` | Whether the pool can create new items when empty | true |
| `LazyLoad` | Whether to delay creation until first request | false |
| `IsAddressable` | Whether the asset is an Addressable | false |

## Addressable Assets Support

The PoolModule provides full support for Unity's Addressable Assets system.

### AssetReferenceSpawnableObject

A specialized AssetReference for poolable objects:

```csharp
[Serializable]
public class AssetReferenceSpawnableObject : ComponentReference<IPoolableItem>
{
    public AssetReferenceSpawnableObject(string guid) : base(guid) { }
}
```

### ComponentReference

Generic base class for component-specific AssetReferences:

```csharp
public class ComponentReference<TComponent> : AssetReference
{
    public new AsyncOperationHandle<TComponent> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null);
    public new AsyncOperationHandle<TComponent> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false);
    public AsyncOperationHandle<TComponent> LoadAssetAsync();
    public void ReleaseInstance(AsyncOperationHandle<TComponent> op);
    
    public override bool ValidateAsset(Object obj);
    public override bool ValidateAsset(string path);
}
```

### Addressable Integration

The system seamlessly handles both regular prefabs and Addressable assets:

```csharp
// In PoolItemVO configuration
public override object Asset => IsAddressable ? (object)AddressablePrefab : Prefab;
```

The `AddressableLoadSubService` manages:
- Asynchronous loading of Addressable assets
- Caching of loaded handles to prevent redundant loading
- Proper cleanup and unloading of assets
- Reference counting for memory management

## Pool Lifecycle

### Initialization Flow

The initialization process follows a robust, signal-driven pattern:

```
Scene Load → Context Start → Mediator Binding → Signal Dispatch → Command Execution → Pool Creation
```

1. **Scene Setup**: `PoolServiceRoot` with `PoolRootAdapter` exists in scene
2. **Context Initialization**: `PoolServiceContext` starts and binds components
3. **Mediator Registration**: `PoolConfigAdapterView` gets bound to its mediator
4. **Signal Dispatch**: Mediator dispatches `RegisterPoolConfigs` signal
5. **Command Execution**: `RegisterPoolConfigCommand` processes configurations
6. **Pool Creation**: Configured pools are created and pre-warmed

### Group Management

Groups provide logical organization of related pools:

```csharp
// Auto-initialize specific group
_poolService.InitializeTag("EnemyPools");

// Create entire group manually
_poolService.Create.Group("WeaponEffects");

// Destroy entire group
_poolService.Destroy.Group("LevelSpecificPools");

// Return all active items in group
_poolService.Return.Group("TemporaryEffects");
```

### Item Lifecycle

Individual items follow this lifecycle:

```
Configuration → Creation → Pool Storage → Retrieval → Active Use → Return → Pool Storage
```

1. **Configuration**: Item defined in `CD_PoolGroup` ScriptableObject
2. **Creation**: Item instantiated during pool initialization or on-demand
3. **Pool Storage**: Item stored in passive pool (inactive state)
4. **Retrieval**: Item retrieved via `Get()` API, moved to active pool
5. **Active Use**: Item used by game logic
6. **Return**: Item returned via `Dismiss()` or manual return
7. **Pool Storage**: Item returned to passive pool for reuse

## Memory Management

### Pool Organization

The system organizes pools in a hierarchical structure:

```
[Pools] (Root Container)
├── PassivePools (Tag → ItemKey → LinkedList<IPoolableItem>)
└── ActivePools (Tag → ItemKey → LinkedList<IPoolableItem>)
```

### Active/Passive Tracking

- **Passive Pool**: Items ready for use (inactive GameObjects)
- **Active Pool**: Items currently in use (active GameObjects)
- **Automatic Tracking**: Items automatically moved between pools
- **Memory Efficient**: Uses `LinkedList<T>` for O(1) insertion/removal

### Cleanup Strategies

The system provides multiple cleanup strategies:

```csharp
// Cleanup individual item
_poolService.Destroy.Item(specificItem);

// Cleanup entire pool
_poolService.Destroy.Pool("EnemyBasic");

// Cleanup entire group
_poolService.Destroy.Group("LevelPools");

// Addressable cleanup handled automatically
// Memory released when pool is destroyed
```

## Async Operations

### Lazy Loading

Items can be configured for lazy loading:

```csharp
// In PoolItemVO configuration
public bool LazyLoad = true;
public int InitialCreateCount = 5;
```

With lazy loading:
- Items are not created during initialization
- First request triggers creation of `InitialCreateCount` items
- Subsequent requests use pre-created items
- Additional items created on-demand if `IsExtendable = true`

### Async Item Retrieval

The builder API supports both synchronous and asynchronous operations:

```csharp
// Synchronous retrieval
var enemy = _poolService.Get("BasicEnemy").Execute<Enemy>();

// Asynchronous retrieval
var boss = await _poolService.Get("BossEnemy").ExecuteAsync<Boss>();

// Async with callback
_poolService.Get("SpecialEffect")
    .WithCallback(OnEffectReady)
    .ExecuteAsync();
```

### Addressable Loading

Addressable assets are loaded asynchronously:

```csharp
// Addressable items automatically use async loading
var addressableItem = await _poolService.Get("AddressableEnemy").ExecuteAsync<Enemy>();
```

The `AddressableLoadSubService` handles:
- Preventing duplicate loading operations
- Caching loaded handles
- Proper async/await patterns
- Memory management

## API Reference

### PoolService Methods

```csharp
// Initialization
void AutoInitialize()                    // Initialize auto-marked pools
void ManualInitialize()                  // Initialize all pools  
void InitializeTag(string tag)           // Initialize specific group

// Item Retrieval
IPoolBuilderSubService Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)

// Sub-Service Access
CreateSubService Create { get; }         // Pool creation operations
ReturnSubService Return { get; }         // Item return operations  
CheckSubService Check { get; }           // Pool state queries
```

### Builder API

```csharp
// Fluent Configuration
IPoolBuilderSubService Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null)
IPoolBuilderSubService WithParent(Transform parent)
IPoolBuilderSubService WithCallback(Action<IPoolableItem> callback)

// Execution
IPoolableItem Execute()                  // Synchronous retrieval
T Execute<T>() where T : class, IPoolableItem
Task<IPoolableItem> ExecuteAsync()       // Asynchronous retrieval  
Task<T> ExecuteAsync<T>() where T : class, IPoolableItem
```

### Sub-Service APIs

#### CreateSubService
```csharp
void Group(string tag)                   // Create entire group
void CreatePool(string itemKey)          // Create specific pool
```

#### DestroySubService
```csharp
void Group(string tag)                   // Destroy entire group
void Pool(string itemKey)                // Destroy specific pool
void Item(IPoolableItem item)            // Destroy individual item
```

#### ReturnSubService
```csharp
void Item(IPoolableItem item)            // Return single item
void Group(string tag)                   // Return all active items in group
```

#### CheckSubService
```csharp
bool IsTagReady(string tag)              // Check if group is initialized
```

## Practical Implementation

### Basic Usage

#### Simple Item Retrieval

```csharp
public class EnemySpawner : MonoBehaviour
{
    [Inject] private IPoolService _poolService;
    
    public void SpawnEnemy()
    {
        var enemy = _poolService.Get("BasicEnemy").Execute<Enemy>();
        enemy.transform.position = spawnPoint.position;
        enemy.Initialize();
    }
}
```

#### With Parent and Callback

```csharp
public class WeaponSystem : MonoBehaviour
{
    [Inject] private IPoolService _poolService;
    
    public void FireProjectile()
    {
        _poolService.Get("Bullet")
            .WithParent(projectileContainer)
            .WithCallback(OnBulletCreated)
            .ExecuteAsync();
    }
    
    private void OnBulletCreated(IPoolableItem item)
    {
        var bullet = item as Bullet;
        bullet.Fire(targetPosition);
    }
}
```

### Advanced Scenarios

#### Custom Poolable Item

```csharp
public class Enemy : PoolableItem
{
    [SerializeField] private float _health = 100f;
    [SerializeField] private float _speed = 5f;
    
    private Vector3 _startPosition;
    
    public override void OnInitialized()
    {
        // Called once when item is first created
        _startPosition = transform.position;
    }
    
    public override void OnGetFromPool()
    {
        // Called each time item is retrieved from pool
        _health = 100f;
        gameObject.SetActive(true);
        transform.position = _startPosition;
    }
    
    public override void OnReturnToPool()
    {
        // Called when item is returned to pool
        gameObject.SetActive(false);
        // Reset any runtime state
    }
    
    public void TakeDamage(float damage)
    {
        _health -= damage;
        if (_health <= 0f)
        {
            Die();
        }
    }
    
    private void Die()
    {
        // Play death effects, then return to pool
        PlayDeathAnimation();
        Dismiss(); // Returns to pool automatically
    }
}
```

#### Addressable Pool Configuration

```csharp
// Create a CD_PoolGroup ScriptableObject
[CreateAssetMenu(fileName = "EnemyPools", menuName = "Game/Pool Groups/Enemy Pools")]
public class EnemyPoolGroup : CD_PoolGroup
{
    // Configuration handled by base class
}

// Configure in inspector:
// - Pool Key: "HeavyEnemy"
// - Initial Create Count: 3
// - Is Extendable: true
// - Is Addressable: true
// - Addressable Prefab: Reference to addressable enemy prefab
```

#### Group-Based Management

```csharp
public class LevelManager : MonoBehaviour
{
    [Inject] private IPoolService _poolService;
    
    public void StartLevel()
    {
        // Initialize level-specific pools
        _poolService.InitializeTag("Level1Enemies");
        _poolService.InitializeTag("Level1Effects");
    }
    
    public void EndLevel()
    {
        // Clean up level-specific pools
        _poolService.Return.Group("Level1Enemies");
        _poolService.Return.Group("Level1Effects");
        
        // Or destroy them completely
        _poolService.Destroy.Group("Level1Enemies");
        _poolService.Destroy.Group("Level1Effects");
    }
}
```

#### Async Loading with Progress

```csharp
public class AssetLoader : MonoBehaviour
{
    [Inject] private IPoolService _poolService;
    
    public async Task LoadBossAsync()
    {
        try
        {
            var boss = await _poolService.Get("MegaBoss").ExecuteAsync<Boss>();
            boss.transform.position = bossSpawnPoint.position;
            boss.StartBossFight();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load boss: {e.Message}");
        }
    }
}
```

### Performance Tips

1. **Pool Sizing**: Set appropriate `InitialCreateCount` based on expected concurrent usage
2. **Lazy Loading**: Use for infrequently used items to reduce startup time
3. **Group Organization**: Group related items to enable efficient batch operations
4. **Addressable Strategy**: Use Addressables for large assets or platform-specific content
5. **Return Promptly**: Return items to pools as soon as they're no longer needed

## Integration with FlowIoC

### Context Setup

```csharp
public class PoolServiceContext : Context
{
    private IPoolService _service;
    private PoolServiceInternalSignals _poolServiceInternalSignals;

    public override void InjectionBindings()
    {
        base.InjectionBindings();

        // Main service
        _service = InjectionBinderCrossContext.Bind<IPoolService, PoolService>();

        // Models
        InjectionBinder.Bind<IPoolConfigModel, PoolConfigModel>();
        InjectionBinder.Bind<IPoolRuntimeModel, PoolRuntimeModel>();

        // Sub-services
        InjectionBinder.Bind<IPoolBuilderSubService, PoolBuilderSubService>();
        InjectionBinder.Bind<CreateSubService>();
        InjectionBinder.Bind<DestroySubService>();
        InjectionBinder.Bind<ReturnSubService>();
        InjectionBinder.Bind<CheckSubService>();
        InjectionBinder.Bind<LoadSubService>();
        InjectionBinder.Bind<AddressableLoadSubService>();
    }

    public override void SignalBindings()
    {
        base.SignalBindings();
        _poolServiceInternalSignals = InjectionBinder.Bind<PoolServiceInternalSignals>();
    }

    public override void CommandBindings()
    {
        base.CommandBindings();
        CommandBinder.Bind(_poolServiceInternalSignals.RegisterPoolConfigs).To<RegisterPoolConfigCommand>();
        CommandBinder.Bind(_poolServiceInternalSignals.UnRegisterConfigs).To<UnregisterPoolConfigCommand>();
    }

    public override void MediationBindings()
    {
        base.MediationBindings();
        MediationBinder.Bind<PoolConfigAdapterView>().To<PoolConfigAdapterMediator>();
    }

    public override void Setup()
    {
        base.Setup();
        _service.AutoInitialize();
    }
}
```

### Signal System

The system uses signals for decoupled communication:

```csharp
public class PoolServiceInternalSignals : ISignalHolder
{
    public Signal<SerializedDictionary<string, PoolGroupEntry>> RegisterPoolConfigs = new();
    public Signal<SerializedDictionary<string, PoolGroupEntry>> UnRegisterConfigs = new();
}
```

Commands handle the signal processing:

```csharp
public class RegisterPoolConfigCommand : Command
{
    [SignalParam] private SerializedDictionary<string, PoolGroupEntry> _configs { get; set; }
    [Inject] private IPoolConfigModel _poolConfigModel { get; set; }

    public override void Execute()
    {
        foreach (var config in _configs)
        {
            _poolConfigModel.RegisterPoolConfig(config);
        }
    }
}
```

### View-Mediator Pattern

The system uses the view-mediator pattern for configuration:

```csharp
// View Component
public class PoolConfigAdapterView : MonoBehaviour, IView
{
    [SerializeField] private SerializedDictionary<string, PoolGroupEntry> _poolConfigs;
    public Action<SerializedDictionary<string, PoolGroupEntry>> UnRegisterScreenConfig = delegate { };
    
    public SerializedDictionary<string, PoolGroupEntry> GetPoolConfigs() => _poolConfigs;
}

// Mediator
public class PoolConfigAdapterMediator : IMediator
{
    [Inject] private PoolConfigAdapterView _view { get; set; }
    [InjectSignal] private PoolServiceInternalSignals _signals { get; set; }

    public void OnRegister()
    {
        var configs = _view.GetPoolConfigs();
        _signals.RegisterPoolConfigs.Dispatch(configs);
    }
}
```

## Best Practices

### Configuration Best Practices

1. **Logical Grouping**: Group related items together (e.g., "EnemyPools", "WeaponEffects")
2. **Meaningful Keys**: Use descriptive, unique keys for each pool item
3. **Appropriate Sizing**: Set initial counts based on expected usage patterns
4. **Lazy Loading Strategy**: Use lazy loading for infrequently used items
5. **Addressable Strategy**: Use Addressables for large or platform-specific assets

### Code Best Practices

1. **Inject Services**: Always inject `IPoolService` rather than accessing directly
2. **Use Typed Returns**: Prefer `Execute<T>()` over `Execute()` for type safety
3. **Handle Async Properly**: Use proper async/await patterns for async operations
4. **Return Promptly**: Return items to pools as soon as they're no longer needed
5. **Implement Lifecycle Methods**: Always implement `OnGetFromPool()` and `OnReturnToPool()`

### Performance Best Practices

1. **Pre-warm Pools**: Use appropriate initial counts to avoid runtime allocation
2. **Batch Operations**: Use group operations when possible for better performance
3. **Monitor Pool Usage**: Use `CheckSubService` to monitor pool health
4. **Cleanup Regularly**: Destroy unused pools to free memory
5. **Profile Memory**: Monitor memory usage and adjust pool sizes accordingly

## Troubleshooting

### Common Issues

#### Item Not Found
**Problem**: `Get()` returns null
**Solutions**:
- Verify item key exists in configuration
- Check if pool group is initialized
- Ensure configuration is properly loaded

#### Addressable Loading Fails
**Problem**: Addressable items fail to load
**Solutions**:
- Verify Addressable asset is properly configured
- Check Addressable groups are built
- Ensure proper GUIDs in AssetReferences

#### Memory Leaks
**Problem**: Memory usage increases over time
**Solutions**:
- Ensure items are properly returned to pools
- Check for circular references in pooled objects
- Verify Addressable assets are properly unloaded

#### Performance Issues
**Problem**: Poor performance during item retrieval
**Solutions**:
- Increase initial pool sizes
- Use lazy loading for infrequently used items
- Profile pool usage and adjust accordingly

### Debugging Tips

1. **Enable Console Logging**: The system includes comprehensive logging
2. **Use Model Viewer**: Check `[ShowInModelViewer]` attributes for runtime inspection
3. **Monitor Pool States**: Use `CheckSubService` to query pool health
4. **Profile Memory**: Use Unity's Memory Profiler to track pool usage
5. **Test Async Operations**: Verify async operations complete properly

### Debug Methods

```csharp
// Check pool state
bool isReady = _poolService.Check.IsTagReady("EnemyPools");

// Monitor pool counts (would need additional debug service)
int activeCount = _poolService.GetActiveCount("BasicEnemy");
int passiveCount = _poolService.GetPassiveCount("BasicEnemy");

// Log pool configuration
foreach (var tag in _poolConfigModel.GetAllTags())
{
    Debug.Log($"Pool Group: {tag}");
    foreach (var item in _poolConfigModel.GetItemConfigsByTag(tag))
    {
        Debug.Log($"  Item: {item.PoolKey}, Count: {item.InitialCreateCount}");
    }
}
```

---

This documentation provides a comprehensive guide to the FlowIoC PoolModule. For additional support or feature requests, please refer to the FlowIoC documentation or contact the development team. 