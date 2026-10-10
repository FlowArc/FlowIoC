---
name: flowioc-models
description: Use when writing or changing a Model in a FlowIoC module - how it holds its state, one record per thing rather than parallel lists or dictionaries, filling it in PostConstruct from its CD_ config and SD_ save assets, keying and indexing it, what its interface hands a Command or a screen, and when reviewing or refactoring a Model whose data is split across collections that every read has to put back together.
---

# Models in FlowIoC

This skill applies only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.birrstudio.flowioc.core` and `Packages/FlowIoC/` does not exist, delete the folder this file
is in: FlowIoC put it there and is no longer in the project.

A Model owns the module's state and the rules that keep it valid, never subscribes to a signal,
and may announce a change - that much is in `AGENTS.md`. This skill is about the inside: how the
state is held, how it is filled, and what the Model hands out.

## One record per thing

**A Model holds one object per thing it keeps, found under one key.** What the config says of a
power-up, what the save holds of it and what play produced about it are three halves of one thing,
so they sit on one object:

```csharp
// Data/ValueObjects/PowerUpVO.cs
internal class PowerUpVO
{
    public PowerUpCVO Config;   // the CD_PowerUps entry itself
    public PowerUpSVO Save;     // the SD_PowerUps entry itself
}

// Models/PowerUpModel.cs
private readonly Dictionary<PowerUpType, PowerUpVO> _powerUps = new();
```

The record takes a plain `VO` name and its parts keep their own suffixes - the data types skill,
*A value object that carries two kinds at once*. Every read is then one lookup:

```csharp
public int Count(PowerUpType type) => _powerUps.TryGetValue(type, out PowerUpVO powerUp) ? powerUp.Save.Count : 0;
```

What this replaces is the Model that keeps a thing in pieces:

```csharp
private readonly Dictionary<PowerUpType, PowerUpSVO> _owned = new();
private readonly List<PowerUpType> _types = new();
private readonly Dictionary<PowerUpType, int> _unlockLevels = new();   // copied out of PowerUpCVO
```

Three collections share one key, one field was copied out of the config and the config entry
thrown away, and every read - *is it offered*, *is it unlocked* - puts the pieces back together,
here with a linear `_types.Contains` in front of a dictionary lookup. A fourth field on the config
means a fourth collection, and a new read means remembering which of them to consult.

### A second collection is an index, never a second half

Two collections are fine when both hold **the same records**:

```csharp
private readonly List<LoadingSetRVO> _sets = new();                        // the order the config lists them in
private readonly Dictionary<string, LoadingSetRVO> _setsByKey = new();     // the same objects, found by key
```

The test is what the second collection holds. The same objects, reached another way, is an index.
A different field of the same thing, under the same key or the same position, is a record torn in
half.

- **Parallel lists** - `_joyDistances`, `_joyTypes`, `_joyLengths` read at the same `i` - are one
  `List<JoyRVO>`.
- **Parallel dictionaries** - `_soundByKey`, `_clips`, `_lastPlayed`, `_lastVariant`, all keyed by
  `AudioKey` - are one `Dictionary<AudioKey, SoundRVO>`.
- **A list of keys beside a dictionary** is the dictionary's `Keys`, or, when the order means
  something, a `List<PowerUpVO>` of the same records.

### Keep the entry, not a copy of its fields

The record holds the `CVO` and the `SVO` themselves. The config entry keeps every field the next
feature will want, and the save entry is the object inside the `SD_` asset, so a count changed on
the record is the count Local Save writes. Copying a field out - `_unlockLevels[type] =
entry.UnlockLevel` - is a second copy that drifts and a field the next feature has to copy again.

## Filling it

A Model reads its own assets off the Root adapter in `PostConstruct` (the data types skill,
*Reading another module's asset*, for another module's). It fills the records **one source at a
time**, each in one pass:

```csharp
public void PostConstruct()
{
    var adapter = _root.GetComponent<RootAdapter>();
    CD_PowerUps config = adapter.GetScriptable<CD_PowerUps>();
    SD_PowerUps data = adapter.GetScriptable<SD_PowerUps>();

    // null checks, each reported once

    FillConfigs(config);
    FillSaves(data);
}

private void FillConfigs(CD_PowerUps config)
{
    foreach (PowerUpCVO entry in config.PowerUps)
    {
        if (_powerUps.ContainsKey(entry.Type))
        {
            FlowLogger.LogError($"CD_PowerUps lists {entry.Type} twice; the second entry is ignored.");
            continue;
        }

        _powerUps[entry.Type] = new PowerUpVO { Config = entry };
    }
}

private void FillSaves(SD_PowerUps data)
{
    foreach (PowerUpSVO save in data.PowerUps)
    {
        if (save == null || !_powerUps.TryGetValue(save.Type, out PowerUpVO powerUp) || powerUp.Save != null)
            continue;

        save.Count = Mathf.Max(0, save.Count);
        powerUp.Save = save;
    }

    foreach (PowerUpVO powerUp in _powerUps.Values)
    {
        if (powerUp.Save != null)
            continue;

        powerUp.Save = new PowerUpSVO { Type = powerUp.Config.Type, Count = Mathf.Max(0, powerUp.Config.StartCount) };
        data.PowerUps.Add(powerUp.Save);
    }
}
```

- **The config decides what exists.** Its pass creates the records; a duplicate is reported and
  skipped.
- **The save attaches to them.** Its pass looks each entry up by key. An entry the config does not
  list stays in the file untouched and is not offered; a second entry for the same key is not read.
- **What is still missing starts from the config**, and the new save entry is added to the `SD_`
  asset so it is written with the rest.
- **No loop runs inside another.** Finding the save entry for each config entry by walking the save
  list is a nested search; a dictionary built in one pass answers it.
- **Clamp and repair on the way in**, once, so no read has to.

## What the Model hands out

- **Reading is open.** A Model may hand out what it holds as it is - the `CVO`, the `SVO`, the
  record, a shared asset's value object - to a Command that needs it, or on a signal to a screen
  that shows four of its ten fields. Copying that into a new structure buys nothing.
- **A field the Model keeps valid, or the save writes, changes through the Model.** A count that
  must not go negative changes through `Spend(type)` and `Grant(type, n)`: a caller that sets
  `Save.Count` itself skips the rule, and the save writes the result. The same holds for two fields
  that have to stay in step - an owner list on one record and a key list on another - because the
  Model is the one place that keeps them in step.
- **A flag a Command keeps for its own flow is the Command's to set.** A field that guards no rule
  and reaches no save may be set on the handed-out record directly: `set.WatcherRunning = true`
  stops a second watch coroutine, `set.BeginAnnounced = true` remembers that one run's Began went
  out - announcing once is the Command's decision. A `SetWatcherRunning(set, bool)` whose body is
  that one assignment protects nothing.
- **A new value object only when nothing existing carries it.** Three cases earn one: a value
  derived by a rule travels with the data - `IsLocked`, compared against the player's level inside
  the Model so no Command and no screen repeats the rule; a reader needs several records combined;
  or the record is a Runtime or `internal` type another module cannot reference, and a public
  signal needs a type in `Scripts/Shared/`. `PowerUpStateVO` is the first and the last at once.
- **A collection goes out read-only** - `IReadOnlyCollection<PowerUpType> Types => _powerUps.Keys`.
  A `Dictionary`'s order is not a promise; when a reader depends on order, keep the records in a
  `List` as well and hand that out.

## What goes wrong

| Mistake | Why it is wrong |
|---|---|
| Parallel lists read at the same index | One insertion or removal in one of them and every read after it pairs the wrong halves. Nothing reports it. |
| Several dictionaries under the same key | Every read reassembles the thing, and every new field adds a collection someone forgets to clear or fill. |
| A field copied out of a `CVO` and the entry dropped | The next feature needs another field and copies again; the copy is not what the asset says once someone edits it. |
| A nested loop to pair config with save | Quadratic, and it hides the one rule - the config decides what exists - inside a search. |
| `List.Contains` as a lookup | A linear search where a key was available. |
| A caller writing a field the Model keeps valid or the save writes | The change skips the Model's rules, and the save writes it. Change it through a Model method. |
| A Model method that is one assignment to a Command's own flag | Ceremony that protects nothing. A flag that guards no rule and reaches no save is set where it is used. |
| A new value object that copies fields a `Shared` one already has | A second shape of the same data to keep in step. Hand the existing one out and let the reader take the fields it needs. |
| A reader deriving a rule from raw values (`level >= unlockLevel` in a screen) | The rule now lives in two places. The Model derives it and hands out the answer. |

## Related

The record's name and its parts' suffixes are the data types skill's. Whether a piece of data
belongs to a Model at all, a sub system or a module of its own is the systems and services skill's.
