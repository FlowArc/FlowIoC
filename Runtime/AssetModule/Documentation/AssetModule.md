# Assets

The Asset Module is a thin, load-once layer over Addressables. You ask for an asset by
key and get it back; ask twice and you get the same instance, without a second load.

```csharp
var icon = await _assetService.LoadAssetAsync<Sprite>("icon_sword");
```

It manages **loading and releasing**, not instancing. `LoadAssetAsync<GameObject>`
gives you the prefab — you call `Instantiate` yourself, or hand the prefab to the
[Pool Module](../../PoolModule/Documentation/PoolModule.md).

- [Setting Up](#setting-up)
- [Loading a Single Asset](#loading-a-single-asset)
- [Runtime Groups](#runtime-groups)
- [Signals](#signals)
- [Scenarios](#scenarios)
- [Pitfalls](#pitfalls)

---

## Setting Up

`AssetServiceRoot` binds `IAssetService` cross-context. It carries an
`initializeOrder` of `-1`, which is the last seat in the Services band, so it starts
before every module that might ask it for an asset — keep it negative if you move the
Root.

```csharp
[Inject] private IAssetService _assetService { get; set; }
```

---

## Loading a Single Asset

```csharp
// The usual form. Awaits the load, or returns the cached asset immediately.
var icon = await _assetService.LoadAssetAsync<Sprite>("icon_sword");

// Synchronous. Only safe when the asset is already loaded.
var config = _assetService.LoadAsset<CD_WeaponConfig>("weapon_config");

// Cache probe — never triggers a load.
if (_assetService.TryGetAsset<Sprite>("icon_sword", out var cached))
    _view.SetIcon(cached);

// Release one asset.
_assetService.Release("icon_sword");
```

**Load-once** is the contract worth remembering: a key is loaded a single time. A
second `LoadAssetAsync` for a key already loaded returns the cached asset; a second
call while the first is still in flight waits for that same load rather than starting
another. You never need to guard a load behind your own "is it loading yet" flag.

Loading from a command, retaining across the await:

```csharp
public class LoadWeaponIconCommand : Command
{
    [Inject] private IAssetService _assetService { get; set; }

    [SignalParam] private string _weaponId { get; set; }

    public override async void Execute()
    {
        Retain();

        var icon = await _assetService.LoadAssetAsync<Sprite>($"icon_{_weaponId}", "weapons");
        if (icon == null)
        {
            FlowLogger.LogError(FlowLogType.WeaponModule, $"Icon missing for '{_weaponId}'.");
            Stop();
            return;
        }

        _weaponModel.SetIcon(_weaponId, icon);
        Release();
    }
}
```

---

## Runtime Groups

A group is a `groupId` you attach to loads so you can release them together. Groups
are formed at runtime — they are not Addressables groups.

```csharp
// Load everything carrying an Addressables label, tracked under a group.
await _assetService.LoadGroupByLabelAsync<Sprite>("menu_icons", "menu");

// Load a known set of keys into a group.
await _assetService.LoadAssetsAsync<AudioClip>("combat", new[] { "sfx_hit", "sfx_death" });

// Track an already-loaded asset under a group too.
_assetService.AddToGroup("combat", "sfx_hit");

// Release everything in the group.
_assetService.ReleaseGroup("combat");

// Inspect.
_assetService.IsGroupLoaded("combat");
_assetService.GetGroupKeys("combat");
```

Group by *lifetime*, the same way you group pools: `menu`, `combat`, `boss_phase_2`.
Then a phase change is one `ReleaseGroup` call instead of a list of keys maintained by
hand.

When the label and the group name are the same thing — which is the common case —
`LoadGroupByLabelAsync` uses the label as the `groupId` by default.

---

## Signals

The module exposes signals so other modules can drive it without injecting the
service:

```csharp
_assetSignals.InComing.LoadGroupByLabel.Dispatch("menu_icons");
_assetSignals.InComing.ReleaseGroup.Dispatch("menu_icons");
_assetSignals.InComing.ReleaseAsset.Dispatch("icon_sword");
```

```csharp
_assetSignals.OutGoing.GroupLoaded.Connect(_menuSignals.Incoming.IconsReady);
_assetSignals.OutGoing.GroupReleased.Connect(_menuSignals.Incoming.IconsGone);
_assetSignals.OutGoing.AssetLoadFailed.Connect(_diagnosticsSignals.Incoming.ReportAssetFailure);
```

Use the signals when a whole phase of the game is loading — the connector wiring makes
the dependency visible — and inject the service directly when one command needs one
asset.

---

## Scenarios

### Let load-once do the deduplication

```csharp
// ✅ Three screens asking for the same icon cause one load.
var icon = await _assetService.LoadAssetAsync<Sprite>("icon_sword");
```

```csharp
// ❌ A hand-rolled cache in front of a service that already caches, with its own
//    race condition when two callers arrive before the first load finishes.
if (!_myCache.TryGetValue(key, out var icon))
{
    icon = await _assetService.LoadAssetAsync<Sprite>(key);
    _myCache[key] = icon;
}
```

### Release by group at a phase boundary

```csharp
// ✅ One line when the match ends.
_assetService.ReleaseGroup("combat");
```

```csharp
// ❌ A list of keys somebody has to remember to update every time an asset is added.
_assetService.Release("sfx_hit");
_assetService.Release("sfx_death");
// ...and the three added last sprint
```

### Retain across the await

```csharp
// ✅ The chain waits for the asset.
public override async void Execute()
{
    Retain();
    var config = await _assetService.LoadAssetAsync<CD_LevelConfig>(_levelKey);
    _levelModel.Apply(config);
    Release();
}
```

```csharp
// ❌ Execute returns at the await, and the next command reads a model that has not
//    been filled yet.
public override async void Execute()
{
    var config = await _assetService.LoadAssetAsync<CD_LevelConfig>(_levelKey);
    _levelModel.Apply(config);
}
```

### `TryGetAsset` when you must not stall

```csharp
// ✅ A per-frame path that uses the icon if it is there and skips it otherwise.
if (_assetService.TryGetAsset<Sprite>(key, out var icon))
    _view.SetIcon(icon);
```

```csharp
// ❌ LoadAsset synchronously in a hot path, hoping it is cached. When it is not, it
//    blocks — and it only fails on the first playthrough, or on a slow device.
_view.SetIcon(_assetService.LoadAsset<Sprite>(key));
```

### Prefabs go to the pool, not to `Instantiate` in a loop

```csharp
// ✅ Load once, pool the instances.
var prefab = await _assetService.LoadAssetAsync<GameObject>("bullet_basic");
// ...registered as a pool item, then _poolService.Get<Bullet>("bullet_basic", root)
```

```csharp
// ❌ The asset service is not an instancer. This creates and destroys objects at
//    combat rates.
var prefab = await _assetService.LoadAssetAsync<GameObject>("bullet_basic");
Instantiate(prefab, position, rotation);
```

---

## Pitfalls

### The asset comes back null

The key does not exist in any Addressables group, the label is misspelled, or the
requested type does not match the asset. `AssetLoadFailed` fires with the key —
connect it to your diagnostics module so a missing asset is a reported event rather
than a null-reference somewhere downstream.

### `LoadAsset` returns null although the asset exists

The synchronous overload does not start a load. It is a cache read. Use
`LoadAssetAsync`, or warm the group first and check `IsGroupLoaded`.

### An asset stays in memory after `Release`

Releasing a key that is also tracked in a group only drops that one reference; the
group still holds it. Release the group, or do not add the key to a group you do not
intend to manage as a unit.

### The service is not ready yet

`AssetServiceRoot` uses `initializeOrder = -1` so it binds ahead of the game's own
modules. If you raised that value above a consumer's, or put the Root in a scene loaded
after its consumers, a context can reach `Setup()` with no asset service bound. Keep the
Root in the bootstrap scene with its default order.

### Two groups fight over the same asset

Group membership is additive: an asset can be in `menu` and in `shop` at once, and it
survives until both are released. That is usually correct — but it means
`ReleaseGroup("menu")` is not a guarantee that memory came back. `GetGroupKeys` tells
you who else is holding it.

---

## Related

- [README — FlowIoC at a Glance](../../../README.md#flowioc-at-a-glance)
- [Pooling](../../PoolModule/Documentation/PoolModule.md) — what to do with a loaded
  prefab
- [Screens](../../ScreenModule/Documentation/ScreenModule.md) — screens load their own
  prefabs through this layer
- [Commands](../../BaseModule/Controller/Documentation/Controller.md) — retaining
  across an `await`
