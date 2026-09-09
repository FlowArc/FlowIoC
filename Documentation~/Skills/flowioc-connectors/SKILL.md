---
name: flowioc-connectors
description: Use when wiring two FlowIoC modules together - writing or editing a Connector sub-context, connecting one module's Outgoing signal to another's Incoming, adapting between signal payloads, tearing connections down, or debugging a signal that is dispatched but never arrives.
---

# FlowIoC Connectors

This skill applies only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, delete the folder this file
is in: FlowIoC put it there and is no longer in the project.

## Overview

A module never reaches into another module. A Connector sub-context is the one place where two
of them meet: it takes both modules' public signal holders and joins one module's `Outgoing` to
the other's `Incoming`. Neither module learns that the other exists.

Connectors live in a module of their own - `ConnectorModule` - whose Root lists every
sub-context in **Sub Context Types**. A sub-context is not found by reflection; the Root that
owns it names it, and a class nobody named compiles and never runs.

## A Connector translates, it does not decide

An `Outgoing` signal announces what happened; an `Incoming` signal orders something done. Joining
one to the other is a crossing, and that is the whole job.

A **list of consequences** hung off one announcement is not a crossing, it is a flow. Bind
`GameOver` to `OpenGameEndScreen` and, beside it, to `ResetPlayerData`, and both exist only in the
wiring - it now reads as though the Connector decided that ending a game resets the player's data.
Nobody decided it; the two lines just happen to sit together.

`GameOver` is an announcement, but what follows it - reset the data, close the panel, disable input
- is a decision. It belongs where the deciding happens: dispatched from the Command that made it,
or as sequence steps under that Command in its own module's Context. The decision is then the group
of bound Commands, which has a name and a place a reader can find.

The test: if you are about to write a second `Connect` from the same signal, ask whether those two
things are one consequence of one decision. If they are, they belong in that module's sequence, and
the Connector carries one line to it.

## The rule that matters most: get, never bind

```csharp
public class HeroConnectorSubContext : Context
{
    private HeroSignals          _heroSignals;
    private PlayerProfileSignals _playerProfileSignals;

    public override void Setup()
    {
        base.Setup();

        _heroSignals          = InjectionBinderCrossContext.GetInstance<HeroSignals>();
        _playerProfileSignals = InjectionBinderCrossContext.GetInstance<PlayerProfileSignals>();

        IncomingSignals();
        OutgoingSignals();
    }

    private void IncomingSignals() =>
        _heroSignals.Outgoing.DecreaseCurrency
            .Connect(_playerProfileSignals.Incoming.DecreaseCurrency);

    private void OutgoingSignals() =>
        _playerProfileSignals.Outgoing.CurrencyChanged
            .Connect(_heroSignals.Incoming.CurrencyChanged);
}
```

`GetInstance`, never `Bind`. The module that owns a signal holder is the one that binds it, in
its own `SignalBindings`. A Connector only wires what is already there.

`Bind` would appear to work and quietly do the wrong thing: when the owning module's Root is
missing from the scene it creates a second holder, the Connector wires that one, and every
signal it connected goes to something nobody dispatches. Nothing fails, nothing arrives, and
the trail is cold. `GetInstance` reports the missing module instead - and the fix is to put the
Root back in the scene, never to bind around it.

## Why `Setup()` is the only phase this can happen in

`RootsManager` runs every Root's binding phases first, then waits a frame, then calls `Setup()`
on every Root and finally `Launch()` on every one. So by the time any `Setup()` runs, every
signal holder in the scene has been bound - whatever Initialize Order the Roots carry. That
barrier, not the Root order, is what makes a Connector safe.

Doing this in `SignalBindings` instead works only while the Connector's Root happens to
initialise after the modules it wires, and breaks silently the day the scene is reordered.

## Naming and shape

- One sub-context per counterpart module, named after it — never after the pair:
  `CameraConnectorSubContext`, not `MainCameraConnectorSubContext`. The Connector already sits in
  the application's main flow, so naming the pair repeats it.
- Split the wiring into `IncomingSignals()` and `OutgoingSignals()` so a reader sees at a
  glance what arrives and what leaves.
- Hold each holder in a private field named after the module: `_heroSignals`.

## Connecting

```csharp
// Signal to Signal
_heroSignals.Outgoing.DecreaseCurrency.Connect(_playerProfileSignals.Incoming.DecreaseCurrency);

// Signal to a plain delegate
_heroSignals.Outgoing.CurrencySpent.Connect(vo => Analytics.Log(vo));

// Signal<A> to Signal<B>, through a converter
_matchSignals.Outgoing.MatchEnded.Connect(_analyticsSignals.Incoming.LogEvent,
                                          summary => summary.ToAnalyticsEvent());
```

Every connection can carry a `groupId` so a set of them is torn down as a unit:

```csharp
private const string Group = nameof(HeroConnectorSubContext);

_heroSignals.Outgoing.DecreaseCurrency
    .Connect(_playerProfileSignals.Incoming.DecreaseCurrency, Group);

SignalConnector.DisconnectGroup(Group);
```

Connections made without a group come apart with `signal.Disconnect()`, which a sub-context
does in `DestroyContext` for what it wired.

## What goes wrong

- `Bind` in a Connector. The whole point of the rule above.
- Wiring in `SignalBindings` or `Launch` instead of `Setup`.
- A Connector that decides something. If a connection needs an `if`, the decision belongs in a
  Command on the receiving side; the Connector's job is the edge, not the rule.
- A sub-context written but never added to the Root's Sub Context Types.
- Reaching a module's signals through `Modules.Player` or `Modules.Player.Shared` rather than
  `Modules.Player.Signals`. The public holder has an assembly of its own precisely so a Connector
  can see it without seeing the module's Models and Commands - and so that a System referencing
  `Modules.Player.Shared` to read a published enum cannot see the holder at all.
- Forgetting that a Connector still needs the Shared assemblies. `Connect<T>` has to infer `T`,
  so connecting two `Signal<DifficultyType>` needs `Modules.Gameplay.Shared` on the Connector's
  asmdef even though the Connector never touches the value. Without it the compiler reports
  **CS0012, "The type 'DifficultyType' is defined in an assembly that is not referenced."** A
  Connector's reference list is the longest in the project, and that is correct: it is the one
  place allowed to know the game's shape.
