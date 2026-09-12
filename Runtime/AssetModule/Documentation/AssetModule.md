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
- [Progress, Background and Downloads](#progress-background-and-downloads)
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

**This is the package's one door to Addressables.** The screen service and the pool
service load their addressable prefabs through it too, so `AssetServiceRoot` has to be
in every scene that has an addressable screen or an addressable pool item — the Create
Module panel puts it in the test scene it builds for a screen. A scene whose screens are
all `Resource` screens and whose pools hold only direct prefabs does not need it. When it
is missing, the first addressable load reports it and answers nothing:

```
[ScreenService.Load] 'SettingsScreenView' is addressable and AssetServiceRoot is not in the scene.
Put AssetServiceRoot in the scene; it is what loads addressables.
```

Each of the two services claims what it loads under an owner of its own —
`Screen/<managerId>` and `Pool/<groupKey>` — so a prefab a pool and a screen both touch
is loaded once and released when the last of them lets go.

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

## Progress, Background and Downloads

A group load takes an `AssetLoadOptions`:

```csharp
await _assetService.LoadGroupByLabelAsync<GameObject>("level30", "Preload/Level30",
    new AssetLoadOptions
    {
        Progress = new Progress<float>(fraction => step.Progress(fraction)),
        Background = true
    });
```

- **`Progress`** is `0..1`, the mean of the loads still in flight, reported once per
  frame and `1f` once at the end. A group whose assets are already in memory reports
  `1f` once. This is what a loading step's `Progress(float)` is fed from.
- **`Background`** lowers `Application.backgroundLoadingPriority` to `Low` for as long
  as any background load is in flight, and puts back the value it found when the last
  one ends. It is a global setting: a foreground load that overlaps a background one
  runs at the lower priority too, so a game that cares lets its boot finish before it
  preloads silently.

For a remote catalogue, two more calls:

```csharp
long bytes = await _assetService.GetDownloadSizeAsync("level30");      // 0 when cached

bool ok = await _assetService.DownloadDependenciesAsync("level30",
    new AssetLoadOptions { Progress = new Progress<float>(step.Progress) });
```

A download is not a claim. It brings the bundles into Addressables' cache and holds
nothing afterwards; the loads that follow find them there. A download that fails
answers `false` and dispatches `AssetLoadFailed` with the key.

---

## Signals

The module exposes signals so other modules can drive it without injecting the
service:

```csharp
_assetSignals.Incoming.LoadGroupByLabel.Dispatch("menu_icons");
_assetSignals.Incoming.ReleaseGroup.Dispatch("menu_icons");
_assetSignals.Incoming.ReleaseAsset.Dispatch("icon_sword");
```

```csharp
_assetSignals.Outgoing.GroupLoaded.Connect(_menuSignals.Incoming.IconsReady);
_assetSignals.Outgoing.GroupReleased.Connect(_menuSignals.Incoming.IconsGone);
_assetSignals.Outgoing.AssetLoadFailed.Connect(_diagnosticsSignals.Incoming.ReportAssetFailure);
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

### A pool and a screen share a prefab

Nothing to write. The pool claims the prefab under `Pool/<groupKey>` and the screen
under `Screen/<managerId>`; Addressables keys its own cache by resource location, so
the address the screen names and the GUID the pool's `AssetReference` carries share
one operation and one loaded asset. The registry keeps the two keys as they were given
— `TryGetAsset` answers under the spelling that was loaded — and decides only when a
claim ends: the asset goes when the last owner releases.

### Preload a level silently, then let the pool find it

```csharp
// At level 20, beside the game, under a silent loading set:
await _assetService.LoadGroupByLabelAsync<GameObject>("level30", "Preload/Level30",
    new AssetLoadOptions { Background = true });

// At level 30 the pool's GetAsync loads the same prefabs and finds the operation cached.
// The pool's claim is added beside the preload's; releasing the preload keeps the pool's.
_assetService.ReleaseGroup("Preload/Level30");
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
- [Screens](../../ScreenModule/Documentation/ScreenModule.md) — screens load their
  prefabs through this layer, under `Screen/<managerId>`
- [Commands](../../BaseModule/Controller/Documentation/Controller.md) — retaining
  across an `await`
- HitNPoP's `AudioModule` is the worked example of the group lifecycle: a group per
  scene of clips, loaded by label on entry and released as one on exit
