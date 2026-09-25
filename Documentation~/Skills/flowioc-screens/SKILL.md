---
name: flowioc-screens
description: Use when adding or changing a UI screen in a FlowIoC Unity project - writing a screen module's context and ScreenCVO, opening a screen and filling it, updating one that is already open, closing it, guarding a screen's Mediator, writing a show or hide animation, or working out why a screen opens empty, opens twice, or has dead buttons.
---

# FlowIoC Screens

This skill applies only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, delete the folder this file
is in: FlowIoC put it there and is no longer in the project.

## Where a screen module lives

A screen module belongs to the module whose feature it shows, so it sits in the `zScreenModules`
of a main or a sub module - never under another screen module, and never under a test module.
`Create Module` offers exactly those parents.

A screen module publishes only signals, so it usually has no Shared assembly at all: its own
assembly, and `Scripts/Signals/` for the holder a Connector reads.

## The context declares the screen

There is no screen config asset. The context derives from `ScreenSubContext<TView, TMediator>`,
which binds the View to the Mediator, and declares everything else in a `ScreenCVO`:

```csharp
public class MainScreenContext : ScreenSubContext<MainScreenView, MainScreenMediator>
{
    private MainScreenSignals _signals;

    protected override ScreenCVO Screen => new()
    {
        ManagerId = 0,
        Layer = 0,
        Tag = ScreenTag.Default,
        Load = ScreenLoadCVO.Addressable("MainScreen"),
        HasShowAnimation = false,
        HasHideAnimation = false,
    };

    public override void SignalBindings()
    {
        base.SignalBindings();
        _signals = InjectionBinderCrossContext.Bind<MainScreenSignals>();
    }

    public override void CommandBindings()
    {
        base.CommandBindings();
        CommandBinder.Bind(_signals.Incoming.OpenMainScreen).ToSequence<OpenMainScreenCommand>();
    }
}
```

`Load` is required - a screen whose `Load` has no key is refused at registration.

## Where the context is listed

On the Root of the module the screen belongs to, with Auto Setup on. **Not** on `ScreenRoot`. It
registers itself in `Setup`, so a screen whose owning module's Root is not in the scene is never
registered, and `Open` reports that instead of opening it.

The Root that lists it may override everything but `Load` - `ManagerId`, `Layer`, `Tag` and the two
animation flags - by ticking *Override Screen* on the entry. Where a prefab lives is the module's
business and not the scene's, which is why `Load` is not offered.

Listing the same context on two Roots with two `ManagerId`s registers it twice, and the two
registrations are opened, pooled and unregistered independently.

## Opening a screen

The chain is always the same:

1. A request arrives from outside the module, crosses a **Connector**, and lands on the screen
   module's own **incoming** signal. Nothing reaches a screen module directly.
2. That signal runs `OpenXScreenCommand`.
3. The Command opens the screen and gets the view back.
4. **The Command fills it**, through the view's own methods, from data it reads wherever that data
   is published.

```csharp
internal class OpenShopScreenCommand : Command
{
    [Inject] private IScreenService   _screenService { get; set; }
    [Inject] private ISharedDataModel _sharedData    { get; set; }

    public override async void Execute()
    {
        Retain();

        try
        {
            // Read first: what the screen shows is published data - a Shared asset here, the
            // module's own Model when the data is the screen module's.
            CD_Shop shop = _sharedData.GetScriptable<CD_Shop>();
            PD_Player player = _sharedData.GetScriptable<PD_Player>();

            if (shop == null || player == null)
            {
                Stop();     // the shared data model has already reported the missing filing
                return;
            }

            ShopScreenView screen = await _screenService.Open<ShopScreenView>().Show<ShopScreenView>();

            if (screen == null)
            {
                FlowLogger.LogError("OpenShopScreenCommand - the screen did not open.");
                Stop();
                return;
            }

            // Filled through the view's own methods, on the instance just awaited.
            screen.ShowItems(shop.Items);
            screen.ShowCoins(player.Coins);
            Release();
        }
        catch (Exception exception)
        {
            FlowLogger.LogError($"OpenShopScreenCommand threw while opening the screen: {exception}");
            Stop();
        }
    }
}
```

**The worked example that ships is BotBar's `OpenBotBarScreenCommand`**
(`BotBarModule/zScreenModules/BotBarScreenModule/Scripts/Runtime/Controllers/`): it reads `CD_BotBar`
and `RD_BotBar` through `ISharedDataModel`, stops when either is not filed, refills a bar that is
already open instead of opening a second, and fills the one it opened. The screen module references
the Shared assembly the assets live in and nothing else of the module that publishes them.

`Open<T>()` returns a builder and nothing happens until `Show()`. `SetParameters`, `OpenInLayer`,
`SkipShowAnimation` and the rest are steps on it.

**All three ways out resolve the retain**: the screen opened, the screen came back null, and the
await threw. A retain nobody resolves hangs the group for ever - no timeout, nothing logged - and
with `async void` a throw leaves `Execute` at the `await` line and surfaces through Unity's
unhandled-exception handler with nothing in it to name the command. What the `catch` does is this
game's decision: `Stop()`, a `Release()` that carries on, or a signal that opens something else.

Use `Show<T>()` rather than `Show()`. A typed view compares against `null` through Unity's own
operator; the `IScreenBody` that `Show()` returns is an interface and does not.

The Command reaches the view directly here, and that is the only place it does: it is holding the
instance it awaited, so the screen is filled before anybody can look at it. A signal dispatched
instead would arrive after the screen is already on screen, which is a frame of an empty screen.

Inside that same Command nothing needs a signal - it has the instance for as long as it runs.

## Changing a screen that is already open

**Do not fetch the open screen and change it.** Whoever changed the value dispatches a signal, and
the screen's Mediator applies it. The view is touched directly once, by the Command that opened it,
and never again.

The exception is about how the values behave, not how many there are:

- A value that changes **on its own** gets a signal of its own. That is most of a screen.
- Values that only ever change **together** are one event, not eight. Either one signal carrying
  them, or a Command that fetches the screen and sets them - and there fetching is the honest
  option, because inventing eight signals that only ever fire together is worse than the fetch.

Every per-value change costs a signal in the holder, a binding and a Mediator handler, so the
question to ask about a value is whether it ever changes independently.

## Closing a screen

Closing that decides nothing is the Mediator's:

```csharp
private void GoBack()
{
    if (_view.Data.State != ScreenState.AvailableToSendSignal) return;

    _signals.Outgoing.GoBack.Dispatch();
    _view.Hide();
}
```

Closing that **is** a decision - the screen may only be left once something is saved, or spends a
currency, or has to ask - is not. That one dispatches, a Command reads the conditions, and the
screen closes as a consequence. The Mediator holds no game rules.

## Which screen opens next

**Which screen opens next is a decision taken when the one before it ends.** The screen says how it
ended on its Outgoing - `Closed`, `Purchased` - and the game's System decides in a Command: closed
while the run is ending opens the next offer, a purchase opens nothing. Screens are never queued up
in advance, because the order depends on how each one ends.

A layer holds one screen. To open a new one on the same layer at once, `ForceOpenAtFullLayer()`
closes the screen there without its hide animation and opens the new one;
`ForceOpenAtFullLayer(true)` lets the old one play its hide animation while the new one opens over
it - it does not wait for it. Screens shown at the same time each take a layer of their own.

## The Mediator's two guards

**It subscribes on `ShowCompleted` and unsubscribes on `HideCompleted`.** `OnRegister` wires those
two events and nothing else, for the same reason the View wires its buttons in `OnEnable`: a screen
is pooled, so `OnRegister` runs once while the screen opens many times.

```csharp
public void OnRegister()
{
    _view.ShowCompleted += OnScreenShown;
    _view.HideCompleted += OnScreenHidden;
}

private void OnScreenShown(IScreenBody screen) => _view.Play += PlayClicked;
private void OnScreenHidden(IScreenBody screen) => _view.Play -= PlayClicked;
```

By the time a Mediator hears `HideCompleted` the screen is back in the pool and its layer is free,
so what it dispatches from there may open the next screen on the same layer.

**And every handler is guarded by the screen's state.**
`ScreenState.AvailableToSendSignal` is `InUse` and nothing else, so a tap landing while the screen
animates in or out does not become a signal. It is not queued and not replayed; it does not happen.

## Animations

`ScreenBody` gates them. `HasShowAnimation` false and `ShowCompleted` fires at once; true and the
screen takes `ScreenState.InShowAnimation`, `PlayShowAnimation()` is called, and **the state stays
until the View invokes `ShowCompleted`**. That is what makes the handler guard mean anything, so the
View must report when the animation *finished* rather than when it started.

```csharp
// no animation
protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

// a timeline
protected override void PlayShowAnimation()
{
    _director.time = 0;
    _director.Play();
    StartCoroutine(WaitForTimelineFinish());   // invokes ShowCompleted at the end
}

// tweens - only the last one reports
protected override void PlayShowAnimation()
{
    BackBtn.transform.DOScale(1, .5f).SetDelay(.1f);
    SaveBtn.transform.DOScale(1, .5f).SetDelay(.3f).OnComplete(() => ShowCompleted?.Invoke(this));
}
```

A screen may have a show animation and no hide animation; nothing depends on the pair.

## Coming back from the pool

The same instance comes back, carrying whatever the last opening left on it. Reset in
**`BeforeScreenActivation`**, which runs immediately before `Show()`:

```csharp
public override void BeforeScreenActivation()
{
    base.BeforeScreenActivation();   // sets the GameObject active
    _scrollRect.verticalNormalizedPosition = 1f;
}
```

`AfterScreenActivation` runs on the other side of the RectTransform work, for anything that has to
wait for the layout. The hide animation is for the look of it and is not where resetting belongs.

## The View

A `ScreenView` wires its buttons in `OnEnable` and drops them in `OnDisable`, never in `Awake` -
`Awake` runs once and the screen opens many times.

It holds no logic. Its actions are plain `Action` fields and its handlers are one line:

```csharp
public Action Play;
public void PlayClicked() => Play?.Invoke();
```

Raw input is translated here rather than in the Mediator - a left swipe calls the same
`NextMapClicked()` the button does, and the Mediator never learns which it was. The View's other
methods are named for what they show, `ShowPlayButton` and `WaitingCountdownTick`: those are what
the opening Command calls to fill the screen, and what the Mediator calls when a signal says a value
changed.

## Signals

**Outgoing** for what leaves the screen module, the **internal** holder for what stays. Paging
between the maps on a match board is the screen's own business and never crosses a Connector;
playing one of them leaves.

## A screen that must be up before Addressables is

`ScreenLoadCVO.Addressable` waits on Addressables' own initialisation - seconds on a remote
catalogue over a bad connection - and a screen that shows the loads cannot wait on that with
nothing on stage. That screen declares `ScreenLoadCVO.Resource("LoadingScreen")` and its prefab
sits in the module's `Resources/` folder: on stage on frame 2, whatever Addressables is doing. The
installer and the tooling know a Resources prefab is not addressable and leave it out of the groups.

What a game wants to change between releases - the splash behind the bar - stays addressable. The
prefab bundles the same art as its fallback, in the module's `Art/` folder, where the installer
registers it under its own name in the screen's group; the screen context's `Launch` dispatches an
internal signal, and a Command loads the addressable copy through `IAssetService` and announces it:

```csharp
public override void Launch()
{
    base.Launch();
    _internalSignals.LoadBackground.Dispatch();
}
```

The Command remembers the sprite in the Model and dispatches `BackgroundLoaded`; the Mediator
applies it to the View, and the opening Command applies it from the Model in the other order - the
art may land before the instance exists, or the instance may be pooled before the art lands. The
View does not reset it in `BeforeScreenActivation`: the art is the same for every opening. A load
that brings nothing leaves the bundled art on stage; a game replaces the asset and keeps the address.

## Testing one

A screen's test Root lists the **production** screen context as a sub-context rather than declaring
the screen again. The test context only opens it in `Launch`.

## What goes wrong

- **The screen opens and every button is dead, with nothing logged.** An overridden
  `PlayShowAnimation` or `PlayHideAnimation` that does not invoke `ShowCompleted` / `HideCompleted`.
  The Mediator never subscribes.
- **The animation is still running and taps already work.** `OnComplete` hung on the wrong tween, so
  the screen left `InShowAnimation` early.
- **`Open` reports the screen is not registered.** Its module's Root is not in the scene. Put the
  Root back; do not register the screen somewhere else.
- **The screen opens empty.** The opening Command awaited `Show()` and then dispatched a signal
  instead of calling the view - or filled it before the await.
- **A value sent right after the screen opened never shows.** `Show<T>()` returns when the show
  *starts*, and with a show animation the Mediator subscribes only at `ShowCompleted` - so a signal
  dispatched in the same sequence, one step after the opening Command, lands on nobody. Fill the
  screen in the opening Command from the published data, or send the value once `ShowCompleted` has
  come.
- **`Layer N is not empty`, and the open comes back null.** A layer holds one screen, and the one
  there - or one still playing its hide - is in the way. Open the next from what the Mediator
  dispatches on `HideCompleted`, when the layer is free, or with `ForceOpenAtFullLayer()` - see
  *Which screen opens next*.
- **The screen opens twice, or in the wrong manager.** The same context is listed on two Roots. That
  is legal and gives two independent registrations, so it is only a bug if it was not meant.
- **A second opening skips its animation.** The screen came back from the pool in the state the last
  one left it in, and nothing reset it in `BeforeScreenActivation`.

## Gone, and not to be written

`ScreenConfig`, `CD_Screen`, and `BaseScreenContext` used as a sub-context. A screen declares itself
in its own context, and there is no config asset.
