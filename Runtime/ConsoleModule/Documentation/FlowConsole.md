# Flow Console

Flow Console is FlowIoC's own log window. It is worth using instead of Unity's for
one reason: the framework already logs itself into it. Every signal dispatch, every
command step, every context phase, every screen state change is on a channel you can
toggle — so most of the time you diagnose a flow by *watching* it rather than by
adding logs.

Open it at **Tools ▸ FlowIoC ▸ Console ▸ Flow Console**.

- [The Two Kinds of Channel](#the-two-kinds-of-channel)
- [The Window](#the-window)
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

Two of them are not the framework narrating itself. `Unity` carries anything Unity
wrote — a `Debug.Log`, an exception, a native warning — and `Compiler` carries compile
errors and warnings, taken from `CompilationPipeline`. They are read through Unity's
public API and nothing else, so a Unity upgrade cannot quietly break them. Both are
recorded whether or not `ENABLE_LOG` is defined: turning the define off is a statement
about *your* logging, and a console that then showed no compile errors would be useless
at the moment it is most needed.

**Project channels** are yours: one per module, auto-registered, and generated as a `const string`
on `FlowLogType`. A module's channel is declared **in the module**, in a part of its own:

```csharp
// Modules/AnalyticsModule/Scripts/Generated/FlowLogType.AnalyticsModule.cs
namespace FlowIoC.ConsoleModule
{
    public static partial class FlowLogType
    {
        public const string AnalyticsModule = "AnalyticsModule";
    }
}
```

Beside it sits a `FlowIoC.Generated.asmref`, which is what puts the part into FlowIoC's own
assembly rather than into the module's. Parts of a partial class have to share an assembly, and
every module has an assembly of its own - without the asmref this could not be a partial class at
all. `Assets/Plugins/FlowIoC/Generated/FlowLogType.cs` keeps what belongs to no module: the project's
`Default` channel, and any channel added by hand.

**A channel is a name, not a number.** It used to be an `int`, handed out in order and reassigned
whenever the list was sorted - so a module whose name sorted early moved every channel after it onto
a different number, and with it every saved filter and every row already recorded. There was also
nobody to hand out the next number: two people adding a module on two branches were each handed the
same one. A name has neither problem, and a part per module means the two of them do not even touch
the same file.

Because both are generated, a renamed or newly created module gets its channel without anyone
editing a list, and a deleted module takes its channel with it.

---

## The Window

Everything Unity's console does, Flow Console does the same way, so it can be the only
console you keep open.

| Toolbar | What it does |
|---|---|
| `Clear ▾` | Empties the list. The arrow holds **Clear on Play**, **Clear on Recompile** and **Clear on Build**. |
| `Collapse` | Folds rows that say the same thing from the same place onto one line with a count. The row keeps the place of its first occurrence, so the list does not reorder itself while you read it. |
| `Error Pause` | Pauses play mode on the next error, exception or assert. |
| `Flow` | Groups the rows into the flows they belong to. See below. |
| `Pinned` | Shows only the rows you pinned. |
| `Timing` | Leads each row with the frame it was written in and the gap since the row above, instead of the clock. |
| search box | See [Searching](#searching). |
| `Locate` | Scrolls the selected row back into view. |
| `1/2/3 lines` | How many lines a row shows. Two is Unity's shape: the message, and underneath it where it came from. |
| `Source:` | `StackTraceCapture`, raised and lowered where the flow is being read rather than three windows away. |
| `Presets` | Channel filters saved under a name. Two ship with the console; the rest are yours. |
| `Export` | Saves or copies the rows that are showing, as plain text. |

The list follows new logs down while it is resting at the bottom and leaves you alone
once you scroll up. Arrows walk it, PageUp/PageDown move by a screen, Home and End take
the two ends, **Enter** opens the selected row's source and **Ctrl+C** copies it with its
trace. **P** pins the selected row.

Double-clicking a row opens the code that wrote it. Where a diagnostic is the framework
complaining about your code — a command that released without retaining, a view with no
context above it — the row opens **your** file, not the framework's guard clause.

A line is drawn across the list wherever a play session begins or ends, so a list that
spans one does not run the editing and the run together.

The list survives a recompile. `FlowLogger.Logs` is a static and a domain reload would
otherwise empty it at the moment a compile error most wants reading, so the newest five
thousand rows are written down before the reload and read back after it.

### Channels

Clicking a channel hides or shows it. **Alt+clicking** one narrows the console to it,
and alt+clicking the one that is already alone brings the rest back — twenty-nine clicks
are not an answer when one channel of thirty is interesting.

### Searching

Terms are ANDed, a term starting with `-` excludes, and a term wrapped in slashes is a
regular expression. What matched is painted behind the text, because a narrowed list says
which rows survived but not why.

```
screen open        both words
-tick              everything except the tick loop
probe -retry       both at once
/probe (1|2)\d/    a regular expression
```

A half-typed expression says `bad pattern` beside the box rather than quietly emptying
the list. Beside the severity counters is how many rows are shown against how many the
console is holding.

### Pinning

Pinning a row is you saying this one is not noise, so it outranks everything that hides
rows: the channel switches, the severity toggles, the trim that bounds the list, and the
automatic clears on play, recompile and build. A search still narrows past it — that is
looking for something rather than hiding a kind of log — and the `Clear` button still
empties everything, because pressing it is asking for exactly that.

### Flow

Every log the framework writes while a signal runs its commands carries the id of that
flow, and a flow started from inside another carries its parent's. `Flow` reads those
back: each flow opens with a line that folds it away, and a nested one sits indented
under it. Read straight down, a busy frame is four operations interleaved; grouped, each
one is a block you can follow.

A silenced command contributes no node — `[HideCommandLog]` suppresses the framework's
lines and there is nothing left to make one from — so a log you wrote inside it sits
directly under the flow's root. Inventing a node for a command somebody asked to hide
would undo the request.

---

## Logging From Your Code

```csharp
using FlowIoC.ConsoleModule;

FlowLogger.Log(FlowLogType.PlayerModule, "Execute - AddCurrencyCommand");
FlowLogger.LogWarning(FlowLogType.PlayerModule, "Currency clamped to zero.");
FlowLogger.LogError(FlowLogType.PlayerModule, "Save slot is not writable.");
FlowLogger.LogLong(FlowLogType.PlayerModule, serializedPayload);
```

`LogLong` is for output you want kept intact — a JSON body, a serialized save — that
would otherwise be truncated.

> **Spell the names out. Never `nameof` inside a log message.**
> `$"{nameof(Execute)} - {nameof(AddCurrencyCommand)}"` written inside `AddCurrencyCommand`
> is a real reference to the type, so Find Usages and a plain search answer *where is this
> Command used* with the command's own logging lines instead of the Context that binds it.
> A rename then leaves the literal stale, and that is the cheaper of the two costs.

Every one of these methods carries `[Conditional("ENABLE_LOG")]`. Without that
scripting define the calls are removed by the compiler, including the string
interpolation that would have built the message. This is why you can leave
`"..."` logs in shipping code without paying for them.

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
| `DeepAnalysis` | On: the detail panel shows the class name and the full stack trace. Off: only the source line. Editor-only — on device no `ConsoleLog` object is created at all. |
| `StackTraceCapture` | Which logs work out where they came from. `WarningsAndErrors` is the default and the one to leave alone: capturing a source builds the whole managed stack as a string and picks it apart, and the framework logs every signal, injection and command, so this is the most expensive thing the console does. Raise it to `Always` while following a flow and put it back afterwards. `Never` is the cheapest and shows no source for anything. |
| `MaxLogCount` | How many logs are kept. The oldest are dropped past this, so a long play session does not hold every log it ever wrote. `0` keeps all of them. |
| `SendLogsToUnityConsole` | Mirror everything into Unity's own console, for when you need the two side by side. |
| `AutoAddEnableLogDefine` | Manage the `ENABLE_LOG` scripting define automatically. |
| `LogTypes` | The channel list: name, value, colour, visibility, and whether the channel is mandatory or auto-registered. |

Per-channel, `IsVisible` is what the window's toggles write. `IsMandatory` marks a
channel that cannot be **removed** — it is what the framework's own channels carry, so
that module detection never deletes one — and says nothing about hiding: every channel
can be switched off in the window. `ProfileName` attaches a default profile to every log
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

To hide a whole project channel, clear its `IsVisible` in the settings, or switch it off
in the window. That is a global switch, not a per-loop one, so prefer the two flags above
when only one loop is noisy.

For a loop you did not write — somebody else's module, or the framework's own lines — the
search box does the same job without touching any code: `-tick` hides every row whose
message carries the word and leaves the channel alone.

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

### A log says "Source not captured"

That is `StackTraceCapture`, and it is the default rather than a fault. Ordinary
logs do not work out where they came from, because doing so is the console's most
expensive operation and the framework writes a log for every signal, injection and
command. Warnings and errors still carry their source. Set `StackTraceCapture` to
`Always` while following a particular flow, and put it back when you are done.

### The console forgets old logs

`MaxLogCount` caps what is kept, at 5000 by default, and the oldest go first. Raise
it, or set it to `0` for no limit, if a long session has to be read back whole.

### `FlowLogType.MyModule` does not exist

The generated file is out of date. It is regenerated when the console registers
module channels — open the Flow Console window once after adding a module, or run
*Tools ▸ FlowIoC ▸ Module Scanner*. Never edit
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
test runs, or at a natural boundary such as returning to the menu — or turn on **Clear
on Play** and let entering play mode do it.

### A pinned row disappeared

The `Clear` button empties everything, pins included. Everything else leaves them: the
automatic clears, the trim at `MaxLogCount`, a recompile, and turning a channel or a
severity off. A search is the other exception, and it is not a fault — a search is
looking for something, and answering it with rows pinned for another reason is noise.

---

## Related

- [README — FlowIoC at a Glance](../../../README.md#flowioc-at-a-glance)
- [Commands](../../BaseModule/Controller/Documentation/Controller.md) — the command
  and signal channels in practice
- [Base Module](../../BaseModule/Documentation/BaseModule.md) — what the `Context`
  channel is narrating
