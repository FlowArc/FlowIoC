---
name: flowioc-systems-services
description: Use when writing or shaping the logic side of a FlowIoC module - deciding whether something is a System or a Service, adding a Service another module will call, giving a System a surface, splitting either into sub services or sub systems, choosing between a signal and a callback for what a Service announces, or working out whether a piece of data belongs to a Model, a sub system or a module of its own.
---

# Systems and Services in FlowIoC

This skill applies only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, delete the folder this file
is in: FlowIoC put it there and is no longer in the project.

## Which of the three a module is

Every module is one of three kinds, and you can tell which by looking:

- A **Service** has files under `Scripts/Runtime/Services/`.
- A **Screen module** has a context deriving from `ScreenSubContext<TView, TMediator>`.
- Everything else is a **System**.

There is no fourth question to ask. A module written for the game at hand is a System, which is
why *Role* in Create Module starts there.

## What separates a Service from a System

| | Service | System |
|---|---|---|
| Depends on | nothing at all | other Systems and Services |
| Specific to this game | no, it travels | yes |
| Reached by another module | directly, through its interface | only through signals |
| Lives in | `Scripts/Runtime/Services/` | `Scripts/Runtime/Systems/` |

A Service answers the input it is given: a countdown, a parser, a storage wrapper. It does not
know another Service, and it does not know a System or a Screen either. **A finished Service's
assembly gets no later additions** - work that arrives afterwards goes somewhere else, or the
Service slowly turns into a module of the game it happens to sit in. The one exception is what
FlowIoC embeds in itself; the package's own services are allowed to lean on each other.

A System is the opposite by design. It is written for this game and may lean on other Systems and
Services - waiting on a signal they raise, or working from data they publish. What it may not do
is reference another module's assembly. Only a Service crosses that way.

## A Service is the one direct crossing

The module that uses a Service references that Service's assembly and injects its interface:

```csharp
public class OpenMatchBoardScreenCommand : Command
{
    [Inject] private ICounterService _counterService { get; set; }
}
```

Being usable this way is the whole point of a Service, and it is what the Context declares by
binding it across contexts:

```csharp
public override void InjectionBindings()
{
    base.InjectionBindings();

    InjectionBinder.Bind<ITimeSource, DeviceTimeSource>();

    // The one type other modules reference directly, which is what makes this a Service.
    InjectionBinderCrossContext.Bind<ICounterService, CounterService>();
}
```

`InjectionBinder` keeps a binding inside the module; `InjectionBinderCrossContext` offers it to
everyone. A Service that binds its interface the first way is a Service nobody can use.

A Service that more than one module needs gets a module of its own. Its folder is named for what
it does - `CounterModule`, not `CounterServiceModule` - while its Root and Context keep the
suffix, `CounterServiceRoot` and `CounterServiceContext`, because that is what the inspector reads
to paint them.

## The two ways into a Service

1. **Call the interface.** The ordinary case. A System or a Screen injects it and calls it.
2. **Dispatch one of its Commands.** A Service may ship ready-made Commands for the case where the
   caller needs the work to be *a step in a sequence, with the next step waiting on it*:

   ```csharp
   CommandBinder.Bind(_signals.Incoming.CelebrateWin)
       .ToSequence<HapticTriggerCommand>()
       .ToSequence<PlayWinAnimationCommand>();
   ```

   A generic `ToSequence<DispatchSignalCommand>` would start the same work, but the sequence
   carries on without waiting for it. A Command of the Service's own is what holds the line.

Exceptions aside, those two are the whole list.

## The two ways out of a Service

1. **A signal**, when what happened may concern the whole game.
2. **A callback the caller handed in**, when the answer is only for whoever asked.

`CounterService` is the second case throughout - `counterTick`, `counterComplete`, `counterStop`,
`elapsedTimeTick`, `checkActive` are all `Action`s the caller passes to `CountDownFrom`. Whoever
started the counter is who wants the tick, so there is nothing to announce.

A Service therefore does not automatically get a signal holder. It gets one when something outside
it has to be told and no subscription already carries that.

## A System needs no `System.cs`

The work a System does is followed through the command flow its Context declares. That is where a
reader looks, and a module can be a complete System without a single type named `...System`.

A `System.cs` appears when the module wants a **surface**:

- To collapse many injections into one. A Command injects `IMapSystem` once and writes
  `_map.Grid.Build(...)` instead of injecting eight Models separately.
- To make what is available discoverable. Press `.` and a short tidy list appears.

**A System type holds injected members and no method that does work.** The moment a method on it
does something, that work belongs in a Command - that is what Commands are for, and it is what
keeps the flow visible in the Flow Console. The one allowance is `PostConstruct`, which may
assemble the facade.

```csharp
internal class MapSystem : IMapSystem, IConstructable
{
    [Inject] public MapLevelSubSystem Level      { get; set; }
    [Inject] public MapGridModel      Grid       { get; set; }
    [Inject] public IThemeModel       Theme      { get; set; }

    public void PostConstruct() => LoadCanvasSettings();
}
```

**A System builds its structure out of sub systems.** Models appear among its members where the
module's own state lives, but the sub system is the unit a System is assembled from.

When a module has a System, a Command reaches that module's own Models **through it** rather than
injecting a Model directly. A module without a System is the ordinary case and its Commands inject
Models as they always have - the rule is conditional on the System existing, not a reason to add
one.

## Model, sub system, or a module of its own

**A Model owns the module's state and its data. A sub system computes, and may read what other
modules publish.**

- `MapGridModel` owns a `Dictionary<Vector2Int, GridVO>` it mutates and dispatches its module's
  internal signal. It touches nothing outside the module. That is a Model.
- `MapLevelSubSystem` owns no module state. It reads config assets and another module's published
  profile, derives two indices from them, and answers `GetMenuLevel()` and `GetGameLevel()`. That
  is a sub system.

A sub system may hold its own data. Which home the data takes is decided by how big it is and how
far its scope reaches:

| Home | When |
|---|---|
| The sub system itself | Its own working data - the config it reads, the values it derives |
| A Model | The data is the **module's** state rather than one sub system's |
| A module of its own | The data is large, or its scope reaches past this module |

## Splitting into sub services and sub systems

Both split the same way, into `Services/Sub/` and `Systems/Sub/`, and for **two** reasons:

1. **It grew.** One file stopped being readable.
2. **A chained surface is wanted.** The split is what makes the call site discoverable, and this
   is the reason that comes up more often.

`IScreenService` is the worked example, and it shows both chaining shapes:

```csharp
public interface IScreenService
{
    LoadSubService  Load  { get; set; }   // grouped by noun: five nouns, not thirty verbs
    CheckSubService Check { get; set; }
    HideSubService  Hide  { get; set; }

    IScreenBuilderSubService Open<T>(int managerId = 0) where T : IScreenBody;
}

// a builder: every step returns the builder, and nothing happens until Show()
_screenService.Open<SettingsScreenView>().OpenInLayer(1).SetParameters(id).Show();
```

A sub service or sub system is an implementation detail. It is reached through the Service or the
System that owns it, never bound across contexts on its own.

## Naming

| Thing | Name |
|---|---|
| Service | `ICounterService` and `CounterService` |
| System | `IMapSystem` and `MapSystem` |
| Sub service | `AssetLoadSubService`, `ScreenBuilderSubService` |
| Sub system | `MapLevelSubSystem` |
| Service module | `CounterModule`, holding `CounterServiceRoot` and `CounterServiceContext` |
| System module | `MapModule`, holding `MapSystemRoot` and `MapSystemContext` |

`MapSystemRoot`, `MapSystemContext` and `IMapSystem` in one module is not a collision. The Root
roots the module, the Context declares its bindings, the interface offers the surface - exactly as
`CounterServiceRoot` and `ICounterService` already sit side by side.

## Folders

`Services/` and `Systems/` are optional folders on a main module. Create Module ticks them from
*Role*: picking **System** arrives with `Systems/` ticked, picking **Service** with `Services/`
ticked, and either tick can be changed before the module is written.

## What goes wrong

- **A Service that grew a dependency.** The moment it injects a System, or reads another module's
  data, it stopped being a Service. Either the dependency moves out, or the module is a System and
  should be named like one.
- **A `System.cs` with methods that do work.** Those methods are Commands that never got written.
  The Flow Console shows nothing, and the sequence they belonged in cannot wait for them.
- **A Service binding its interface with `InjectionBinder`.** It compiles, the module works, and
  no other module can inject it - which is the one thing a Service is for.
- **A sub service bound across contexts.** It is the Service's own machinery; a caller that
  injects it has reached past the interface that was meant to be the whole surface.
- **A System reaching another module's assembly** because it needed one enum. Data crosses through
  the other module's Shared assembly; signals cross through a Connector. Neither is a reason to
  reference `Modules.Other`.
