# MobileNotification

## Purpose
Schedules local notifications on iOS and Android from a catalogue asset - "your chest is ready",
"come back, it has been three days" - asks for permission as a step a game binds where it wants,
and sends the come-back reminders by itself when the app is left.

## Concepts
notification, local notification, mobile notification, push, reminder, return reminder, schedule,
cancel, channel, permission, badge, tray, opened from, picture, banner, big picture, attachment,
IMobileNotificationService, CD_MobileNotifications

## Using it
1. `MobileNotificationServiceRoot` into the scene the game boots from, Initialize Order -20, the
   catalogue on its adapter (the shipped `CD_MobileNotifications`, or the game's own).
2. Project Settings > Mobile Notifications: Android icons registered under the names the
   catalogue uses; "Reschedule on Device Restart" on; iOS "Request Authorization on App Launch"
   off - the ask is the module's step.
3. One line where the ask belongs:
   `.ToSequence<IMobileNotificationService.Commands.RequestPermission>()`.
4. A computed time is a game Command: `_notifications.Schedule("ChestReady", chest.Remaining,
   slot.ToString(), chest.Name)`. A fixed one is a bound step:
   `.ToSequence<IMobileNotificationService.Commands.Schedule>("FreeChest")`.
5. Nothing for the return reminders: the catalogue's list goes out on pause and comes back on
   resume.
6. A banner on a template: the image under `Assets/StreamingAssets/`, its relative path in the
   template's `Picture` - `Notifications/chest.png`. Android shows it under the text when the
   notification is expanded, iOS as the thumbnail at the right of the banner.
7. `Tools/FlowIoC-Modules/Mobile Notification/Panel` edits the catalogue, previews each template as
   the Android and iOS trays draw it with sample arguments in its placeholders - the picture
   included - flags the three Mobile Notifications settings that fail silently, and lists what
   the service scheduled while the game runs.

## Decisions
- **Platform gateways, not the package's unified API.** `Unity.Notifications.Unified` offers one
  Android channel and no icon per notification; a game wants a "Chest" channel the player can
  keep while muting "Reminders", and a chest icon on the chest. So `AndroidNotificationGateway`
  and `IosNotificationGateway` speak to the two centers directly, bound under `#if` in the
  Context the way `IHapticPlayer` is, and `EditorNotificationGateway` logs - and delivers what is
  due as a log line, so the test scene shows an arrival without a device.
- **The return reminders are the module's.** WayOfKings scheduled its 1-day / 3-day / 7-day
  reminders once after the permission answer and never cancelled them, so they fired at players
  who had played that morning. Here they are scheduled when the app is left and taken back when it
  returns, from the catalogue, with no game code (owner, 2026-09-15). Each reminder is tagged with
  its minutes - `Return#1440`, `Return#4320`, `Return#10080` - because three reminders on one
  template with no tag would be one identity replacing itself.
- **Identity is key plus tag.** A scheduled notification is a template key and an optional
  instance tag; the identifier `key#tag` is the iOS id and the data on both platforms, and Android's
  int id is an FNV-1a hash of it. The same identity scheduled again replaces; cancelling names the
  same pair. No table of magic ids.
- **Permission is a step, never a boot action.** `Commands.RequestPermission` holds a sequence
  until the OS answers, and the game binds it where the ask belongs. The module reads the status
  at initialize and offers it on the interface; scheduling while denied is a plain log, because a
  denied permission is the player's choice.
- **No `Scripts/Signals`, no `Scripts/Shared`.** The interface is the whole surface, and the two
  facts settled at boot - the permission, the notification that opened the app - are read from
  it, never announced: a listener would come up too late to hear.
- **Minutes in the asset.** A day is 1440 and a device test can say 0.2; one unit everywhere.
- **Inexact scheduling.** Exact alarms need a permission and a rationale on Android 12+; a
  reminder does not need the minute.
- **The panel previews, it does not send.** `MobileNotificationPanel` draws a likeness of the two
  trays from the catalogue - the app name, the small icon tinted, the title, a two-line body, the
  large icon, the picture - so a developer sees a title that will not fit before a build does. The
  icon list is read off the package's settings manager by reflection, because the package
  publishes Add and Remove but not the list; the two switches come off `NotificationSettings`. The
  catalogue is edited here the way the A/B Test editor edits its asset - authoring, through
  SerializedObject and Undo - while the running service is only read.
- **A picture is a file, copied once.** Both platforms want a path on the device, not a texture:
  Android's `BigPictureStyle` decodes a file, iOS's attachment is a file URL. `StreamingAssets` is
  the one folder a build carries as files, but on Android it sits inside the APK where only
  `UnityWebRequest` reads it, so `PreparePicturesCommand` copies every picture the catalogue names
  into `persistentDataPath/flowioc-notifications/` at initialize, once, and the draft carries that
  copy's path. A picture not there yet - a schedule in the first seconds, a copy that failed - goes
  out as a plain notification with a log line, never as a refusal. What the whole tray looks like
  is the OS's decision - Android 12+ draws an app's own layout inside the system template, iOS
  wants a Content Extension - so the module offers the one shape both trays draw by themselves.
- **Proved on a phone, 2026-09-15.** OnePlus CPH2747, Android 16, `MobileNotificationCheckScene`
  installed with `bundletool --local-testing`: `Commands.RequestPermission` raised the system
  dialog and reported Granted; Schedule A set an `RTC_WAKEUP` alarm on
  `UnityNotificationManager` and the notification landed in the tray 60 s later with the
  Reminders channel, the title and the body; tapping it opened the app and the label read
  `Opened from: Test#a`; the resume cleared the tray; Home set three alarms at +1, +3 and +7 days
  (`Return#1440`, `Return#4320`, `Return#10080`) and the next resume took all three back. The
  picture the same night: `PreparePicturesCommand` put the copy under
  `files/flowioc-notifications/`, the posted notification read `android.template =
  BigPictureStyle` with a 900x450 bitmap, and the tray showed the banner under the title and
  the body once expanded. Two things the phone taught: the package drops a notification whose
  `ShowInForeground` is off while the app is in front (`UnityNotificationManager.java`), so the
  Test template fires only after Home; and `BigPictureStyle` replaces the body with its summary
  text when expanded, so the gateway sets the body as the summary too.

## Known gaps
- The iOS gateway was written on Windows against the package source; the first Xcode build is
  its test.
- `OpenedFromKey` holds the last notification that opened the app until another does; there is no
  acknowledge. A game that acts once compares it with what it acted on.
- A notification received while the app is in the foreground is not surfaced; a game that wants
  an in-game toast for it asks for the module's first signal.
- The `Scheduled` list is what this session asked for; a delivered notification stays in it until
  cancelled or the session ends, because neither platform reports delivery back.

Version: 1.0.0

<!-- FLOWIOC:BEGIN version=1 hash=9a05fb72 | generated by Tools/FlowIoC/Module Scanner - do not edit inside this block -->
**Kind** Main · **Assemblies** Modules.MobileNotification
**Root** MobileNotificationServiceRoot → MobileNotificationServiceContext
**Services** IMobileNotificationService
**Sub modules** MobileNotificationTestModule (Test)
<!-- FLOWIOC:END -->
