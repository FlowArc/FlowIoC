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
        OutGoingSignals();
    }

    private void IncomingSignals() =>
        _heroSignals.Outgoing.DecreaseCurrency
            .Connect(_playerProfileSignals.Incoming.DecreaseCurrency);

    private void OutGoingSignals() =>
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

- One sub-context per counterpart module, named after it: `HeroConnectorSubContext`,
  `MainConnectorSubContext`.
- Split the wiring into `IncomingSignals()` and `OutGoingSignals()` so a reader sees at a
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
- Reaching a module's signals through `Modules.Player` rather than `Modules.Player.Shared`. The
  public holder lives in the Shared assembly precisely so a Connector can see it without seeing
  the module's Models and Commands.
