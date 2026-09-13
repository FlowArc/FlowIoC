# ConsoleModule

## Purpose
FlowIoC's own log window, with a channel per module and per framework concern.

## Concepts
flow console, log channel, FlowLogger, FlowLogType, channel colour, channel profile, preferences,
command log, player connection, attach to player, device logs, PlayerLogSender, PlayerLogBridge

## Decisions
- **A channel switch hides plain logs only.** A warning and an error show whatever the switch, the
  group mute or the isolation says, in the window and in what is forwarded to Unity's console;
  `FlowConsoleChannelRule` is the decision, asked by both.
- **A device's rows come over FlowIoC's own message, not Unity's forwarding.** Unity's receiver
  writes a current player's lines straight into the native console and the editor's log callback
  never fires, so the bridge could not see them; and Unity's copy is text, without the channel and
  the flow. `PlayerLogEnvelope` is JSON through JsonUtility so the two ends may differ in version.
- **There is no settings asset.** `CD_FlowConsole.asset` held four things with four owners in one
  committed file, so every module added and every switch flipped landed in the same diff. The
  framework's channels are `SystemLogChannelTable` in code; a developer's switches and preferences
  are EditorPrefs (`FlowConsoleChannelVisibility`, `FlowConsolePreferences`, edited under
  Preferences ▸ FlowIoC ▸ Flow Console); the module list is the module index; a module's colour and
  profile are in the module - written into its generated `FlowLogType` part from the `Colour:` and
  `Profile:` lines of its card, the palette picking by name when the card says nothing.
  `FlowLogChannels` reads every `const string` on `FlowLogType` back by reflection, so nothing keeps
  a second list of channels.
- **Logging compiles only in the Editor and in a Development Build.** Every plain log carries
  `[Conditional("UNITY_EDITOR")]` and `[Conditional("DEVELOPMENT_BUILD")]`, OR'd, in place of an
  `ENABLE_LOG` define FlowIoC used to write into every platform's PlayerSettings and fight any
  build tool that stripped it. An error carries neither.

## Known gaps
- Rows go one message each; nothing batches them, and `TrySend` drops rows when the editor has
  not drained the buffer.
- An IL2CPP player carries file and line in its traces only when its symbols are present; a row
  without them has no source to open.
- The export writes no player name; a saved text of device rows does not say which device.
- A `Colour:` edit made in the card by hand reaches the console only after the generator runs
  again - the next Editor load, a Module Scanner repair, or the Filters panel's window - and the
  compile that follows.
