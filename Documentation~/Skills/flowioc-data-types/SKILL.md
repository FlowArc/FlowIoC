---
name: flowioc-data-types
description: Use when adding or naming data in a FlowIoC Unity project - a ScriptableObject data asset, a serializable value object, saved player state, downloaded backend data, editor-only settings - or when unsure whether a class should be CD_/RD_/PD_/ED_/DD_ or carry a VO/CVO/RVO/PVO/EVO/DVO suffix.
---

# FlowIoC Data Types

This skill applies only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, delete the folder this file
is in: FlowIoC put it there and is no longer in the project.

## Overview

In a FlowIoC project the name of a data type says where its contents come from, so a
reader knows what is safe to regenerate and what has to survive a restart without opening
the file. A ScriptableObject asset takes a prefix; the value objects it carries take the
matching suffix.

## Quick Reference

| Prefix | Kind | Filled by | Value objects inside |
|---|---|---|---|
| `CD_` | Config data - constant in every session, on every device | An author, in the Editor | `MapCVO` |
| `RD_` | Runtime data - produced during play, gone when it stops | Play | `MapRVO` |
| `PD_` | Player data - this player's state, loaded at startup and saved back on every change | Play, through the save system | `MapPVO` |
| `ED_` | Editor data - settings and caches only editor tooling reads | Editor tools | `MapEVO` |
| `DD_` | Database data - a copy of something a backend owns | A download | `MapDVO` |

Plain `VO` is for data that belongs to no one asset: a payload passed between commands,
the shape a Function returns.

## Where the files go

```
Scripts/Runtime/Datas/
├── UnityObjects/    # CD_Maps.cs, PD_Maps.cs, RD_MapPool.cs
└── ValueObjects/    # MapVO.cs, MapCVO.cs, MapPVO.cs
```

Data another module reads goes in `Scripts/Shared/Datas/` instead, under the same two
folders and the same naming. Shared is an assembly of its own - `Modules.Player.Shared` -
so a screen or sub module can reference the data without reaching the module's Models and
Commands. The naming does not change with the folder; only who can see it does.

## Example

```csharp
// Datas/UnityObjects/CD_Maps.cs
[CreateAssetMenu(fileName = "CD_Maps", menuName = "Game/Data/CD_Maps")]
internal class CD_Maps : ScriptableObject
{
    public List<MapCVO> Maps = new();
}

// Datas/ValueObjects/MapCVO.cs
[Serializable]
public class MapCVO
{
    public string Id;
    public int    StarTarget;
}
```

A value object that carries two kinds at once is named after **neither**:

```csharp
[Serializable]
public class GameHexVO
{
    public GameHexCVO Config;   // what the level author placed
    public GameHexRVO Runtime;  // what play produced
}
```

Calling that `GameHexCVO` would be a lie about half its contents, so it is named for the
hex and the halves keep their own suffixes.

## Choosing

Ask where the value comes from, not what it is about:

- Typed in by a designer and never written at runtime → `CD_` / `CVO`
- Computed while playing and thrown away at the end → `RD_` / `RVO`
- Has to still be there next launch → `PD_` / `PVO`
- Only an Editor tool ever reads it → `ED_` / `EVO`
- Downloaded from a backend that owns the truth → `DD_` / `DVO`

A project may add a family of its own - a new prefix and its matching suffix - by
declaring both in `<Solution>.sln.DotSettings`.

## Common Mistakes

| Mistake | Why it is wrong |
|---|---|
| `MapData`, `MapConfig`, `MapSO` | The suffix family is the convention; a bare descriptive name says nothing about lifetime. |
| Writing to a `CD_` asset at runtime | Config is constant. If it changes during play it is `RD_`, and if it must survive a restart it is `PD_`. |
| A `CVO` list inside a `PD_` asset | The suffix has to match the asset it lives in, or the name stops predicting the lifetime. |
| Naming a mixed holder `GameHexCVO` | Name a two-kind holder plain `VO` and keep the lettered suffixes on its parts. |
| A new `.cs` file dropped anywhere | Data lives in `Datas/UnityObjects/` or `Datas/ValueObjects/`; the generators and namespace tools depend on it. |

## Related

FlowIoC's full architecture rules live in the project's `AGENTS.md`, written by
*Tools ▸ FlowIoC ▸ AI ▸ Agent Rules*. The legal prefixes and suffixes are declared in
`<Solution>.sln.DotSettings`, written by
*Tools ▸ FlowIoC ▸ Module Configuration ▸ Update Namespace Settings*.
