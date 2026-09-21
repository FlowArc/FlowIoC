---
name: flowioc-root-order
description: Use when placing a Root in a FlowIoC scene or choosing its Initialize Order - adding a new module Root, a Service Root, a Connector or a screen Root, deciding which context binds before which, making a Root persistent across a scene load and knowing what its context takes back when it is destroyed, or debugging a null signal holder, a missing binding or a Connector that wired nothing.
---

# Ordering Roots in FlowIoC

This skill applies only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, delete the folder this file
is in: FlowIoC put it there and is no longer in the project.

## Overview

A module joins the game by having its Root in the scene, and every Root carries one number:
**Initialize Order**, edited at the top of the Root's inspector. `RootsManager` sorts every
registered Root by it and drives them in that order, so the number is the only lever there is
over which module is built first.

The numbers are not free-form. They form bands, and picking a number means picking the band a
Root belongs to.

## The bands

The whole range is `-100` to `100`. Nothing needs to sit outside it: `-100` is as early as a Root
can be and `100` is as late.

| Order | Who sits there | Why |
|---|---|---|
| -100, -90 | The modules that put data in place before anything reads it | `PostConstruct` runs during the binding pass, not after `Setup`, and each Root finishes its own before the next begins - so being first is what puts data in place before anything reads it. Saved data is restored at `-100` (`LocalSaveServiceRoot`); config is rewritten from an A/B assignment at `-90` (`AbTestFlowServiceRoot`). A service whose `PostConstruct` reads config sits after `-90`, or it reads the original before the variant lands and nothing is logged. |
| -80 … -10 | The other Services, on the tens, in the order the boot reads | A Service depends on nothing else, so it comes up early and is ready for everyone. `-80` the asset service, the door every Addressables load goes through; `-70` the screen service and `-60` the pool service, which load through it; then the helpers - `-50` haptics, `-30` world pointers, `-10` the loading service, last because it opens a screen. |
| 0 - 97 | The game's own modules and systems | Gameplay, camera, the modules this game is made of. |
| 98 | `ConnectorRoot` | After every module it wires, so the scene reads as modules first and wiring after them. A Connector binds no signal holder anyway - it gets the ones every other context bound. |
| 99 | `ScreenRoot` | The screen manager owns the screen prefabs, so it is up before the flow that opens the first screen. |
| 100 | `MainRoot` | The application's entry point. Its `Launch()` dispatches the first signal, last of all. |

What the shipped Roots actually use: `LocalSaveServiceRoot` `-100`, `AbTestFlowServiceRoot` `-90`,
`AssetServiceRoot` `-80`, `ScreenServiceRoot` `-70`, `PoolServiceRoot` `-60`, `HapticServiceRoot`
`-50`, `WorldPointerServiceRoot` `-30`, `LoadingServiceRoot` `-10`, `GameplaySystemRoot` `0`,
`CameraSystemRoot` `1`, `ConnectorRoot` `98`, `ScreenRoot` `99`, `MainRoot` `100`.

A game's own service takes a free ten, or a unit below the ten it leans on - `-69` for one that
wants the screen service bound first. Inside the `0 - 97` band the exact number rarely matters.
Two modules that never touch each other can both sit at `0`; a System that another Root wants
bound before it goes a step lower.

## The scene reads top to bottom

`MainScene` is authored in the same order, with separator objects between the bands, so the
Hierarchy shows the boot order without opening a single inspector:

```
MainScene
├── AssetServiceRoot           -80
├── ScreenServiceRoot          -70
├── PoolServiceRoot            -60
├── LoadingServiceRoot         -10
├── ------------------------
├── GameplaySystemRoot           0
├── ------------------------
├── ConnectorRoot               98
├── ScreenRoot                  99
└── MainRoot                   100
```

Keep a new Root in its band's place in that list. A Hierarchy that disagrees with the numbers is
a trap for the next reader.

## What the order actually buys

`RootsManager.StartContexts` does three passes, and only the first is per-Root:

1. Sorted by Initialize Order, each Root runs its binding phases - `Context.Start()`,
   `SignalBindings`, `InjectionBindings`, `MediationBindings`, `CommandBindings`, then
   `InjectAllInstances`.
2. **One frame passes.**
3. `Setup()` on every Root, in the same order. Then `Launch()` on every Root, in the same order.

A sub-context listed on a Root - a screen context, a Connector's - goes through every pass with
the Root that lists it, after the Root's own context: it binds, it is set up, and it is launched,
so a screen context's `Launch` is where it dispatches what it loads for itself.

So the order decides who *binds* first, and who is called first within the `Setup()` and
`Launch()` passes. It does not decide whether cross-module access is safe: the frame barrier
already guarantees that every signal holder in the scene exists before any `Setup()` runs. That
is why a Connector does its work in `Setup()` and why `Launch()` is where the first signal is
dispatched.

`ConnectorRoot`'s `98` is therefore about reading order, not about safety. It sits after every
module it wires and before the screen host and the entry point; any other number in the band
would work just as well.

## What belongs in each phase

- **The binding phases declare.** `SignalBindings`, `InjectionBindings`, `MediationBindings` and
  `CommandBindings` say what the module is made of and nothing else. A Context that needs an `if`
  is making a decision, and a decision belongs in a Command.
- **`Setup()` initialises.** Every binding in the scene is done by the time it runs, so this is
  where a module gets its Models ready if they need readying, and where a Connector wires one
  module's `Outgoing` to another's `Incoming`. It is the only phase that may reach across modules.
- **`Launch()` starts the game.** It runs after every `Setup()`, and it dispatches the module's
  first signal - the entry point's `Launch` being the one that starts the flow.

## A Root that outlives its scene, and what a context takes back

A module whose work has to survive a scene load makes its Root persistent in
`BeforeCreateContext`, which runs just before the context is built:

```csharp
protected override void BeforeCreateContext()
{
    transform.SetParent(null);
    DontDestroyOnLoad(gameObject);
}
```

The `SetParent` is not decoration. Unity marks only root-level objects as do-not-destroy, so a
Root authored under something else has to detach itself first.

**A context takes back what it bound across.** `DestroyContext` empties the context's own binder
and removes from `InjectionBinderCrossContext` everything this context put there - its signal
holder, its Service interface - so a module's public surface lives exactly as long as its Root. A
scene's module goes with the scene and is bound fresh when the scene comes back; a persistent
Root's Service lives for the whole run, because that Root is never torn down. What was handed in
with `BindInstance` - the two providers - belongs to the run and stays.

Two things follow, and both are easy to get wrong:

- **A persistent module never keeps hold of a scene module's holder or Service.** A Service knows
  nobody anyway, and the one place that reaches across is a Connector, which is rebuilt with its
  scene: it gets holders in `Setup` and disconnects them in `DestroyContext`.
- **A module that has to start clean when its scene comes back has nothing to reset.** Its Models
  and its signal holder are new instances. Writing a reset pass for them is work the teardown
  already did.

## Choosing a number for a new Root

- Is it a Service - self-contained, not specific to this game? Negative: a free ten, or a unit
  below the ten it leans on. After `-90` if its `PostConstruct` reads config, and below anything
  that injects it at bind time.
- Is it a module or System this game is made of? Somewhere in `0 - 97`. Use `0` unless another
  Root genuinely has to bind first.
- Is it a Connector, a screen host or an entry point? Those three seats are taken: `98`, `99`,
  `100`. A second Connector for a large project sits beside the first, still below `ScreenRoot`.
- Then place the GameObject in the Hierarchy where its number says it belongs.

## What goes wrong

- A Root left at `0` that other Roots inject from. It binds in registration order relative to its
  peers, so the failure is intermittent - fine on one machine, null on another.
- A Connector mixed in among the modules it wires. It still works, because the barrier saves it,
  but the scene stops reading as modules first and wiring after them.
- Doing cross-module work in `Launch()` that belonged in `Setup()`, and then fixing it by nudging
  Initialize Order. The phase is the fix; the number is not.
- A Service given a positive number. It cannot need one - if it does, it is a System, and it
  belongs in the `0 - 97` band with a name to match.
- A service whose `PostConstruct` reads config seated above `-90`. The pool service at `-98` was
  the case: it read `CD_PoolGroup` before the A/B module at `-90` had written the variant over it,
  and nothing was logged, because nothing went wrong.
