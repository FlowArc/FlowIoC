---
name: flowioc-data-types
description: Use when adding or naming data in a FlowIoC Unity project - a ScriptableObject data asset, a serializable value object, saved player state, downloaded backend data, editor-only settings - when unsure whether a class should be CD_/RD_/PD_/ED_/DD_ or carry a VO/CVO/RVO/PVO/EVO/DVO suffix, or when one module needs to read a ScriptableObject another module filed as shared. Also when naming an asset file - a texture, sprite, material, shader, prefab, mesh or FBX, audio clip, animation - and choosing its prefix.
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
Scripts/Runtime/Data/
├── UnityObjects/    # CD_Maps.cs, PD_Maps.cs, RD_MapPool.cs
└── ValueObjects/    # MapVO.cs, MapCVO.cs, MapPVO.cs
```

Data another module reads goes in `Scripts/Shared/Data/` instead, under the same two
folders and the same naming. Shared is an assembly of its own - `Modules.Player.Shared` -
so a screen or sub module can reference the data without reaching the module's Models and
Commands. The naming does not change with the folder; only who can see it does.

Shared holds data and nothing else. The module's public signal holder is next door in
`Scripts/Signals/` (`Modules.Player.Signals`), so referencing a module to read one of its
value objects does not hand you its signals as well. A value object that a public signal
carries still belongs in Shared - that is exactly what `Modules.Player.Signals` references
it for.

## Example

```csharp
// Data/UnityObjects/CD_Maps.cs
[CreateAssetMenu(fileName = "CD_Maps", menuName = "Game/Data/CD_Maps")]
internal class CD_Maps : ScriptableObject
{
    public List<MapCVO> Maps = new();
}

// Data/ValueObjects/MapCVO.cs
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

## Reading another module's asset

The `.Shared` assembly settles the type; the instance is filed once. A ScriptableObject other
modules read goes in the **Shared Scriptables** of one Root's `RootAdapter` - the slot beside the
module's own map - and any injectable reads it through `ISharedDataModel`:

```csharp
public class ShowMatchResultCommand : Command
{
    [Inject] private ISharedDataModel _sharedDataModel { get; set; }

    public override void Execute()
    {
        RD_Match match = _sharedDataModel.GetScriptable<RD_Match>();
    }
}
```

- `GetScriptable<T>()` looks under the type name, `GetScriptable<T>(name)` under the name it was
  filed as - the adapter's own two overloads.
- The slot says the asset is common, not who produces it. A test module's Root files a ready-made
  `RD_Match` when the producer is not in the scene, and the reader cannot tell.
- A Root files its slot when it registers, at `Awake`, before any binding phase - so a
  `PostConstruct` may read one. Nothing waits for `Setup`.
- A second filing of the same name is reported at the Root that made it - a warning for the same
  asset, an error for a different asset under the same name - and the first filing answers.
- An asset nobody filed is an error naming it, and the reader gets null.
- The reader references `Modules.Match.Shared`, as for any published type. That one line is the
  record of who reads whose data; the compiler keeps `Modules.Match` out of reach.
- Scene components go the same way: the adapter's **Shared Mono Map**, read with
  `ISharedDataModel.GetMonoBehaviour<Canvas>("OverlayCanvas")`. The type has to be one the reader
  can name - Unity's own, or one from a Shared assembly.

A Mediator injects nothing but its View, so a screen that needs shared data dispatches, and a
Command reads it.

The module's own two slots - Scriptable Map and Mono Map - are read by its Model off the adapter
in `PostConstruct`, and a Command asks the Model; a Command never reaches for the adapter.
*Tools ▸ FlowIoC ▸ Wiki ▸ Data Types ▸ Root Adapter* shows all four slots with the Model and the
Command that read each.

## Asset files

Every asset file a project makes carries a prefix that says what the file **is** - never where it
is used, and never how a scene flags it. A mesh that is marked static in one scene and moved in
another is still one mesh.

No prefix is a single letter. A type a project holds many of takes a two or three letter
abbreviation; a rare type, or one whose abbreviation would not read, takes the whole word.

| Asset | Prefix | Example |
|---|---|---|
| Texture a material samples (png, tga, jpg, exr, psd) | `TX_` | `TX_Rock_Normal` |
| Sprite - Texture Type *Sprite*, for UI and 2D alike | `SPR_` | `SPR_Icon_Coin` |
| Material | `MT_` | `MT_Rock` |
| Shader, Shader Graph, Sub Graph | `Shader_` | `Shader_Water` |
| Prefab | `PB_` | `PB_Coin` |
| Prefab variant | `PBV_` | `PBV_Coin_Gold` |
| Mesh with no rig (FBX) | `SM_` | `SM_Road_Straight` |
| Rigged, skinned mesh (FBX) | `RM_` | `RM_Hero_Fox` |
| Animation clip, and an FBX that carries only animation | `Anim_` | `Anim_Hero_Fox_Run` |
| Animator Controller | `Animator_` | `Animator_Hero_Fox` |
| Animator Override Controller | `AnimOverride_` | `AnimOverride_Hero_Fox_Skin` |
| Avatar Mask | `AvatarMask_` | `AvatarMask_UpperBody` |
| Audio clip | `SND_` | `SND_SFX_GunShot_01` |
| Audio Mixer | `Mixer_` | `Mixer_Master` |
| VFX Graph | `VFX_` | `VFX_Explosion` |
| Render Texture | `RT_` | `RT_Minimap` |
| Sprite Atlas | `Atlas_` | `Atlas_Shop` |
| Timeline | `Timeline_` | `Timeline_Intro` |
| Physics Material | `PhysicsMat_` | `PhysicsMat_Ice` |
| Volume Profile | `Volume_` | `Volume_Night` |
| Font, and its TextMesh Pro asset | `Font_` | `Font_Lexend_Bold`, `Font_Lexend_Bold_SDF` |
| ScriptableObject data | `CD_` `RD_` `PD_` `ED_` `DD_` | the table at the top of this skill |

- **A category is the second token**: `SND_Music_Combat_01`, `SND_SFX_GunShot_01`, `SND_UI_Click_01`.
  A particle effect is a prefab, so it is `PB_FX_TorchFire` - `FX` is its category, not its prefix.
- **A variant number has two digits**: `_01`, `_02`.
- **A material is named after what it dresses**, not after its shader: `MT_Grass`, never
  `Shader_Grass` on a `.mat`.
- **A sprite and a texture are told apart by the import**, not by where they are shown. A 2D
  game's character art is a sprite even though it is not UI.
- **A prefab variant that is unpacked or made a base prefab** loses the `V` - rename it with the
  change.
- **A prefab named after the class it carries keeps the class name**: `GameplaySystemRoot.prefab`,
  a screen's prefab. The prefix is for content, not for the prefabs FlowIoC's generators write.
- **A scene keeps `<Name>Scene`**, the name Create Module writes.
- **A vendor package's assets are never renamed.** The next update brings the old names back and
  every reference to the renamed copy breaks.

## Common Mistakes

| Mistake | Why it is wrong |
|---|---|
| `MapData`, `MapConfig`, `MapSO` | The suffix family is the convention; a bare descriptive name says nothing about lifetime. |
| Writing to a `CD_` asset at runtime | Config is constant. If it changes during play it is `RD_`, and if it must survive a restart it is `PD_`. |
| A `CVO` list inside a `PD_` asset | The suffix has to match the asset it lives in, or the name stops predicting the lifetime. |
| Naming a mixed holder `GameHexCVO` | Name a two-kind holder plain `VO` and keep the lettered suffixes on its parts. |
| A new `.cs` file dropped anywhere | Data lives in `Data/UnityObjects/` or `Data/ValueObjects/`; the generators and namespace tools depend on it. |
| The same `RD_` asset dragged onto every reader's adapter | It works while the producer is in the scene and reads an asset nobody fills when it is not, and nothing reports it. File it once, in one Root's Shared Scriptables, and read it through `ISharedDataModel`. |
| Reading a shared asset from the reader's own `RootAdapter` | The adapter is the module's own map. Another module's asset comes through `ISharedDataModel`, which reports an asset nobody filed. |
| `T_`, `M_`, `S_`, `P_` on an asset file | No prefix is a single letter. It is `TX_`, `MT_`, `Shader_`, `PB_`. |
| `AC_` on an audio clip or an animator controller | The two collide on the same letters. An audio clip is `SND_`, an animator controller `Animator_`. |
| `UI_` or `IMG_` on a sprite | The prefix says what the file is, not where it is shown. A sprite is `SPR_` in UI and in the world. |
| `SM_` because the mesh is static in the scene | Static is a flag in one scene. `SM_` means the FBX has no rig, `RM_` that it has one. |

## Related

FlowIoC's full architecture rules live in the project's `AGENTS.md`, written by
*Tools ▸ FlowIoC ▸ AI ▸ Agent Rules*. The legal prefixes and suffixes are declared in
`<Solution>.sln.DotSettings`, written by
*Tools ▸ FlowIoC ▸ Module Scanner*.
