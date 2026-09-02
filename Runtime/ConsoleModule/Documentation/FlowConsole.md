# Flow Console

Flow Console is FlowIoC's own log window. It is worth using instead of Unity's for
one reason: the framework already logs itself into it. Every signal dispatch, every
command step, every context phase, every screen state change is on a channel you can
toggle — so most of the time you diagnose a flow by *watching* it rather than by
adding logs.

Open it at **Tools ▸ FlowIoC ▸ Console ▸ Flow Console**.

- [The Two Kinds of Channel](#the-two-kinds-of-channel)
- [Logging From Your Code](#logging-from-your-code)
- [Formatting With Profiles](#formatting-with-profiles)
- [Settings](#settings)
- [Reading a Flow](#reading-a-flow)
- [Silencing Noise](#silencing-noise)
- [Scenarios](#scenarios)
- [Pitfalls](#pitfalls)

---

## The Two Kinds of Channel

**Framework channels** are built in and written by FlowIoC itself:

| Channel | What appears on it |
|---|---|
| `Context` | Root and Context lifecycle: initialize, start, each binding phase, setup, launch, destroy |
| `Injection` | Bindings created and resolved |
| `Signal` | Every dispatch, with the signal name and parameter count |
| `Command` | Every command step as it executes |
| `CommandOperation` | Group orchestration and pool returns |
| `Function` | Function execution |
| `Screen` | Screen open, show, hide, unload, layer decisions |
| `Pool` | Pool creation, checkout, return |
| `Model` | Model activity |
| `Asset` | Asset load and release |

You never write to these — they are the framework narrating itself. You toggle them
in the console window.

**Project channels** are yours: one per module, auto-registered, and regenerated into
`Assets/Plugins/FlowIoC/Generated/FlowLogType.cs` as `const int` fields.

```csharp
namespace FlowIoC.ConsoleModule
{
    public static class FlowLogType
    {
        public const int Default          = 100;
        public const int AnalyticsModule  = 1000;
        public const int AudioModule      = 1020;
        // ...
    }
}
```

Because they are generated, a renamed or newly created module gets its channel
without anyone editing a list.

---

## Logging From Your Code

```csharp
using FlowIoC.ConsoleModule;

FlowLogger.Log(FlowLogType.PlayerModule, $"{nameof(Execute)} - {nameof(AddCurrencyCommand)}");
FlowLogger.LogWarning(FlowLogType.PlayerModule, "Currency clamped to zero.");
FlowLogger.LogError(FlowLogType.PlayerModule, "Save slot is not writable.");
FlowLogger.LogLong(FlowLogType.PlayerModule, serializedPayload);
```

`LogLong` is for output you want kept intact — a JSON body, a serialized save — that
would otherwise be truncated.

Every one of these methods carries `[Conditional("ENABLE_LOG")]`. Without that
scripting define the calls are removed by the compiler, including the string
interpolation that would have built the message. This is why you can leave
`$"..."` logs in shipping code without paying for them.

`AutoAddEnableLogDefine` in the settings adds the define for you; turn it off for a
release build and the entire logging layer disappears.

---

## Formatting With Profiles

A `FlowLogProfile` decorates a message with a prefix, a postfix, colours and styles.
It is a fluent builder, so a module can define its profiles once and reuse them.

```csharp
private static readonly FlowLogProfile Warning = new FlowLogProfile()
    .SetPrefix("[ECONOMY]", FlowTextStyle.Bold, "#FFAA00")
    .SetMessageStyle(FlowTextStyle.Italic)
    .SetMessageColor("#DDDDDD")
    .SetPostfix("<-- check this", FlowTextStyle.None, "#888888");

FlowLogger.Log(FlowLogType.EconomyModule, "Currency went negative.", Warning);
```

| Method | Sets |
|---|---|
| `SetPrefix(text, style, color \| hex)` | the tag before the message |
| `SetPostfix(text, style, color \| hex)` | the tag after it |
| `SetMessageStyle(style)` / `SetMessageColor(color \| hex)` | the message itself |
| `SetColor(color \| hex)` | prefix, message and postfix at once |

`FlowTextStyle` is a flags enum — `None`, `Bold`, `Italic`, `Underline` — so styles
combine:

```csharp
.SetPrefix("[BOOT]", FlowTextStyle.Bold | FlowTextStyle.Underline, "#00CCFF")
```

Colours take either a `Color` or a hex string.

Keep profiles as `static readonly` fields. A profile built inline allocates on every
log call, which matters exactly where logging matters least — inside a loop.

---

## Settings

The `CD_FlowConsole` asset controls the whole layer.

| Setting | Effect |
|---|---|
| `IsLoggingEnabled` | Master switch. Off: nothing is recorded. |
| `DeepAnalysis` | On: each log captures class name and full stack trace, and the detail panel shows them. Off: only the source line. Editor-only capture — on device no `ConsoleLog` object is created at all. |
| `SendLogsToUnityConsole` | Mirror everything into Unity's own console, for when you need the two side by side. |
| `AutoAddEnableLogDefine` | Manage the `ENABLE_LOG` scripting define automatically. |
| `LogTypes` | The channel list: name, value, colour, visibility, and whether the channel is mandatory or auto-registered. |

Per-channel, `IsVisible` is what the window's toggles write. `IsMandatory` marks a
channel that cannot be hidden. `ProfileName` attaches a default profile to every log
on that channel, so a module can have a consistent look without passing a profile at
each call site.

---

## Reading a Flow

The framework channels are most useful in combination. A single button press should
read like this:

```
[Signal]           Signal is dispatched: 'PurchaseRequested' with 1 parameter!
[CommandOperation] [CommandGroup][InitializeGroupWithSignal] : 'PurchaseRequested'.
[Command]          Execute as Sequence : ValidatePurchaseCommand
[Command]          Execute as Sequence : SpendCurrencyCommand
[Signal]           Signal is dispatched: 'CurrencyChanged' with 1 parameter!
[Command]          Execute as Sequence : GrantItemCommand
[CommandOperation] Command is returned to pool! - GrantItemCommand
```

Reading that top to bottom answers most questions without a breakpoint:

- **Dispatch line missing** → the mediator or connector never fired. The problem is
  upstream of the command chain.
- **Dispatch present, no command line** → the signal is not bound, or you bound a
  different instance of the signal holder.
- **Chain stops mid-way** → the last command that logged retained and never released.
- **Commands run in an order you did not expect** → check where `ToParallel` sits in
  the binding.

Turn off every channel but `Signal` and `Command` while chasing a flow. Turn on
`Context` when something did not initialise, and `Screen` or `Pool` when the symptom
is visual.

---

## Silencing Noise

A tick loop dispatching many times per second drowns everything else. Two switches
suppress framework lifecycle logs without hiding the channel entirely:

```csharp
// Silences this signal's dispatch and group lines.
public Signal Tick = new(hideCommandLog: true);
```

```csharp
// Silences this command's execute and pool-return lines.
[HideCommandLog]
internal class AdvanceTimersCommand : Command { }
```

Both are needed for a fully silent loop — the signal flag does not cover the command
lines and vice versa. Neither affects your own `FlowLogger` calls inside the command
body.

To hide a whole project channel, clear its `IsVisible` in the settings. That is a
global switch, not a per-loop one, so prefer the two flags above when only one loop
is noisy.

See [Commands — Silencing High-Frequency Chains](../../BaseModule/Controller/Documentation/Controller.md#silencing-high-frequency-chains)
for the full table.

---

## Scenarios

### Watch the flow before adding a log

```csharp
// ✅ Turn on the Signal and Command channels and press the button. The console
//    already shows which step is missing — no code change, no rebuild.
```

```csharp
// ❌ Sprinkling Debug.Log through four commands to find out which one runs, when the
//    framework has been logging exactly that the whole time.
Debug.Log("ValidatePurchaseCommand start");
Debug.Log("ValidatePurchaseCommand end");
```

### One channel per module

```csharp
// ✅ Auto-registered, so the console can filter to just this module.
FlowLogger.Log(FlowLogType.EconomyModule, "Granted 100 soft currency.");
```

```csharp
// ❌ Everything on Default. The filter becomes useless and you are back to reading
//    a wall of text.
FlowLogger.Log(FlowLogType.Default, "Granted 100 soft currency.");
```

### Say what happened, not that you got here

```csharp
// ✅ The line is useful six months later, in a bug report from a player.
FlowLogger.Log(FlowLogType.EconomyModule,
    $"Purchase '{_itemId}' for {_price} {_currencyType}; balance now {_model.Balance}.");
```

```csharp
// ❌ Tells you the method ran, which the Command channel already told you.
FlowLogger.Log(FlowLogType.EconomyModule, "PurchaseCommand executed");
```

### Reuse profiles

```csharp
// ✅ Built once, used everywhere in the module.
private static readonly FlowLogProfile Economy = new FlowLogProfile()
    .SetPrefix("[ECONOMY]", FlowTextStyle.Bold, "#FFAA00");

FlowLogger.Log(FlowLogType.EconomyModule, message, Economy);
```

```csharp
// ❌ A new profile object per call, inside the hot path.
FlowLogger.Log(FlowLogType.EconomyModule, message,
    new FlowLogProfile().SetPrefix("[ECONOMY]", FlowTextStyle.Bold, "#FFAA00"));
```

### `LogError` for things that are actually wrong

```csharp
// ✅ An error is a state the game cannot recover from on its own.
FlowLogger.LogError(FlowLogType.SaveModule, "Save file is corrupt; falling back to defaults.");
```

```csharp
// ❌ Errors used for flow control. The error filter fills with expected outcomes and
//    stops being the first place anyone looks.
FlowLogger.LogError(FlowLogType.ShopModule, "Player cannot afford this item.");
```

---

## Pitfalls

### Nothing appears in the console

Check in this order: `IsLoggingEnabled` in the settings; the channel's `IsVisible`
toggle in the window; and whether `ENABLE_LOG` is defined. Without the define every
`FlowLogger` call is compiled away, so the code looks correct and produces nothing.

### `FlowLogType.MyModule` does not exist

The generated file is out of date. It is regenerated when the console registers
module channels — open the Flow Console window once after adding a module, or run
*Tools ▸ FlowIoC ▸ Module Scan*. Never edit
`Assets/Plugins/FlowIoC/Generated/FlowLogType.cs` by hand; it is overwritten.

### `FlowLogger.Log(SystemLogType.Signal, ...)` does not compile

The `SystemLogType` overloads are `internal` — they belong to the framework. Game
code uses the `int` overloads with a `FlowLogType` constant.

### Logs are missing their stack trace on device

`DeepAnalysis` captures class and stack information in the Editor only; on a device
no `ConsoleLog` object is created for it. For on-device diagnosis, put the context
you need into the message itself.

### The console is slow with a long session

Every log is retained in `FlowLogger.Logs`. Call `FlowLogger.ClearLogs()` between
test runs, or at a natural boundary such as returning to the menu.

---

## Related

- [README — FlowIoC at a Glance](../../../README.md#flowioc-at-a-glance)
- [Commands](../../BaseModule/Controller/Documentation/Controller.md) — the command
  and signal channels in practice
- [Base Module](../../BaseModule/Documentation/BaseModule.md) — what the `Context`
  channel is narrating
