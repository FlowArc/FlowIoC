# Pooling

The Pool Module keeps reusable GameObjects — bullets, damage numbers, enemies, VFX —
alive between uses instead of instantiating and destroying them. You describe what to
pool in a `CD_PoolGroup` asset, and ask for items by key:

```csharp
var bullet = _poolService.Get<Bullet>("bullet_basic", _projectileRoot);
```

When the object is done with itself, it goes home:

```csharp
bullet.Dismiss();
```

- [Setting Up](#setting-up)
- [Making an Object Poolable](#making-an-object-poolable)
- [Getting and Returning](#getting-and-returning)
- [Groups](#groups)
- [Addressable Items](#addressable-items)
- [Scenarios](#scenarios)
- [Pitfalls](#pitfalls)

---

## Setting Up

### 1. The pool service

`PoolServiceRoot` binds `IPoolService` cross-context. Put it in your bootstrap scene
once, then inject it wherever you spawn things:

```csharp
[Inject] private IPoolService _poolService { get; set; }
```

### 2. A pool group asset

Create one via **Assets ▸ Create ▸ FlowIoC ▸ PoolModule ▸ Data ▸ CD_PoolGroup**. Each
entry describes one poolable prefab:

| Field | Meaning |
|---|---|
| `PoolKey` | The string you pass to `Get`. Unique within the group. |
| `InitialCreateCount` | How many to instantiate up front. Default 10. |
| `IsExtendable` | `true`: the pool grows past its initial count when it runs dry. `false`: `Get` returns nothing. |
| `LazyLoad` | `true`: create nothing until the first `Get`. |
| `IsAddressable` | `true`: load through `AddressablePrefab`. `false`: use the direct `Prefab` reference. |
| `Prefab` / `AddressablePrefab` | The thing to pool, per the flag above. |

### 3. Registering the group

A `PoolConfigAdapterView` component in the scene carries a dictionary of group key →
`PoolGroupCVO`, and registers those groups when its context starts.

| `PoolGroupCVO` field | Meaning |
|---|---|
| `Group` | The `CD_PoolGroup` asset |
| `AutoInitialize` | Create the items as soon as the group is registered |
| `GroupSpecificPools` | Keep this group's pools separate from other groups using the same keys |
| `UnregisterWhenViewDestroyed` (on the view) | Tear the group down when this GameObject goes away |

That component is why pooling is scene-scoped by default: the combat scene registers
combat pools, the menu scene does not pay for them.

If you would rather not create the items at registration time, leave
`AutoInitialize` off and warm them when you are ready:

```csharp
_poolService.InitializeGroup("combat");
_poolService.InitializeAll();
```

---

## Making an Object Poolable

Inherit `PoolableItem`, or implement `IPoolableItem` if you already have a base class:

```csharp
public class Bullet : PoolableItem
{
    [SerializeField] private TrailRenderer _trail;

    private float _lifetime;

    // Once, when the pool first creates this instance.
    public override void OnInitialized()
    {
        _trail.Clear();
    }

    // Every time it comes out of the pool.
    public override void OnGetFromPool()
    {
        _lifetime = 0f;
        _trail.Clear();
    }

    // Every time it goes back in.
    public override void OnReturnToPool()
    {
        _trail.Clear();
    }

    private void Update()
    {
        _lifetime += Time.deltaTime;
        if (_lifetime > 3f)
            Dismiss();
    }
}
```

The three hooks answer three different questions:

| Hook | Runs | Use for |
|---|---|---|
| `OnInitialized()` | once per instance, at creation | caching components, one-time wiring |
| `OnGetFromPool()` | every checkout | resetting state for this use |
| `OnReturnToPool()` | every return | stopping effects, clearing references |

`Dismiss()` is how an object returns itself — it invokes `ReturnToPoolAction`, which
the pool set when it handed the object out. An object that does not know who spawned
it can still go home.

If you implement `IPoolableItem` directly rather than inheriting `PoolableItem`,
`Dismiss()` must be written as `ReturnToPoolAction?.Invoke(this)`.

---

## Getting and Returning

```csharp
// Typed — the usual form.
var bullet = _poolService.Get<Bullet>("bullet_basic", _projectileRoot);

// Untyped, when the caller does not care what it is.
IPoolableItem item = _poolService.Get("vfx_hit", _vfxRoot);

// With a callback, for items that may load asynchronously.
_poolService.Get<Enemy>("enemy_grunt", _enemyRoot, spawned => spawned.SetActive(true));
```

The `parent` argument is where the object is parented on checkout. Keeping an
"active" root separate from the pool's own storage root makes the hierarchy readable
while the game runs.

Returning happens three ways:

```csharp
bullet.Dismiss();                       // the object returns itself
_poolService.Return.Item(bullet);       // a system returns it
_poolService.Return.Group("combat");    // everything in a group comes home
```

`Return.Group` is what you call when a match ends: every bullet, corpse and floating
number returns in one line, without hunting for references.

### Checking state

```csharp
_poolService.Check.IsGroupConfigExist("combat");   // is the group registered?
_poolService.Check.IsGroupCreated("combat");       // have its items been instantiated?
_poolService.Check.IsGroupReady("combat");         // both of the above
```

---

## Groups

A group is the unit of registration, warming and return. Group by *lifetime*, not by
category:

```
combat_core     always present during a match — bullets, hit VFX, damage numbers
combat_boss     only while a boss is alive
menu_fx         menu confetti and button sparkles
```

That grouping lets you warm `combat_core` on the loading screen, warm `combat_boss`
when the boss spawns, and return both when the match ends — without a per-prefab list
anywhere in code.

`GroupSpecificPools` matters when two groups use the same `PoolKey` for different
prefabs. With it on, each group keeps its own pool for that key; with it off, they
share.

Creating a group at runtime is possible when the config is not known ahead of time:

```csharp
_poolService.Create.Group("boss_phase_2", poolGroupCVO);
```

---

## Addressable Items

Set `IsAddressable` and fill `AddressablePrefab` with an
`AssetReferenceSpawnableObject`. The pool then loads through Addressables instead of
holding a direct prefab reference.

This is the difference between a scene that references every VFX prefab it might ever
show — and therefore loads all of them with the scene — and one that pulls them in on
demand. For anything large or rarely used, prefer addressable.

Addressable items load asynchronously, so a `Get` on a cold pool may not be able to
hand you the object in the same frame. Use the callback overload, or warm the group
first with `InitializeGroup` and be certain `IsGroupCreated` is true before you rely
on a synchronous return.

---

## Scenarios

### Reset in `OnGetFromPool`, not in `Awake`

```csharp
// ✅ Runs on every checkout, which is what "a fresh bullet" means here.
public override void OnGetFromPool()
{
    _lifetime = 0f;
    _rigidbody.linearVelocity = Vector3.zero;
    _trail.Clear();
}
```

```csharp
// ❌ Awake runs once per instance. The second bullet out of the pool keeps the first
//    one's velocity and trail — a bug that never appears on the first shot.
private void Awake()
{
    _lifetime = 0f;
    _rigidbody.linearVelocity = Vector3.zero;
}
```

### `Dismiss()`, never `Destroy()`

```csharp
// ✅ The object goes back to the pool and is reused.
if (_lifetime > 3f)
    Dismiss();
```

```csharp
// ❌ Destroying a pooled object removes it from the pool permanently. The pool
//    believes it still owns the instance, and a later Get hands out a destroyed
//    reference — a MissingReferenceException far from this line.
if (_lifetime > 3f)
    Destroy(gameObject);
```

### Return the group, not each object

```csharp
// ✅ One line at the end of a match.
_poolService.Return.Group("combat_core");
```

```csharp
// ❌ Every system has to remember what it spawned, and one that forgets leaks an
//    object that is never returned and never collected.
foreach (var bullet in _spawnedBullets) _poolService.Return.Item(bullet);
foreach (var vfx in _spawnedVfx)         _poolService.Return.Item(vfx);
// ... and the one list somebody forgot to add
```

### Size the pool to the peak, and decide about extending

```csharp
// ✅ InitialCreateCount 64 for a weapon that fires 60 rounds a second, IsExtendable
//    on, so a burst above the estimate still works.
```

```csharp
// ❌ InitialCreateCount 4 with IsExtendable off. Above four concurrent bullets the
//    gun silently stops firing, and nothing in the game says why.
```

Turn `IsExtendable` off only when a hard cap is the behaviour you want — a maximum
number of on-screen enemies, for example — and handle the empty return.

### Group by lifetime

```csharp
// ✅ Warmed and returned together, because they live and die together.
"combat_core"  → bullet, hit_vfx, damage_number
"combat_boss"  → boss_projectile, shockwave, boss_death_vfx
```

```csharp
// ❌ Grouped by what they look like. Now nothing can be warmed or returned as a
//    unit, and the boss VFX are resident during the tutorial.
"all_vfx"      → hit_vfx, shockwave, confetti, boss_death_vfx, menu_sparkle
```

### Keep the active root separate

```csharp
// ✅ Checked-out objects are parented under a visible root; the pool keeps the idle
//    ones elsewhere. The hierarchy shows what is actually in play.
_poolService.Get<Bullet>("bullet_basic", _projectileActiveRoot);
```

```csharp
// ❌ Everything under one transform. Two hundred inactive objects sit between you
//    and the ten you are debugging.
_poolService.Get<Bullet>("bullet_basic", transform);
```

---

## Pitfalls

### `Get` returns null

Either the group is not ready, or the pool is empty and not extendable:

```csharp
if (!_poolService.Check.IsGroupReady("combat_core"))
    _poolService.InitializeGroup("combat_core");
```

For an addressable group, "not ready" can also mean the load is still in flight.
Warm on the loading screen, not on the first shot.

### The object comes out with the previous run's state

Reset in `OnGetFromPool()`. `Awake` and `Start` run once per instance, not once per
checkout — that is the entire point of a pool, and the most common source of pooling
bugs.

Coroutines are a particular trap: a coroutine started before the object was returned
keeps running unless you stop it in `OnReturnToPool()`.

### `MissingReferenceException` on a pooled object

Something called `Destroy` on it. Pooled objects are returned, never destroyed — and
that includes indirect destruction, such as being parented under a GameObject that
gets destroyed with its scene. Parent checked-out items under a root that outlives
them, or return the group before unloading the scene.

### The pool grows without bound

`IsExtendable` is on and nothing returns the items. Every extension is permanent —
the pool keeps the instances it created. Check that every checkout path has a
matching `Dismiss()`, including the ones where the object dies early, and call
`Return.Group` at the end of the phase.

### Two prefabs fight over one key

Two groups declaring the same `PoolKey` share a pool unless `GroupSpecificPools` is
on, so a `Get` can hand back the other group's prefab. Either namespace your keys
(`combat.bullet`, `menu.bullet`) or turn on `GroupSpecificPools`.

### The group disappears when a scene unloads

`PoolConfigAdapterView` with `UnregisterWhenViewDestroyed` on tears the group down
with its GameObject. That is usually what you want — but if the pool must outlive the
scene that registered it, put the adapter on an object owned by a `SingletonRoot`
instead.

---

## Related

- [README — FlowIoC at a Glance](../../../README.md#flowioc-at-a-glance)
- [Base Module](../../BaseModule/Documentation/BaseModule.md) — injection, contexts
- [Screens](../../ScreenModule/Documentation/ScreenModule.md) — screens have their
  own pooling, separate from this one
