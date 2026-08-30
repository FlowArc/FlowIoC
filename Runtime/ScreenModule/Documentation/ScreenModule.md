# Screens

The Screen Module opens, stacks, pools and unloads UI. You describe each screen once
in a `ScreenConfig` asset, then ask for it by type:

```csharp
await _screenService.Open<SettingsScreenView>().Show();
```

Everything else — loading the prefab from Addressables or Resources, finding the
right layer, hiding whatever was there, playing the animation, returning the instance
to the pool afterwards — is the module's job.

- [Setting Up](#setting-up)
- [Opening a Screen](#opening-a-screen)
- [Finding, Hiding, Unloading](#finding-hiding-unloading)
- [Preloading](#preloading)
- [Managers, Layers and Tags](#managers-layers-and-tags)
- [Screen Lifecycle Hooks](#screen-lifecycle-hooks)
- [Animations](#animations)
- [Scenarios](#scenarios)
- [Pitfalls](#pitfalls)

---

## Setting Up

Four things have to exist before `Open<T>()` works.

### 1. The screen service

`ScreenServiceRoot` binds `IScreenService` cross-context. Put it in your bootstrap
scene once. Every module then injects the service:

```csharp
[Inject] private IScreenService _screenService { get; set; }
```

### 2. A `ScreenManager` in the scene

`ScreenManager` is the MonoBehaviour that owns a canvas, a set of layers, and the list
of `ScreenConfig` assets it is allowed to open. Its `ManagerID` is what the
`managerId` parameter on every service call refers to — `0` is the default.

Use more than one manager when you have genuinely separate UI surfaces: a world-space
diegetic canvas and a screen-space HUD, for example. Otherwise one is enough.

### 3. A screen view

A screen view is an ordinary FlowIoC view that inherits `ScreenView`:

```csharp
[RequireComponent(typeof(ViewInjector))]
public class SettingsScreenView : ScreenView
{
    [SerializeField] private Toggle _musicToggle;
    [SerializeField] private Button _closeButton;

    public Action<bool> MusicToggled;
    public Action       CloseClicked;

    private void OnEnable()
    {
        _musicToggle.onValueChanged.AddListener(v => MusicToggled?.Invoke(v));
        _closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
    }

    private void OnDisable()
    {
        _musicToggle.onValueChanged.RemoveAllListeners();
        _closeButton.onClick.RemoveAllListeners();
    }
}
```

`OnEnable` and `OnDisable`, not `Awake` or `Start`. A screen is pooled: hiding it deactivates
the GameObject and parks it, and opening it again shows that same instance. `Awake` and `Start`
run once for a screen that opens fifty times, so a listener added there is added once and never
removed — which looks fine until you subscribe again somewhere else and every click fires twice.
Wiring on enable and unwiring on disable keeps one listener per open.

Its mediator is a normal `IMediator` — the screen module does not introduce a
different kind:

```csharp
public class SettingsScreenMediator : IMediator
{
    [Inject]       private SettingsScreenView _view    { get; set; }
    [InjectSignal] private SettingsSignals    _signals { get; set; }

    public void OnRegister()
    {
        _view.MusicToggled += OnMusicToggled;
        _view.CloseClicked += OnCloseClicked;
    }

    public void OnRemove()
    {
        _view.MusicToggled -= OnMusicToggled;
        _view.CloseClicked -= OnCloseClicked;
    }

    private void OnMusicToggled(bool on) => _signals.Outgoing.MusicToggled.Dispatch(on);
    private void OnCloseClicked()        => _signals.Incoming.Close.Dispatch();
}
```

### 4. A `ScreenConfig` asset

One `ScreenConfig` per screen tells the module where the prefab lives and how it
behaves by default. Edit them through *Tools ▸ FlowIoC ▸ Screen Config Manager*.

| Field | Meaning |
|---|---|
| `LoadType` | `Addressable`, `Resource` or `DirectPrefab` |
| `AddressableKey` / `ResourcePath` / `DirectPrefab` | Where the prefab comes from, per load type |
| `DefaultLayer` | The layer this screen opens in unless `OpenInLayer` overrides it |
| `Tag` | `Default` or `GroupA`…`GroupH` — used for bulk load, hide and unload |
| `HasShowAnimation` / `HasHideAnimation` | Whether the module waits for your animation hooks |
| `ViewType` / `MediatorType` | The pair to instantiate and register |

Register the config with the manager that is allowed to open it. A screen the manager
does not know about cannot be opened from that manager.

### The module context

A module that owns screens derives its context from `BaseScreenContext`, which
resolves `IScreenService` into the protected `_screenService` field and binds the
`ScreenManager` mediation for you:

```csharp
public class SettingsScreenSubContext : BaseScreenContext
{
    private SettingsSignals _signals;

    public override void SignalBindings()
    {
        base.SignalBindings();
        _signals = InjectionBinderCrossContext.Bind<SettingsSignals>();
    }

    public override void MediationBindings()
    {
        base.MediationBindings();
        MediationBinder.Bind<SettingsScreenView>().To<SettingsScreenMediator>();
    }

    public override void CommandBindings()
    {
        base.CommandBindings();
        CommandBinder.Bind(_signals.Incoming.Open).ToSequence<OpenSettingsScreenCommand>();
        CommandBinder.Bind(_signals.Incoming.Close).ToSequence<CloseSettingsScreenCommand>();
    }
}
```

`BaseScreenRoot<TContext>` is the matching Root when the screen module needs its own
GameObject rather than being a sub-context.

---

## Opening a Screen

`Open<T>()` returns a builder. Nothing happens until you call `Show()`.

```csharp
await _screenService.Open<SettingsScreenView>().Show();
```

| Builder call | Effect |
|---|---|
| `OpenInLayer(int layerIndex)` | Override the config's default layer |
| `ForceOpenAtFullLayer(bool withHideAnim = false)` | If the layer is occupied, hide the occupant instead of refusing |
| `ForceOpenAtDuplication(bool withHideAnim = false)` | If this screen is already open, hide the existing instance first |
| `SetParameters(params object[] parameters)` | Data for the screen, readable as `Data.Parameters` |
| `SkipShowAnimation()` / `SkipHideAnimation()` | Open or close instantly this one time |
| `AddToHistory()` | Record this screen in the navigation history |
| `Show()` | `Task<IScreenBody>` |
| `Show<T>()` | `Task<T>` — the typed instance |

### Opening from a command

Screens are opened from commands, so the flow shows up in the console and can be
triggered from anywhere — a mediator, a deep link, a test context.

```csharp
public class OpenSettingsScreenCommand : Command
{
    [Inject] private IScreenService _screenService { get; set; }

    public override async void Execute()
    {
        Retain();

        var screen = await _screenService
            .Open<SettingsScreenView>()
            .ForceOpenAtDuplication()
            .Show<SettingsScreenView>();

        if (screen == null)
        {
            FlowLogger.LogWarning(FlowLogType.SettingsModule, "Settings screen could not open.");
            Release();
            return;
        }

        screen.SetVersionLabel(Application.version);
        Release();
    }
}
```

Two details that matter in that example:

- **`async void Execute()` with `Retain()` / `Release()`.** The command must retain
  before the first `await`, or the chain moves on while the screen is still loading.
- **`Show<T>()` can return `null`.** A full layer without `ForceOpenAtFullLayer`, a
  config the manager does not know, or a failed load all end that way. Check it.

### Passing data in

```csharp
await _screenService
    .Open<RewardScreenView>()
    .SetParameters(rewardId, amount)
    .Show();
```

```csharp
public class RewardScreenView : ScreenView
{
    public override void BeforeScreenActivation()
    {
        base.BeforeScreenActivation();

        var rewardId = (string) Data.Parameters[0];
        var amount   = (int)    Data.Parameters[1];

        Render(rewardId, amount);
    }
}
```

`Data.Parameters` is an `object[]`, so this is a cast boundary. Keep it to one value
object rather than a list of loose primitives — `SetParameters(rewardVO)` beats
`SetParameters(id, amount, rarity, isFirstTime)`.

---

## Finding, Hiding, Unloading

The service exposes four sub-services. They read like sentences.

### Check — is it open?

```csharp
_screenService.Check.IsScreenActive<SettingsScreenView>();
_screenService.Check.IsLayerFull(layerIndex: 3);
```

### TryGet — give me the instance

```csharp
if (_screenService.TryGet.Screen<SettingsScreenView>(out var settings))
    settings.RefreshVolume(_audioModel.Volume);
```

`TryGet` returns the live instance without opening anything. This is how one command
updates a screen another command opened.

### Hide — take it off screen

```csharp
_screenService.Hide.Screen<SettingsScreenView>();
_screenService.Hide.ScreenInLayer(layerIndex: 3);
_screenService.Hide.ScreensByTag(ScreenTag.GroupA);
_screenService.Hide.ScreensAtManager(managerId: 1);
_screenService.Hide.AllScreens();
```

Every overload takes `isForce` — pass `true` to skip the hide animation.

Hidden screens go back to the pool. They are still loaded, so reopening is cheap.

### Unload — release the memory

```csharp
_screenService.Unload.Screen<SettingsScreenView>();
_screenService.Unload.ScreensByTag(ScreenTag.GroupA);
_screenService.Unload.ScreensByManager(managerId: 1);
_screenService.Unload.AllScreens();
```

Unloading destroys the instance and releases the asset. The next `Open` pays the load
cost again. Unload when leaving a whole area of the game; hide when the player might
come back in a moment.

---

## Preloading

Warming screens during a loading bar keeps the first open instant.

```csharp
_screenService.Load.ByTag(
    ScreenTag.GroupA,
    completeCallback: () => _signals.Outgoing.MenuScreensReady.Dispatch(),
    loadingProgressCallback: (done, total) => _view.SetProgress(done / (float) total));
```

```csharp
_screenService.Load.ScreensAtManager(managerId: 0, completeCallback: OnHudReady);
_screenService.Load.All(completeCallback: OnEverythingReady);
```

Tag the screens a given phase needs, and preload the tag rather than naming each
screen — the list then lives in the config assets, not in code.

---

## Managers, Layers and Tags

**Managers** separate UI surfaces that never interact. Each `ScreenManager` has its
own layers and its own config list; `managerId` selects one.

**Layers** are slots inside a manager. One screen occupies one layer at a time.
Opening into an occupied layer fails unless you pass `ForceOpenAtFullLayer()`, which
hides the occupant first. That failure is deliberate: it turns "two popups fighting
over the same slot" into a null return you can see, instead of an overlap you notice
in a screenshot three weeks later.

**Tags** (`Default`, `GroupA`…`GroupH`) group screens across layers and managers for
bulk operations — load a menu set, hide every gameplay overlay, unload a whole area.

---

## Screen Lifecycle Hooks

`ScreenView` gives you three override points:

```csharp
public class RewardScreenView : ScreenView
{
    // The instance exists and Data is filled, but it is not visible yet.
    // Read parameters and render here.
    public override void BeforeScreenActivation()
    {
        base.BeforeScreenActivation();
        Render((RewardVO) Data.Parameters[0]);
    }

    // The show animation has finished. Start idle effects, autoplay, timers.
    public override void AfterScreenActivation()
    {
        base.AfterScreenActivation();
        _sparkles.Play();
    }

    // The screen is off-screen and heading back to the pool. Reset it.
    public override void ScreenHidden()
    {
        base.ScreenHidden();
        _sparkles.Stop();
        _scrollRect.verticalNormalizedPosition = 1f;
    }
}
```

`ScreenHidden` matters more than it looks. Screens are pooled, so the instance the
player sees next time is the same object with the same scroll position, the same
tween state and the same leftover text — unless you reset it here.

---

## Animations

Set `HasShowAnimation` / `HasHideAnimation` in the config, then override the play
hooks:

```csharp
public class SettingsScreenView : ScreenView
{
    protected override void PlayShowAnimation()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.25f)
                 .OnComplete(() => ShowCompleted?.Invoke(this));
    }

    protected override void PlayHideAnimation()
    {
        transform.DOScale(Vector3.zero, 0.2f)
                 .OnComplete(() => HideCompleted?.Invoke(this));
    }
}
```

You must invoke `ShowCompleted` / `HideCompleted` yourself. Until you do, the screen
stays in `InShowAnimation` / `InHideAnimation` state — the awaited `Show()` never
returns and the layer is never released.

`SkipShowAnimation()` on the builder bypasses the hook for a single open, which is
what you want when restoring UI after a scene load.

---

## Scenarios

### Open from a command, not from a mediator

```csharp
// ✅ The mediator announces intent; a command decides and opens.
private void OnSettingsClicked() => _signals.Incoming.OpenSettings.Dispatch();
```

```csharp
// ❌ The mediator opens the screen directly. Nothing else can trigger settings —
//    not a deep link, not a tutorial step, not a test — and the console shows no
//    step for it.
private async void OnSettingsClicked()
    => await _screenService.Open<SettingsScreenView>().Show();
```

### Retain across the await

```csharp
// ✅ The chain waits for the screen to actually exist.
public override async void Execute()
{
    Retain();
    var screen = await _screenService.Open<RewardScreenView>().Show<RewardScreenView>();
    if (screen == null) { Release(); return; }
    screen.PlayIntro();
    Release();
}
```

```csharp
// ❌ Execute() returns at the await. The next command in the sequence runs while the
//    screen is still loading, and whatever it expects to be on screen is not there.
public override async void Execute()
{
    var screen = await _screenService.Open<RewardScreenView>().Show<RewardScreenView>();
    screen.PlayIntro();
}
```

### Reset pooled state in `ScreenHidden`

```csharp
// ✅ The next player to open this screen sees it the way you designed it.
public override void ScreenHidden()
{
    base.ScreenHidden();
    _scrollRect.verticalNormalizedPosition = 1f;
    _searchField.text = string.Empty;
    _selection.Clear();
}
```

```csharp
// ❌ Nothing is reset. The screen reopens scrolled halfway down with the last search
//    still in the box — a bug that only reproduces on the second open.
public override void ScreenHidden() => base.ScreenHidden();
```

### Hide for "back", unload for "leaving"

```csharp
// ✅ Closing a popup hides it — reopening is instant.
_screenService.Hide.Screen<InventoryScreenView>();

// ✅ Leaving the menu for a match unloads the whole menu tag.
_screenService.Unload.ScreensByTag(ScreenTag.GroupA);
```

```csharp
// ❌ Unloading a popup the player toggles constantly. Every open pays a full
//    Addressables load, and the frame hitches each time.
_screenService.Unload.Screen<InventoryScreenView>();
```

### One value object, not four loose parameters

```csharp
// ✅ Adding a field to RewardVO does not touch the call site or the cast.
.SetParameters(rewardVO)
```

```csharp
// ❌ Positional and untyped. Reordering two of them compiles fine and fails at
//    runtime, in the screen, far from the change.
.SetParameters(rewardId, amount, rarity, isFirstTime)
```

### Let a full layer fail, unless you mean it

```csharp
// ✅ Explicit: this screen is allowed to displace whatever is in its layer.
await _screenService.Open<AlertScreenView>()
    .ForceOpenAtFullLayer(withHideAnim: true)
    .Show();
```

```csharp
// ❌ ForceOpen on everything, so two screens silently displace each other and the
//    layer system stops telling you anything.
await _screenService.Open<ShopScreenView>().ForceOpenAtFullLayer().Show();
await _screenService.Open<ChatScreenView>().ForceOpenAtFullLayer().Show();
```

---

## Pitfalls

### `Show()` never returns

The screen has `HasShowAnimation` enabled and `PlayShowAnimation()` never invoked
`ShowCompleted`. The screen stays in the `InShowAnimation` state, the awaited task
never completes, and any command that retained around it hangs forever.

Check that every animation path — including the one where a tween is killed or the
object is disabled mid-animation — ends in `ShowCompleted?.Invoke(this)`.

### `Show<T>()` returns null

In order of likelihood:

1. The target layer is occupied and you did not pass `ForceOpenAtFullLayer()`.
2. The screen is already open and you did not pass `ForceOpenAtDuplication()`.
3. The `ScreenConfig` is not registered with the manager you are opening from.
4. The load failed — wrong `AddressableKey`, wrong `ResourcePath`, or a missing
   `DirectPrefab` reference.

The Flow Console's `Screen` channel names which of these happened.

### The screen opens but the mediator never runs

The view is missing its `ViewInjector` component, or the `MediationBinder.Bind<View>().To<Mediator>()`
line is in a context that never started. A screen view is still an ordinary FlowIoC
view — inheriting `ScreenView` does not register it.

### Second open looks wrong

Pooling. The instance is the same one the player closed. Reset per-open state in
`ScreenHidden()`, and read parameters in `BeforeScreenActivation()` rather than in
`Awake` or `Start`, which only run once per instance.

### Opening the same screen twice stacks two instances

You passed `ForceOpenAtDuplication()` when you meant to reuse. Without it, the second
open returns `null` and leaves the first instance alone — usually what you want. If
you want the existing one, use `TryGet.Screen<T>(out var screen)` and drive it
directly.

### A preload never completes

`Load.ByTag` only loads screens whose configs carry that tag *and* are registered with
a manager. A config created but never added to a `ScreenManager` is invisible to the
loader — and to `Open`.

---

## Related

- [README — FlowIoC at a Glance](../../../README.md#flowioc-at-a-glance)
- [Base Module](../../BaseModule/Documentation/BaseModule.md) — views, mediators,
  contexts
- [Commands](../../BaseModule/Controller/Documentation/Controller.md) — retaining
  across an `await`
- [Pooling](../../PoolModule/Documentation/PoolModule.md) — the general object pool
