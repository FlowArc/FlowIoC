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
public class OpenMatchboardScreenCommand : Command
{
    [Inject] private IScreenService _screenService { get; set; }
    [Inject] private ISharedDataModel _sharedData { get; set; }

    public override async void Execute()
    {
        var screen = await _screenService.Open<MatchboardScreenView>().Show<MatchboardScreenView>();

        screen.ShowPlayButton(true);
        screen.ShowStateLayouts(count, currentIndex);
    }
}
```

`Open<T>()` returns a builder and nothing happens until `Show()`. `SetParameters`, `OpenInLayer`,
`SkipShowAnimation` and the rest are steps on it.

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
- **The screen opens twice, or in the wrong manager.** The same context is listed on two Roots. That
  is legal and gives two independent registrations, so it is only a bug if it was not meant.
- **A second opening skips its animation.** The screen came back from the pool in the state the last
  one left it in, and nothing reset it in `BeforeScreenActivation`.

## Gone, and not to be written

`ScreenConfig`, `CD_Screen`, and `BaseScreenContext` used as a sub-context. A screen declares itself
in its own context, and there is no config asset.
