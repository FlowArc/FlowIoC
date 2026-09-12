# ConsoleModule

## Purpose
FlowIoC's own log window, with a channel per module and per framework concern.

## Concepts
flow console, log channel, FlowLogger, FlowLogType, ENABLE_LOG, command log, player connection,
attach to player, device logs, PlayerLogSender, PlayerLogBridge

## Decisions
- **A channel switch hides plain logs only.** A warning and an error show whatever the switch, the
  group mute or the isolation says, in the window and in what is forwarded to Unity's console;
  `FlowConsoleChannelRule` is the decision, asked by both.
- **A device's rows come over FlowIoC's own message, not Unity's forwarding.** Unity's receiver
  writes a current player's lines straight into the native console and the editor's log callback
  never fires, so the bridge could not see them; and Unity's copy is text, without the channel and
  the flow. `PlayerLogEnvelope` is JSON through JsonUtility so the two ends may differ in version.

## Known gaps
- Rows go one message each; nothing batches them, and `TrySend` drops rows when the editor has
  not drained the buffer.
- An IL2CPP player carries file and line in its traces only when its symbols are present; a row
  without them has no source to open.
- The export writes no player name; a saved text of device rows does not say which device.
