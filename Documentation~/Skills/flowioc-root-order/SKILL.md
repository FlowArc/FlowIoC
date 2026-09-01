---
name: flowioc-root-order
description: Use when placing a Root in a FlowIoC scene or choosing its Initialize Order - adding a new module Root, a Service Root, a Connector or a screen Root, deciding which context binds before which, or debugging a null signal holder, a missing binding or a Connector that wired nothing.
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

| Order | Who sits there | Why |
|---|---|---|
| negative | Services | A Service depends on nothing else, so it can come up first and be ready for everyone. |
| 0 - 97 | The game's own modules and systems | Gameplay, input, camera, the modules this game is made of. |
| 98 | `ConnectorRoot` | After every module it wires, so the scene reads as modules first and wiring after them. A Connector binds no signal holder anyway - it gets the ones every other context bound. |
| 99 | `ScreenRoot` | The screen manager owns the screen prefabs, so it is up before the flow that opens the first screen. |
| 100 | `MainRoot` | The application's entry point. Its `Launch()` dispatches the first signal, last of all. |

What the shipped Roots actually use: asset service `-10000`, screen service `-99`, pool service
`-2`, gameplay `0`, input `0`, camera system `1`, `ScreenRoot` `99`, `MainRoot` `100`,
`ConnectorRoot` `98`.

Inside a band the exact number rarely matters. Two modules that never touch each other can both
sit at `0`; a System that another Root wants bound before it goes a step lower.

## The scene reads top to bottom

`MainScene` is authored in the same order, with separator objects between the bands, so the
Hierarchy shows the boot order without opening a single inspector:

```
MainScene
├── Directional Light
├── EventSystem
├── ScreenServiceRoot          -99
├── PoolServiceRoot            -2
├── ------------------------
├── GameplayRoot                 0
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

## Choosing a number for a new Root

- Is it a Service - self-contained, not specific to this game? Negative. Go below anything that
  injects it.
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
