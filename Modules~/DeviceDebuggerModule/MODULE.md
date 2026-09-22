# DeviceDebugger

## Purpose
An on-device debug panel in the shape of SRDebugger, for a development build: the log the Flow
Console would show, the options a game and its modules chose to expose with `[DebugOption]`,
every public signal of every module in the scene, a stats strip and the device's facts - read and
steered on the phone without a cable.

## Concepts
device debugger, debug panel, on-device console, SRDebugger, options, cheats, DebugOption,
IDeviceDebuggerService, error badge, error overlay, stats, fps, signals tab, UI Toolkit

## Using it
1. `DeviceDebuggerServiceRoot` into the scene the game boots from, Initialize Order -95, the
   `CD_DeviceDebugger` asset on its adapter (the shipped one, or the game's own: trigger, corner,
   log capacity, the error badge, fps on the trigger). The Root is persistent; one per game.
2. Nothing else for the Console, Signals, Stats and Info tabs: they read the scene.
3. An option is one attribute on a public signal field, in the holder under `Scripts/Signals`:
   `[DebugOption("Gameplay", "Win level")] public Signal WinLevel = new();` on `Incoming` is a
   button; `Signal<bool>` a toggle, `Signal<int|float|double>` a number (`Min`/`Max` make it a
   slider), `Signal<string>` a text field, `Signal<TEnum>` a dropdown; `Argument = 1000` makes a
   payload signal a button carrying the constant. The same attribute on an `Outgoing` field is a
   value row showing the last payload, and a value row with the same category and label as a
   control feeds that control's shown state.
4. A step a Service ships carries the attribute on its class, more than once when the argument
   differs: `[DebugOption("Haptic", "Play Success", Argument = HapticPreset.Success)]` on
   `IHapticService.Commands.Play` is a button that runs the step with that parameter. Haptic,
   Local Save, Mobile Notification and the screen service ship theirs; a step whose Service is
   not in the scene is left off the panel.
5. Open it from the corner (the pill, three taps in a `TripleTap` zone, or the red badge after an
   error), or from code: inject `IDeviceDebuggerService` and call `Show()`, or bind
   `.ToSequence<IDeviceDebuggerService.Commands.Show>(DebugTab.Console)` where a settings screen's
   hidden button belongs.
6. In a release build the Root stays, `IsAvailable` is false, and every call is a no-op; the panel
   never exists there.

## Decisions
- **A development-build thing, one constant.** Logging compiles only in the Editor and in a
  Development Build, and the panel follows the same line: `DeviceDebuggerConstants.IS_AVAILABLE`
  is the module's one `#if`, and every decision that depends on it is taken in a Command reading
  it. A release player carries the Root and a service that answers "not available", nothing else.
- **UI Toolkit, not uGUI.** `ListView` virtualises a thousand log rows for the cost of the visible
  twenty; `PanelSettings` scales by physical size, so the text is the same size on every phone -
  the complaint against Unity's own development console; and the layout is text, which an agent
  writes and a diff shows. The game's screens stay uGUI; this panel is not the game's UI.
- **Options are found, never registered.** A `[DebugOption]` on a public signal field or on a
  shipped step is the whole declaration; `DiscoverOptionsCommand` walks every cross-context
  holder on each open and scans the assemblies for annotated steps once. The attribute lives in
  the package so a holder compiles with the debugger uninstalled, and a ready-made module can
  bring its own options without referencing anything. Internal holders are bound locally and are
  never seen - they are not public surface.
- **An option is a signal, never a property.** SRDebugger reads and writes a property; here a
  control dispatches the signal and the Command bound to it decides. A control learns the truth
  from an `Outgoing` value row with the same key; nothing reads a Model. A step option runs
  through a signal of the debugger's own, bound in its own binder with `ToSequence(Type)`, so a
  tap is an ordinary step in the Flow Console with the retain and release bookkeeping every step
  gets.
- **No signal on the log path.** `FlowLogger.OnLogRecorded` calls the Model's `AddLog` directly:
  the framework logs every dispatch and every command, and a signal per row would record itself
  for ever. The hook is installed from the service's `PostConstruct`, during the module's own
  binding pass, so the binding lines of every Root after `-95` are in the ring and a boot that
  fails on the phone is readable without a cable.
- **The mediator reads the model to paint, and changes nothing in it.** The ring, the options and
  the info are live data the view shows on its tick; a tab tap is a `Show` with the tab, so
  discovery runs again and the Command decides; the frame sampler is the mediator's own, because
  a Command per frame would spam the console.
- **Holders are re-walked on every open and nothing of a scene module is kept.** The Root is
  persistent so the ring survives a scene load; a persistent module never holds a scene module's
  holder, so the option list is rebuilt from the cross-context binder each time.
- **The Console is the Flow Console's two-line row.** The kind as an icon drawn with Painter2D
  (a player has no Editor icons), the message alone on the first line, the channel tag in its
  colour and the source under it - the leading rich-text tag the logger writes is taken off the
  message for that, and put back for search and copy. No clock on the row; the detail carries it.
  Whole rows are never tinted: a page of one colour per channel read as noise on the phone. The
  Filters button folds out the logger's channel switches, the game's modules first. A tapped row
  opens its detail as a sheet over the lower third, with Close and a Copy of its own, so the rows
  above stay in view and the next tap lands on another row; a tap is a press and a release that stayed put, never a drag,
  because the ListView's own selection took a scroll for a tap. The button beside Clear says "Copy all logs" and does that - the ring, whatever the filters
  show. The header is two rows: Clear, Copy all logs and the three kinds, then Search with Filters
  beside it; the search field keeps its width whatever is typed. Scrollers are 8dp thin with no
  arrow buttons, the way a phone draws one.
- **The error badge is the on-device error overlay.** While the panel is closed an error or an
  exception turns the corner red with the unread count; tapping it opens the Console, DPI-scaled
  and readable. It shows even with the trigger set to `None`, unless the asset turns it off.
- **A panel unit is a density-independent pixel.** `PanelSettings` scales by physical size with a
  reference of 160 dpi, so `14px` in the sheet is 14dp on every phone - Android's own body size -
  and `44px` a finger. The first build used 96 (a desktop pixel) and the pill covered a third of
  the screen. The panel keeps the whole screen and pads its content by the safe area plus the
  asset's `BottomInset` (24dp), so a notch's strip is covered rather than showing the game and
  the scrollbar's end stays off the rounded corner; the trigger sits inside the same insets.
  Padding, not root offsets, because the trigger and the panel are the root's absolute children
  and an offset would leave the strip transparent. The bar and the filter row carry
  `flex-shrink: 0`, or a tall list on a short screen squeezes them.
- **A press is answered by a class, not by `:active`.** On a touch screen the ScrollView captures
  the pointer the moment it lands, and UI Toolkit takes `:active` off the button under it at once,
  so Run, Set and Fire looked untouched under a finger. `PressFeedback` listens on the panel root
  in the trickle-down phase, finds the control under the press by class, and keeps `dd-pressed`
  on it while the finger is down and for 180 ms at least, so a quick tap is seen too. The release
  is listened for on the control itself, not the root: a captured pointer's events go to the
  capturing element alone, so a root listener never saw the up and the button stayed pressed.
  Up, cancel, capture-out (the ScrollView taking the pointer to scroll) and leave all end it.
- **Proved on a phone, 2026-09-16.** OnePlus CPH2747, Android 16, 1272x2772 at 560 dpi,
  `DeviceDebuggerCheckScene` as a development APK: the pill and the badge, touch on every tab,
  the God mode toggle answered by its Outgoing row, `+1000 coins` read back as `Coins now 1000`,
  an exception thrown from a button raising the badge and opening the Console on it, the Info rows
  with the safe area `1272 x 2631` and the top inset honoured.

## Known gaps
- A `Signal<T1, T2>` and any payload the panel cannot type are drawn disabled with the reason.
- No bug reporter, no docked mode, no keyboard shortcut, no pinned options.
- Of the ready-made modules, Haptic, Local Save, Mobile Notification and Screen annotate a step;
  the others are one attribute away.
- iOS is untested; the Android phone check is the proof so far.

Version: 1.0.1

<!-- FLOWIOC:BEGIN version=1 hash=6aaa847e | generated by Tools/FlowIoC/Module Scanner - do not edit inside this block -->
**Kind** Main · **Assemblies** Modules.DeviceDebugger
**Root** DeviceDebuggerServiceRoot → DeviceDebuggerServiceContext
**Services** IDeviceDebuggerService
**Sub modules** DeviceDebuggerTestModule (Test)
<!-- FLOWIOC:END -->
