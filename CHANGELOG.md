# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- **The agent rules are kept up to date without asking.** FlowIoC used to open a modal dialog on
  startup offering to write or refresh the rule block, which was wrong twice over: a modal during a
  load callback blocks every other one behind it - the setup modules install from one, and on a
  first open they waited for the dialog to be answered - and the question had one sensible answer,
  because a stale block describes a version of FlowIoC the project is no longer on. The block is
  generated text between two markers, so nothing a reader wrote is ever touched, which is what
  makes writing it without asking honest. It is now written whenever it is absent or stale, with a
  line in the console saying which file was taken, the same way the agent skills report themselves.
  A project that would rather decide for itself turns it off with a toggle in the Agent Rules
  window, and then nothing is written until Sync is pressed. `Do not ask again`, which lapsed the
  next time the rules changed, is gone - the toggle stays off until it is turned back on.

- **Removing the generated `FlowLogType.cs` is announced rather than done in silence.** Deleting it
  is correct when the settings carry no log types beyond the mandatory channels, because there is
  then nothing to generate - but a deletion that says nothing is indistinguishable from the bug
  fixed in `1.4.3`, where the same call ran on a settings stand-in and took the file with it. The
  cleanup now logs which file it removed and why, and that it comes back as soon as a module or a
  custom channel is registered.

### Fixed

- **The agent rules notice stopped noticing after the first check of a session.** It guarded itself
  with a session flag, and `SessionState` survives a domain reload, so a package update - which is
  a domain reload inside the same session - never re-ran the check: a project that updated twice in
  one sitting kept a rule block describing the version it started on. The check now answers from
  the files themselves, which is the only thing that can be stale, and needs no flag at all.

## [1.4.3] - 2026-09-03

### Fixed

- **Updating the package shows What's New straight away, rather than on the next Editor launch.**
  The notice guarded itself with a session flag so that a domain reload - which happens every time
  a script compiles - would not be mistaken for an update and bring the window back over and over.
  But `SessionState` survives a domain reload and is cleared only when the Editor restarts, and
  updating a package *is* a domain reload inside the same session, so the flag swallowed the one
  event the feature exists for: the reader pressed Update, the new version resolved, and nothing
  was shown until they next opened the Editor. The session now remembers which version it answered
  for instead of that it answered, which still elides a recompile on the same version and no longer
  elides an update to a new one.

- **A project's first open no longer deletes the `FlowLogType.cs` it just generated.** When
  `CD_FlowConsole.asset` cannot be loaded - which happens during the import storm of a first open -
  `FlowLogger` hands back an in-memory stand-in built by `ResetToDefaults`, and that stand-in
  carries the mandatory system channels and no module channels. `FlowLogTypeGenerator` read three
  empty type lists from it and asked `LogTypeSettingsGuard` whether the settings could be trusted;
  the guard answered by counting mandatory channels, which the stand-in has, so the generated file
  was deleted without a word. The next compile then failed with `The name 'FlowLogType' does not
  exist in the current context` in every command that logs, and only focusing the Editor once put
  it right. The settings object now says whether it came from disk, and the guard asks that first,
  so a stand-in is never trusted with a deletion. When it is refused, the console says so rather
  than staying silent.

## [1.4.2] - 2026-09-03

### Fixed

- **What's New is shown to a project that has been on FlowIoC for a while, not only from the
  second update onwards.** The notice remembers the version a reader has seen in `EditorPrefs`,
  and reads nothing recorded as somebody meeting FlowIoC for the first time - who wants the
  introduction rather than a list of changes to a package they have not used. That is right for a
  new project and wrong for every existing one on the day the feature lands, which is how `1.4.1`
  shipped What's New and then could not announce itself to a single reader. The rule now asks the
  project as well as the reader: `ProjectSettings/FlowIoCSetup.json` carries the version the setup
  modules were installed at, so a marker naming an older version is the project saying it has been
  here before, and the notes are shown. No marker, or one naming the version now installed, still
  means a first meeting.

## [1.4.1] - 2026-09-03

### Added

- **A What's New tab on the Help window's Welcome page, and the window opens itself there once
  after an update.** The window has always opened on the introduction, which is right the first
  time somebody meets FlowIoC and wrong every time after a new version lands. The tab is read out
  of the `CHANGELOG.md` the package ships, reduced to one line per entry: every entry's first
  sentence, grouped under the version and section it was written in, newest first. Nothing is
  written twice, so a release still means one file to update. The version a reader has seen is
  remembered per person rather than per project, because a marker committed with the project
  would be ticked by whoever updated the package and nobody who pulled afterwards would ever be
  shown the notes.

- **Add Sub Context says what kind each listed context is, and where it is already used.** The
  window now wears the same `SCREEN` and `CONNECTOR` badges the Root's own list does, so a context
  is recognisable before it is added rather than after. A context that a Root in one of the open
  scenes already lists says which Root has it, and sorts to the bottom of the list instead of
  disappearing from it - adding the same screen context to a second Root with a second `ManagerId`
  is a real thing to want, and hiding the row would have made it look impossible.

### Fixed

- **An error reaches the console whether or not `ENABLE_LOG` is defined, and exactly once.** Every
  `FlowLogger.LogError` overload now routes through one `WriteError` that carries no
  `[Conditional]` and consults no setting, so an error survives both a missing `ENABLE_LOG` define
  and `IsLoggingEnabled` being off - a build that had turned logging off was throwing its
  diagnostics away with it. Errors also take an optional context object, so clicking one in the
  console selects the object it is about, and the three runtime `Debug.LogError` call sites moved
  onto the matching channels; the last `Debug`/`FlowLogger` pair among them had been printing the
  same error twice.
- **`InputRoot.prefab`'s view registers again, and both shipped scenes hang their views off a
  Root.** `InputView` sat on the Root's own GameObject, and bubbling up starts at the view's parent
  rather than at the view itself, so the only way it ever worked was the *Use Root Selection*
  toggle the `ContextSource` rework retired. The view moved to an `InputView` child under
  `InputRoot`, where bubbling up finds the Root with nothing configured. The counter module's test
  scene was re-authored the same way, after `LocalSaveTestScene`: the `Canvas` carries the
  `ViewInjector` and the View itself, and it and the `EventSystem` sit under the test Root, which
  is what the rule about a module's GameObjects living under its Root asked for all along. Every
  other shipped scene and prefab was reserialized onto `ContextSource` in the same pass, so no
  asset FlowIoC ships still carries the retired `UseBubbleUp` and `UseRootSelection` keys.
- **A View that names its Root now actually registers against it.** The injector resolved a
  view's context from its entry when the object started, and registration then threw that answer
  away and looked at the selected Root alone. A view with *Use Bubble-up* off and a Root Name
  typed therefore waited for the named context and registered against whatever Root happened to
  sit above it in the hierarchy - or reported that there was none. `ViewInjector.ResolveContext`
  is now the one place that answers the question, and `ViewExtensions.Register` asks it. A Root
  name that no Root answers to is reported and the object carries on, where
  `RootsManager.GetRootByName` used to throw a `KeyNotFoundException` on a typo.
- **Two views on one object waiting for different Roots no longer strand one of them.** The
  injector subscribed to `OnContextReady` once per view and unsubscribed once per handler run, so
  the first Root to become ready took away the subscription the other view was still waiting on.
  It now subscribes once and lets go only when every view on the object has its context.
- **The inspector's Register button works on a view that does not register itself.** *Auto
  Register* says whether the injector registers the view on its own; it was also consulted by the
  registration itself, so pressing the button by hand did nothing and said nothing.

- **A generated module's signal holders no longer keep `Scripts` in their namespace.** *Create
  Module* wrote `Modules.PlayerModule.Scripts.Shared.Signals` while every module already on disk
  said `Modules.PlayerModule.Shared.Signals`, so Rider reported the namespace as not matching the
  file location and a namespace reorganise would have renamed a type nobody meant to touch. The
  generator wrote its skip list into each assembly's `.csproj.DotSettings` and then read it back
  from a project-wide `Project.DotSettings` that has never existed, so every folder counted as a
  namespace provider. Both sides now answer from `DotSettingsPlan`, the one list the settings files
  are written from, and the folder-to-segment decision moved into `FolderNamespaceSegments`, which
  is covered by tests that need no project on disk.

### Changed

- **`CountdownServiceModule` is now `CounterModule`, and its service counts in both directions
  under names that say so.** The module always measured elapsed time as well as counting down, so
  a name that described only half of it was the wrong name. **This is a breaking change**: the
  folder is `CounterModule`, the assemblies are `Modules.Counter`, `Modules.Counter.Shared` and
  `Modules.Counter.Test`, the namespaces are `Modules.CounterModule.*`, and the types are
  `ICounterService`, `CounterService`, `ICounterModel`, `CounterModel`, `CounterVO`,
  `CounterRequestVO` and `CounterServiceSignals`. `EvaluateElapsedTime` is now `CountUpFrom`, to
  read beside `CountDownFrom`, and the request's `CountdownTick`, `CountdownComplete` and
  `CountdownStop` are `CounterTick`, `CounterComplete` and `CounterStop`. The Root and Context
  keep the suffix - `CounterServiceRoot` and `CounterServiceContext` - because a Root takes the
  colour of whatever it roots and reads that from its own name, which is now written down as a
  rule in the agent rules, the README and the Help window.
- **Add Sub Context offers only the contexts that may honestly go on a Root.** A context that some
  Root declares as its `Root<T>` is built by that Root already, so adding it to a second Root would
  build a second instance and run the same bindings twice - those are now hidden. A module meant to
  be hosted on another module's Root says so with the new `[AllowAsSubContext]` on its context and
  is offered again, and `[ExcludeFromContextWindow]` says the opposite and wins over it. A
  Connector sub-context is offered on the Connector Root and nowhere else, so the wiring between
  two modules stays in the one place it belongs; a context counts as a Connector's when its name
  says so or when it carries `[FlowHeader(FlowRole.Connector)]`. *Create Module* offers
  `[AllowAsSubContext]` as a toggle on a main module that gets a Root, unticked, because a module
  with a Root of its own is the ordinary case.
- **A view says where its context comes from with one value instead of two toggles.** *Use
  Bubble-up* and *Use Root Selection* are replaced by `ContextSource`, which is `BubbleUp`,
  `SelectedRoot` or `RootName`. The two booleans could say both things at once and the inspector
  had to hide one of them to keep the answer single. **This is a breaking change**: an entry that
  selected a Root is read back as bubbling up, because the old toggles are gone from the
  serialised data. The two shipped entries that selected a Root have been re-authored to bubble up
  instead of migrated, because a view that cannot find its Root by bubbling up is a hierarchy
  problem rather than a setting; every other entry FlowIoC ships was already bubbling up. A project
  that had a view selecting a Root has to pick it again.
- **The View Injector's inspector was rewritten in the same visual language as the rest.** One
  card, one folding entry per view wearing a badge that says where its context comes from, and a
  `?` beside every field that opens what that field does - read from the documentation
  `ViewInjectorData` now carries, so nothing is written twice. Selecting a Root inside a prefab
  warns that a prefab cannot hold a scene reference, which used to fail silently when the asset
  was saved. While the game runs each entry reports the Context it reached and whether it
  registered, with the badge and the single action the Root's own lifecycle table uses; the red
  and green buttons are gone. Edits go through the serialized object, so they are undoable and
  land on the prefab or scene that holds the object.
- **`AssetServiceRoot` moved from `-10000` to `-1`**, the last seat in the Services band. It was
  the one shipped Root still carrying a number from the old open-ended scheme, so Initialize Order
  now runs from `-100` to `100` with nothing outside it, exactly as the agent rules, the README,
  the `flowioc-root-order` skill and the Help window's Ordering Roots page all say. Nothing binds
  differently: the asset service still comes up ahead of every module that injects it, and no
  shipped scene overrides the value. A project that placed a Root below `-1` on purpose, to bind
  before the asset service, should check that it still does.

## [1.4.0] - 2026-09-02

### Added

- **Initialize Order is a documented convention rather than a number people guess at.** The Roots
  the package ships fall into bands - Services take negative numbers because they depend on
  nothing, the game's own modules take `0` to `97`, `ConnectorRoot` takes `98` so the wiring reads
  after the modules it wires, `ScreenRoot` takes `99`, and `MainRoot` takes `100` because its
  `Launch` is the entry point. The bands are now written down in the agent rules, in the README's
  lifecycle section, in a new **Ordering Roots** page in the Help window - with a picture of
  `MainScene` showing the Hierarchy authored in the same order - and in a `flowioc-root-order`
  skill installed alongside the others. The page's numbers are checked against the
  `initializeOrder` actually serialised into the shipped prefabs, so the two cannot drift apart.
- **What each lifecycle phase is for, written down.** The binding phases declare, `Setup`
  initialises - it does not run until every Root in the scene has finished binding, so it is where
  a module readies its Models and the only phase that may reach across modules - and `Launch`
  dispatches the first signal. In the agent rules, the README, and the Help window's Root & Context
  page.
- **A `flowioc-connectors` skill**, covering the shape of a Connector sub-context, the `Connect`
  overloads, connection groups, and what to check when a signal is dispatched but never arrives.
- **One visual language across every inspector.** A FlowIoC component now wears a bar that says
  what it is and which module it comes from, coloured by its role: Root purple, Service blue,
  System teal, View orange, Mediator its darker shade, Screen amber, Connector green, Adapter and
  Test grey. Each colour is two values - a deep one the bar is filled with, dark enough that the
  white title clears 4.5:1, and a vivid one for the stripe down its left edge and the help accents -
  and the palette is covered by a test, so a colour that fails contrast or disappears against a
  skin cannot be committed.

  A Root takes the colour of whatever it roots, which is why `ScreenServiceRoot` reads as a Service
  and `ConnectorRoot` as a Connector while a game module's Root stays a Root. A Service, a System
  and a Connector sub-context are not components, so without that rule three of the colours would
  never have been seen at all; the sub-context list badges connector and screen entries for the
  same reason.
- **A help button on every documented field, reading the code's own `///` summary.** Nothing is
  written twice: the parser takes the summary above a field, follows the type's base chain, and a
  field with no comment simply has no button. The Root's fields are documented for it, so the
  lifecycle table explains itself. Where Odin is installed the button joins Odin's own drawer
  chain, so `[Button]`, `[ShowIf]` and the rest keep drawing exactly as before.
- **`[FlowHeader]`**, for a type whose role the package cannot read from the type itself. It also
  takes an optional label, so a type can wear a role's colour without claiming to be one -
  `ViewInjector` wears the Mediator colour and says `VIEW INJECTOR`.
- **`Tools/FlowIoC/Inspector Bar`**, which turns all of this off for a project that wants its
  inspectors untouched.

### Changed

- **One *Module Scan* panel replaces the three tools that used to repair a module.** *Assembly
  Creator Window*, *Module Configuration ▸ Detect & Fix Module Index* and *Module Configuration ▸
  Update Namespace Settings* are gone. Between them they had a dependency order nothing stated -
  the namespace settings silently skip a module whose index entry is stale or whose assembly is
  missing - and none of them could say what was actually wrong. Assembly Creator could not fire at
  all: its checkbox was disabled whenever a module already had an asmdef, and *Create Module*
  always writes one.

  The panel reads every module from the folder tree rather than from the index, so its rows are
  right even when the index is not, and reports nine things: mandatory folders, the Shared
  assembly, the assembly definition, its references, the root `.csproj.DotSettings`, and for the
  project itself the module index, orphaned settings files, the Flow log types and the solution
  code style. `Fix All` repairs everything that does not require renaming an assembly or removing
  a reference someone added by hand; those rows stay red and say what to do. If anything is wrong,
  the console carries one line about it on editor load.

- **BREAKING: a screen declares itself in its context. The `CD_Screen` asset is gone.** A screen
  module's context now derives from `ScreenSubContext<TView, TMediator>`, which binds the View to
  the Mediator, and declares where the prefab lives and how the screen behaves in a `ScreenCVO` -
  `Load = ScreenLoadCVO.Addressable("SettingsScreen")`, `Layer`, `Tag`, `ManagerId`, the two
  animation flags. The context registers the screen with the service in `Setup`; when the screen is
  instantiated the service tells the prefab's `ViewInjector` which context owns the view
  (`ViewInjector.AssignContext`), so the mediator comes from the context that bound it instead of
  being reflected into `ScreenRoot`'s context from strings in an asset.

  Gone with the asset: the type-name strings and the AppDomain scan that resolved them, the
  `ScreenConfigAdapter`, the `ScreenManager`'s config list, the `DirectPrefab` load type (a context
  cannot hold a prefab reference; `Addressable` and `Resource` remain), and
  `Tools/FlowIoC/Screen Config Manager` with its Help page. `Create Module` writes the context, adds
  it to the parent module's Root prefab, and the screen's test Root lists that same context instead
  of re-declaring the screen.

  Upgrading: on the first Editor open, the package generates a context for every `CD_Screen` asset
  whose screen has none, attaches it to the parent Root where it can find one, and asks before
  deleting the assets. A screen that already has a hand-written context, or that used
  `DirectPrefab`, is reported with the `Screen` block to paste, because that file is yours. The
  legacy `CD_Screen` type is kept editor-only for this, and goes in the following release.

- **BREAKING: every ScriptableObject the package ships now carries the data-type prefix its own
  rules ask for.** `CD_PoolGroup` already followed the table; the rest did not. The editor-only
  assets became `ED_CodeGenerator`, `ED_ModuleIndex`, `ED_FolderPainter` and
  `ED_MainModuleDirectoryStructure` / `ED_ScreenModuleDirectoryStructure` /
  `ED_TestModuleDirectoryStructure`, and the config a game reads at runtime became
  `CD_FlowConsole`. The serializable classes they are built out of took the
  matching suffixes: `FolderEVO`, `ModuleDescriptorEVO`, the nine `FolderPainter*EVO` rule types,
  and `CD_FlowConsole.FlowConsoleLogTypeCVO`.

  `FlowConsoleSettings` was public, so code that names it has to be updated to `CD_FlowConsole`.
  Nothing else about it changed.

  The asset files were renamed with the types. The assets FlowIoC writes into a project are moved
  by the path migrator on the first editor tick after the upgrade, keeping their GUIDs and
  everything configured in them, so a project keeps its log types, folder colors and generator
  settings.

- **BREAKING: the Folder Drawer is now the Folder Painter.** The menu item is
  `Tools/FlowIoC/Folder Painter`, the types are `FolderPainter`, `FolderPainterWindow`,
  `ED_FolderPainter` and the nine `FolderPainter*EVO` rule classes, and the namespace is
  `FlowIoC.Editor.FolderPainter`. Unity already calls a property drawer a drawer, and the tool
  paints folders rather than drawing them, so the old name read as the wrong thing twice over.
  The project-local config moves from `Editor/FolderDrawer/ED_FolderDrawer.asset` to
  `Editor/FolderPainter/ED_FolderPainter.asset` on the first editor tick after the upgrade,
  keeping its GUID and every rule configured in it.

- **A Connector now gets signal holders instead of binding them.** `InjectionBinderCrossContext`
  `.GetInstance<PlayerSignals>()` rather than `.Bind<PlayerSignals>()`, in the shipped
  `ConnectorModule` and in every example. `Bind` is get-or-create, so a Connector wiring a module
  whose Root is missing from the scene used to receive a fresh holder, connect to that, and
  silently deliver every signal to nobody. Module Contexts still bind their own holder as before.
- **`InjectionBinder.GetInstance<T>` reports a missing binding instead of throwing.** It used to
  index the container directly and hand the caller a bare `KeyNotFoundException`; it now names the
  type - and the name it was asked for, if any - and returns the default.
- **`ConnectorRoot` moved from `101` to `98`.** The frame barrier already guarantees that every
  signal holder is bound before any `Setup` runs, so the Connector never needed to be last; `98`
  places it after the modules it wires and before the screen host and the entry point.
- **A Root's inspector is one lifecycle table.** Six option boxes and a status box that repeated
  what they said became a single card: a row per phase, with the toggle that says whether it
  happens on its own and - while the game runs - a badge for what has happened and a button to make
  it happen now. Two bugs went with them: the *Bind Mediations* toggle wrote to
  `AutoBindInjections` and so never saved, and a derived Root drew its own fields twice. Removing
  an entry from the sub-context list now records an Undo, which it did not before.
- **The shipped Roots are named after what they root**, so their colours follow from their names:
  `CameraRoot` is `CameraSystemRoot`, `GameplayRoot` is `GameplaySystemRoot`, and the pool prefab
  drops its `Variant` suffix.
- **The help window's banner comes from the inspector palette.** It also gets darker: white on the
  purple it used to be measured 3.7:1.

### Removed

- **The `Assets ▸ FlowIoC ▸ Create Assembly` and `Assets ▸ FlowIoC ▸ Update Module's Namespaces`
  context menus.** Both wrote `.csproj.DotSettings` inside the module folder, where Rider never
  reads it - the file has to sit beside the `.csproj`, which is at the project root - so neither
  had any effect. Rewriting the `namespace` line inside `.cs` files goes with them: once the
  settings file at the root is right, Rider's own *Adjust Namespaces* does that better, moving
  files and fixing `using` directives as it goes.
- **BREAKING: `CustomClassHeaderAttribute` is gone, and with it the header drawer nothing called.**
  The attribute described a gradient bar in colours each type chose for itself, which is the
  opposite of a shared language; a role decides the colour now, and `[FlowHeader]` is there for the
  types whose role cannot be read from the type. `CustomHeaderDrawer`, `CustomHeaderEditor`,
  `GenericCustomHeaderEditor` and the `ED_Header` asset they used were never called from anywhere
  and go with it.

  Upgrading: delete the attribute from your own types. Most need nothing in its place - a Root, a
  View and a screen are recognised on their own.

## [1.3.1] - 2026-08-31

### Added

- **An Input module, installed from the Help window.** It turns the pointer into signals -
  `PointerPressed`, `PointerDragged` and `PointerReleased`, each carrying the screen position - so
  nothing in a game has to read the Input System itself. Dragged is announced only while the
  pointer is down, because a signal per mouse move would be a dispatch per frame for something
  almost no game wants. `SetActionMapEnabled` turns a map on or off by name, which silences input
  at the source rather than leaving it to be ignored downstream. The module brings an action asset
  of its own with a Pointer map bound to mouse and to touch, read from the prefab rather than from
  the project wide actions, so a game can point it at an asset it owns. It is a ready-made module
  rather than part of the setup set: a scene's EventSystem is an ordinary Unity component, not a
  reason to install a module.

### Fixed

- **The setup set had no `EventSystem`, so nothing in `MainScene` could be pressed.** uGUI received
  no pointer input at all, which is why the three difficulty buttons did nothing. `MainScene`
  carries an ordinary EventSystem now. The component beside it matters as much: the generated test
  scenes used `StandaloneInputModule`, which reads through the legacy `UnityEngine.Input` and throws
  in a project whose active input handling is the Input System alone - what a new Unity 6 project is
  set to. FlowIoC looks the Input System's own module up by name, so no package reference is added
  to the Editor assembly and a project without `com.unity.inputsystem` still compiles, and falls
  back to the legacy module when the package is absent.

- **The setup modules could not install into a project that had never had FlowIoC in it.** Without
  a `CodeGeneratorSettings` asset - which until now only the generator menus created - the module
  index was left empty, `FlowLogType` was written with no channels, and every module the set had
  just installed referred to a channel that did not exist. The project would not compile and the
  Editor would not enter play mode. The settings asset is created before the set is registered now.

### Changed

- **A module ships whatever it hands the reader, and adds nothing to the `Tools/FlowIoC` menu.**
  The countdown module's test scene was built by a menu item; it is an ordinary asset in the
  payload now, the way `MainScene` already was, and the `Tools/FlowIoC/Modules` branch that held
  that one item is gone. The payload carries a `.meta` beside every script it references, so the
  scene's references resolve in a consuming project.

- **The Editor's own `SerializableDictionary` is gone**, replaced by
  `UnityEngine.Rendering.SerializedDictionary`, which the package already depended on. The two
  types serialize under different field names, so a settings asset written by an earlier FlowIoC
  arrives with its maps empty; `CodeGeneratorSettings` refills a map that comes back empty from the
  defaults and leaves one that still holds anything alone, because removing a single entry in the
  inspector is a considered act.

- **`MainScene` no longer brings a Global Volume**, and `GameplayRoot`'s initialize order lives on
  the prefab rather than as an override on the scene instance.

- **Two rules were added to the agent rules, the README and the Help window.** A GameObject a
  module needs in the scene goes under that module's Root; and a module whose work outlives a scene
  detaches its Root and marks it do-not-destroy in `BeforeCreateContext`, where the reparenting is
  not decoration - Unity marks only root level objects as do not destroy.

## [1.3.0] - 2026-08-31

### Fixed

- Module assembly names are built in one tested place. Four hand-rolled copies read the suffix off
  a module folder without asking what stood in front of it, so a module named exactly
  `ScreenModule` matched the screen rule with an empty parent and came out as `Modules..Screen`.
  An empty parent falls through to the plain module rule now, and the `ScreenTestModule` branch no
  longer strips twelve characters of a sixteen character suffix.

- The code style FlowIoC ships now reaches a project on its own. The naming rules that decide what
  a `CD_` asset or a `PVO` value object may be called live in the solution level settings file, and
  `Tools/FlowIoC/Module Configuration/Update Namespace Settings` was the only thing that ever wrote
  it - so a project that installed the package and generated a module had every convention
  documented and none of them enforced. The file is written when the Editor opens now, the way the
  agent rules and the skills already are. Only the keys FlowIoC ships are touched, so a team's own
  settings survive, and a session that finds the file already correct writes nothing and says
  nothing.

- The package's own assemblies declare their root namespace, so an IDE stops reporting every
  file in FlowIoC as living in the wrong namespace. Rider derives the expected namespace from
  the folder path below the assembly definition and had nothing to put in front of it, so
  `FlowIoC.BaseModule.Controller` read as one segment too long. A registry install cannot be
  fixed the way an embedded one was - the folder rules a project keeps name a path, and under
  `Library/PackageCache` that path carries a hash that changes with every version - so the
  answer belongs in the assembly definition, which travels with the package.

- A test module is wired to the module it sits under. `Create Module` gave a test module a
  reference to FlowIoC and to its parent's Shared assembly, but not to the parent itself, so the
  first thing a test module tried to test would not compile until somebody edited the asmdef by
  hand. A test module is the one kind allowed to reach anything, so it now gets the reference
  outright. Every other module type is unchanged: reaching a neighbour's Models and Commands is
  still the thing the architecture does not allow, and Shared is still how data crosses.

- A test module's generated signal holder is wrapped in `#if UNITY_EDITOR`, the way its Root and
  Context already were. It was the one file `Create Module` left unguarded, which was enough to
  carry the whole test module into a player build.

- `ModuleDeleter.DeleteModule` no longer opens a dialog of its own. It returns the list of what it
  removed and leaves announcing to the caller, so the Delete Module window still shows its summary
  while a batch script, an editor test or a tool driving the Editor can delete a module without a
  modal blocking the Editor until somebody clicks it.

### Changed

- A module publishes its signals through Shared. The public holder lives in
  `Scripts/Shared/Signals/` and compiles into the module's Shared assembly, so a Connector reaches
  a module through `Modules.X.Shared` alone and no module's assembly ever has to reference
  another's. What a public signal carries moves to Shared with it. The module's own traffic gets a
  second holder, `XInternalSignals` in `Scripts/Runtime/Signals/`, with no `Incoming` and no
  `Outgoing`: those two halves describe a boundary, and an internal signal never crosses one.
  Screen modules therefore get a Shared assembly of their own, and the Create Shared toggle starts
  ticked for them.

- Screen views and mediators are generated straight into `ViewsMediators/`; the `ScreenViews`
  folder is gone. A generated screen view wires its buttons in `OnEnable` and drops them in
  `OnDisable` rather than in `Awake`, because a screen is pooled - `Awake` runs once while the
  screen opens many times.

- The Help window's sidebar nests. A category holds sections rather than pages, so Structure and
  Editor Tools fold out inside Wiki instead of competing with it at the top level. Closed, the
  sidebar is now three entries - Welcome, Wiki, Modules - and a reader picks the part of FlowIoC
  they want before picking a topic in it. Which categories are open is remembered by the path down
  to them rather than by their name, so two modules may both bring a category called Usage. The
  sidebar is wider and keeps its scrollbar's width reserved, so a topic two levels down fits its
  name on one line and the list no longer shifts under the cursor the moment the scrollbar appears.

### Added

- **The setup modules.** A project that gets FlowIoC and has no modules of its own is given the six
  a game starts with, the first time the Editor opens on it: `MainModule` launches the game and
  owns `MainScene`, `ScreenModule` holds the ScreenManager and the layers screens open into,
  `ConnectorModule` is where the modules meet, `GameplayModule` is the game, and
  `MainScreenModule` and `GameplayScreenModule` sit inside their parents. Together they are a flow
  that already runs - the game launches, the main screen opens, picking Easy, Medium or Hard closes
  it and opens the gameplay screen with the difficulty carried as a signal parameter - so a reader
  can see how FlowIoC is wired rather than be told.

  There is no button and no dialog. The set arrives registered: the module index, the `FlowLogType`
  channels, the `.csproj.DotSettings`, the Addressables entries for both screens, and `MainScene`
  at the front of the build list and open. It installs all of it or none - the payload holds GUID
  references that cross module boundaries, so half a set is a set that does not work, and every
  target is checked before anything is written.

  It happens once. `ProjectSettings/FlowIoCSetup.json` records that it did and belongs in source
  control, so delete one of the modules and it stays deleted. A project that already had modules of
  its own when FlowIoC arrived is marked and left alone - having them is a decision - and can still
  take the set from the button on the Setup Modules page in the Help window. A batch run writes
  nothing at all.

- **CameraSystemModule.** Named Cinemachine cameras switched by signal, per-camera last positions
  and custom blends as data, with two adapters that register a rig's cameras on their own.
  Installing it copies its own folder and nothing else, so its Help page carries the connector
  sub-context to write by hand. Its assemblies reference Cinemachine and the render pipeline core,
  which a project may not have: what is absent is worked out and added first, and the intent
  survives the domain reload the resolve usually triggers.

- Shared has a toggle of its own in `Create Module`, beside Create Signals, rather than only being
  reachable by ticking its folder in the preview tree. It starts off for a plain module - one that
  hands nothing to its neighbours has no use for a second assembly - and ticked for a screen
  module, which publishes its signals through Shared. `Tools/FlowIoC/Help` becomes Welcome, Wiki
  and Modules, each opening the window on the first topic of that section rather than always on the
  introduction.

- Ready-made modules, installed from the Help window. A module the package ships lives in
  `Modules~/`, which Unity does not import, and the Install button on its Help page copies it into
  `Assets/Modules/` - then registers it in the module index, gives it its `FlowLogType` channel and
  writes the `.csproj.DotSettings` its namespaces need. Copying the folder by hand gets the files
  and none of the other three. Whether a module is already installed is decided by the assembly it
  declares rather than by the folder it landed in, so renaming that folder or moving it out of
  `Assets/Modules/` - which a game is free to do, the module being its code now - does not offer to
  install a second copy on top of it. A module already in the project is never overwritten: the copy there
  is the one the game has been editing.

- The first of them: **CountdownServiceModule**. Named countdowns with once-a-second callbacks,
  seconds left or 0..1, several listeners on one id, counting up as well as down, and an
  `ITimeSource` behind it so a server clock can replace the device one without touching a call site.
  It ships with its test module, which is also the worked example.

- A Modules category in the Help window, and the first page in it: Countdown Service, with what the
  module does, a Usage tab of worked calls, and a Time Source tab on swapping the device clock for
  a clock the player cannot move.

- A page in the Help window can offer one action, drawn as a button on the right of its banner.
  Install is the first; the tabs beside it change what you are reading, and this changes the
  project.

- A second agent skill, `flowioc-scaffolding`, installed into `.claude/skills` alongside
  `flowioc-data-types`. The agent rules say not to lay a module out by hand; this is the part that
  did not fit in them - which menu item does what, what to fill in, why the optional folders are
  the step that is easiest to get wrong, where the `.csproj.DotSettings` files land and why, and
  how to drive the generators from a terminal against an open Editor.

### Removed

- The Command Execution Test Module sample, and the Samples tab it was imported from. Ready-made
  modules cover the same ground better: they install from the Help window, land somewhere a game
  can actually use them rather than under `Assets/Samples/<package>/<version>/`, and arrive
  registered instead of as loose files.

## [1.2.0] - 2026-08-27

### Added

- The Help window's Wiki opens on a new *Creating a Module* page, ahead of the folder layout
  and the data types, because generating the module is what a reader does before either
  convention applies. It carries a screenshot of `Tools > FlowIoC > Create Module`, says why a
  module is generated rather than made by hand, and explains what the Main, Screen and Test
  types each produce, where they are allowed to sit and what the toggles beside them decide.
  Help pages can show a picture now: screenshots live in `Editor/Help/Images` and are drawn
  through `HelpPainter.Image`, scaled down to the page and captioned.

- The extension methods that lived in the separate FlowIoC-addons package now ship with the
  core package, under `Runtime/ExtensionModule` and the `FlowIoC.ExtensionModule` namespace:
  vector and float maths, enum flag enumeration, list conversion and UTC time formatting.
  `SafeSerializedScriptableObject` and the enum modifier built on it need Odin Inspector, so
  they compile only where `ODIN_INSPECTOR` is defined and the package gains no dependency of
  its own. The addons package keeps its copies under `FlowIoC.Addons.Extensions`, so a project
  that has both installed still compiles.

- The data type convention is written down. A module's data carries its origin in its name:
  `CD_` for config a designer authors, `RD_` for what play produces, `PD_` for what the save
  system keeps, `ED_` for editor tooling and `DD_` for a copy of what a backend owns, each
  with the matching `CVO`, `RVO`, `PVO`, `EVO` and `DVO` suffix on the value objects inside
  it. A value object that carries two kinds at once - an authored half and a runtime half in
  one place - is named after neither and keeps the lettered suffixes on its parts. The rule is
  in the agent rules, in the README, and on a new *Data Types* page in the Help window.

- The skills FlowIoC ships are installed into the project's `.claude/skills` folder when the
  Editor opens, one folder per skill, and refreshed when the package changes one. What gets
  written is logged rather than done silently, and a batch run installs nothing.
  `Tools/FlowIoC/AI/Agent Skills` shows what is there and puts a deleted skill back without
  waiting for the next session. Removing FlowIoC through the Package Manager takes the shipped
  skills with it, file by file, so nothing the consumer wrote is caught up in it: a skill of
  their own, or a note left beside a shipped one, keeps its folder. A removal that raises no
  event - `manifest.json` edited by hand, the folder deleted - is covered the way the rule block
  covers it: every shipped skill opens by saying it applies only while FlowIoC is installed.
  The agent rules are what an assistant is told on every task and so stay short; a skill is what
  it reaches for when one kind of work comes up and can afford the detail. The first is
  `flowioc-data-types`. Only the files the package owns are compared, so a skill you wrote
  yourself is never touched and a note left beside a shipped one survives an install.

- *Create Module* writes a module's signal holder. `Signals` is a folder type of its own now
  rather than a plain folder, so the generator can find it; a new `TempSignals` template
  produces `<Name>Signals` with its `Incoming` and `Outgoing` classes, the Context declares
  and binds it in `SignalBindings`, and a screen Mediator gets it injected. Mandatory for a
  Screen module, which has no Context of its own and so reaches the outside world only
  through its holder; a `Create Signals` toggle for Main and Sub modules, on by default;
  never offered for a Test module. A project whose `DirectoryStructureConfig` assets predate
  the folder type has its `Signals` folder still typed as a plain folder — retype it in the
  asset's inspector, or the generator cannot resolve it.

- A shared code style. `Tools/FlowIoC/Module Configuration/Update Namespace Settings` now also
  writes `<Solution>.sln.DotSettings`, so every project FlowIoC is installed in gets the same
  ReSharper and Rider naming rules, prefixes and spacing. Rider reads only a settings file named
  after the solution and that name differs per project, so the file is generated rather than
  shipped. Only the keys FlowIoC owns are written; anything else in the file survives, and a
  settings file left behind by a renamed solution is removed. The agent rules point at the file
  so agents write code that matches it.

- `Tools/FlowIoC/Help`, an Editor window that introduces the architecture without leaving
  Unity. It opens on the module folder layout — every folder annotated with what belongs in
  it — and the topics beside it walk one diagram at a time through the Root and Context, the
  signal surface, commands and functions, models, views and mediators, and connectors. Each
  step lights up a box, states the rule it stands for, and shows the code that rule produces.
  The window teaches one worked example and never inspects the project it is opened in.

- `FlowIoCModuleIndex.asset` is kept out of version control. FlowIoC writes a `.gitignore` next
  to the asset covering it and its `.meta`. The index is a cache the next rebuild reproduces, so
  two people adding modules on separate branches were meeting in the same serialized file for
  nothing. The rule goes in that folder rather than in the project's root `.gitignore`, which
  belongs to the project: a `.gitignore` only reaches the directory it sits in and below, and
  this is the folder FlowIoC owns and the path migrator can move. Lines outside the
  `FLOWIOC:BEGIN`/`FLOWIOC:END` markers are left alone, the same way the agent rules block works.
  A project that already committed the asset keeps committing it until it runs
  `git rm --cached Assets/Plugins/FlowIoC/Editor/CodeGenerator/FlowIoCModuleIndex.asset*` once —
  `.gitignore` does not untrack what git already tracks.

### Changed

- The Help window sidebar folds *Folder Layout* into a *Wiki* category, alongside the new
  *Data Types* page, and the window now opens on *Welcome* rather than on the folder layout - a
  reader who has never seen FlowIoC meets what it is before meeting how its folders are arranged.

- The agent rules spell out how a Model relates to signals, which was previously left to
  inference. A Model never subscribes to a signal: an incoming signal runs a Command and
  the Command calls the Model. A Model may dispatch its own module's outgoing signals to
  announce that a value it holds has changed — announcing is allowed, listening is not.
  The **Who does what** table in the README says the same thing.
- A Service is now defined by what it depends on rather than by what it talks to. The old
  wording made a Service the module's boundary with HTTP, storage and vendor SDKs, and
  forbade it from deciding anything. A Service is a self-contained unit of work that
  answers the input it is given, is not specific to the game around it, and is free to
  decide whatever its own job requires. A Service several modules need gets its own module.
- **System** joins the vocabulary: the game-specific counterpart to a Service. A System may
  lean on other Systems and Services, waiting on a signal they raise or working from data
  they share, which is exactly what a Service may not do. Both dispatch outgoing signals,
  and a Command drives either the same way it drives a Model. Systems live in `Systems/`,
  as an interface and an implementation.
- The rules name the exceptions to "a module never reaches into another module", which was
  stated absolutely while the framework has always had three. A Service crosses directly —
  reference its assembly, inject its interface, which is what makes a Service worth its own
  module. A nested module may use the types of the module it sits in, one way only: a
  module never knows what its own `z` folders hold. And a test module may reference
  anything, in exchange for every script in it being wrapped in `#if UNITY_EDITOR`.
  Systems are not on that list: two Systems in separate modules meet through a Connector,
  never through an assembly reference.
- `Create Module` produces a `Systems` folder in main modules. Screen and test modules are
  unchanged. The folder list is stored in your project's
  `MainModuleDirectoryStructureConfig.asset`, which an upgrade does not rewrite, so a
  project created before this release keeps its old list — add `Systems` in that asset's
  inspector, or delete the asset and let FlowIoC recreate it.
- Modules are no longer marked with text files. `_module_info.txt` held a module's name and
  kind, both already implied by the folder itself — its own name, and its parent's — and
  `_<foldertype>_info.txt` marked a container folder so a rename could be followed, which
  Unity's own folder GUIDs already do without help. Both are gone: a module's whole identity
  now lives in one `FlowIoCModuleIndex.asset`, next to the code generator settings, keyed on
  folder GUID rather than name or path. The index is a cache, not a source of truth — name,
  kind and nesting are read back off the folder tree on every rebuild, so a stale entry is
  repaired by rebuilding it, never by hand-editing it. Each module's folder-type GUID map is
  the exception: it is durable state, carried forward rather than read back off the tree and
  healed only lazily, so deleting the asset outright is not the same as rebuilding it. A
  project upgrading from an earlier version has its markers swept away the first time it opens
  after upgrading, once every pre-existing module's folder GUIDs are safely on record — 74
  files (37 markers and their `.meta` siblings) in this template's three modules.
- The Create Command window now lists modules. Its own copy of the marker filename was
  `module_info.txt`, missing FlowIoC's leading underscore, so its probe never matched a real
  `_module_info.txt` and the module tree came up empty. `Create Model` and `Create View` used
  the correct name and were never affected.
- A module's kind is decided in one place. `ModuleAutoDetector` compared the parent folder
  against the configured folder names, `DeleteModuleMenu` matched hardcoded `zTest` / `zScreen`
  / `zSub` prefixes and ignored the settings, and `NamespaceProvider` parsed a string out of a
  marker file and silently mapped anything it did not recognise to `Main`. Renaming
  `zScreenModules` in the settings used to make those three disagree with each other.
- Log types for deleted modules are removed. Registration only ever added a channel, so a
  module's channel and its constant in the generated `FlowLogType.cs` used to outlive the
  module's own folder. Removal only ever targets auto-registered channels, and only runs when
  the scan actually found at least one module, so a failed scan is never mistaken for a
  project with no modules and cannot wipe every channel.
- *Module Configuration ▸ Detect & Fix Module Infos* is now *Detect & Fix Module Index*. It has
  rebuilt the index rather than repaired info files since info files stopped existing, and the
  menu was the last place still saying otherwise.
- Folder types a module is not required to have no longer warn when they are missing. About half
  the tracked types are optional, so the warning that a folder could not be found fired several
  times per pass on a project that was perfectly healthy, which is exactly how a warning that
  matters gets ignored.

### Removed

- `SingletonRoot<TContext>`, its duplicate registry, and the *Make Root Singleton* option in
  *Create Module*. A Root that has to outlive a scene load now says so itself by overriding
  `BeforeCreateContext` and calling `DontDestroyOnLoad` there, which is all the base type did
  beyond standing duplicates down. **Breaking:** a project with `SingletonRoot` Roots must
  rebase them on `Root<TContext>` and add that override where the lifetime mattered. Nothing
  deduplicates Roots any more, so a Root that survives scene loads belongs in a bootstrap
  scene and nowhere else.

- `ModuleCleaner`, and with it *Module Configuration ▸ Cleanse Module Infos*. Its only job was
  pruning stale module names out of the container-folder marker files; rebuilding the index
  makes that pruning automatic, since a module missing from the folder tree is simply missing
  from the next rebuild.

### Fixed

- *Update Namespace Settings* survives a module the index is wrong about. The index is a cache,
  and `AssetDatabase.GUIDToAssetPath` answers for a deleted folder with the last path it knew
  rather than with nothing, so the path looked usable and the first `Directory.GetFiles` on it
  threw `DirectoryNotFoundException` - taking the whole run down, including the orphan cleanup
  and the solution code style, which have nothing to do with that module. A folder that is not
  on disk is now reported and stepped over the way an unresolvable folder GUID already was, a
  module that fails no longer stops the ones after it, and the two closing steps run whatever
  happened before them.

- The shipped code style declares `PD_` a legal type prefix. `CD_`, `RD_`, `ED_` and `DD_` were
  all there and the matching `PVO` suffix was too, so a player data asset was the one kind whose
  name Rider flagged.

- Reading a folder name for a type the project's `CodeGeneratorSettings.asset` has never
  heard of no longer throws `KeyNotFoundException`. That asset is written once in the
  consuming project, so any folder type added by a later FlowIoC version is missing from
  every existing copy, and indexing the dictionary directly took module creation down with
  it. Names now fall back to the built-in default.
- A rebuild that could not run no longer leaves its callers working from an empty index. The
  rebuild returned nothing and each caller loaded the index itself, which created an empty one
  and looked exactly like a project with no modules: a newly created module's folder GUIDs went
  unrecorded without a word, and log type detection would have proposed removing every
  auto-registered channel. The rebuild now hands back the index it built, or nothing at all.
- A recorded folder GUID that no longer resolves is looked for by name again. Deleting a folder
  or moving it outside Unity used to skip that folder type for good, because the name fallback
  only ran for a type that had never been recorded at all.
- Deleting the last module out of a renamed container folder now removes the emptied container.
  `zSubModules`, `zTestModules` and `zScreenModules` are configurable names, and the cleanup
  matched the hardcoded ones.

## [1.1.1] - 2026-08-23

### Fixed

- `FlowConsoleSettings.asset` is no longer replaced when it exists on disk but fails to load.
  Unity returns null both for an absent asset and for one whose script cannot be resolved —
  which happens while scripts are not compiling, or after the package's asset paths change —
  and FlowIoC treated the second case as the first, writing a fresh settings asset over the
  real one and taking every auto-registered module log type with it. The file on disk now
  gets the benefit of the doubt and a warning explains what to fix.
- `Assets/FlowIoC/Generated` is no longer deleted when the log type settings come back empty.
  A settings asset loaded from disk always carries its mandatory channels, so an object with
  none of them is a stand-in for one that failed to load, not a project with no log types.
  Deleting `FlowLogType.cs` on its word broke compilation, and the compile errors then kept
  the settings from loading — a loop that repeated on every domain reload.
- Declining the agent rules startup notice is now remembered per project. `EditorPrefs` is
  shared by every project opened with the same Editor, so "Do not ask again" used to silence
  the notice everywhere on the machine. `Tools/FlowIoC/AI/Agent Rules` also gained a button to
  switch the notice back on, which previously required the rules themselves to change.

## [1.1.0] - 2026-08-23

### Migrating from `com.flowioc.core`

Renaming a package changes the asset path of every script it ships. Unity keeps the
association between a script asset and its compiled type in `Library/`, and that
association does not survive the change: `MonoScript.GetClass()` starts returning null
for the package's scripts even though the types are compiled and loaded. Every
`ScriptableObject` the package defines then fails to load, and FlowIoC's own recovery
paths make that permanent — `FlowLogger` recreates `FlowConsoleSettings` from scratch,
losing the auto-registered module log types, and the log type generator, finding no log
types, deletes `Assets/FlowIoC/Generated`. `FlowLogType` disappears, every module that
logs stops compiling, and the compile errors keep the script association broken, so the
cycle repeats on each domain reload.

**Do the migration with the Editor closed:**

1. Close Unity.
2. Edit `Packages/manifest.json`: remove `com.flowioc.core`, add
   `"com.flowarc.flowioc.core": "https://github.com/FlowArc/FlowIoC.git#1.1.0"`.
3. Delete the project's `Library/` folder. It is derived and gitignored; Unity rebuilds
   it on the next open, which is what re-establishes the script associations.
4. Open the project. The first import takes a while.

If you renamed with the Editor open and hit the loop above, the recovery is the same
sequence: close Unity, restore `Assets/FlowIoC/Generated` and
`Assets/Resources/FlowConsoleSettings.asset` from version control **while Unity is
closed**, delete `Library/`, then reopen. Restoring them with the Editor running does not
hold — the running Editor rewrites them before the next compile finishes.

One smaller side effect: removing the old package strips the FlowIoC block from
`AGENTS.md`, because as far as the Package Manager is concerned FlowIoC was uninstalled.
Anything you wrote outside the block survives, and the new package offers to write the
rules back on the next domain reload.

### Added

- `Tools/FlowIoC/AI/Agent Rules` writes FlowIoC's architecture rules into the project's
  root `AGENTS.md` as a marked block and points `CLAUDE.md` at it, so AI coding
  assistants follow the framework's conventions instead of guessing at them. Only the
  text between the `FLOWIOC` markers is touched, so rules you wrote yourself survive, and
  a malformed marker makes the tool refuse to write rather than guess. FlowIoC offers to
  install the block on first open and to refresh it when the rules change — detected by
  hashing the rule text, so edits that ship without a version bump are still caught — and
  removes it again when the package is uninstalled through the Package Manager. The rule
  text ships in `Documentation~/AgentRules.md`.
- `[SignalParam]` accepts an index: `[SignalParam(1)]` binds to the second value of
  that property's type in the signal payload. The index counts within the type, so
  adding a parameter of another type to the signal does not shift it. Commands can
  now read a `Signal<int, int>` or a `Signal<string, string>` correctly.
- An EditMode test assembly at `Tests/Editor`, covering signal parameter resolution.
- A Scene Switcher dropdown on the main toolbar. It lists every scene under
  `Assets/Modules` as `ModuleName/SceneName` and opens the picked one, prompting to save
  the open scene in edit mode and loading through `SceneManager` in play mode. The main
  toolbar API arrived in Unity 6000.3, so the feature compiles out on earlier editors
  and the package minimum stays at 6000.0.

### Changed

- **Package name is now `com.flowarc.flowioc.core`** (was `com.flowioc.core`), matching the
  FlowArc organisation the repository lives under. The Package Manager treats a package
  name as its identity, so this is not an upgrade: remove `com.flowioc.core` from
  `Packages/manifest.json` and add `com.flowarc.flowioc.core` pointing at the new tag.
  Nothing else changes — assembly names, namespaces and the `FlowIoC` folder are untouched.
  **If you are upgrading an existing project, read the migration note below before you
  start.** A fresh install needs none of it.
- `[SignalParam]` properties are now discovered by walking the command's base-class
  chain directly rather than through the assembly scan the other injection attributes
  use. Two consequences for existing code: a `[SignalParam]` declared on a base class
  that lives in a *different assembly* from the command now binds, where it was
  previously skipped without a warning; and a `public` inherited `[SignalParam]` is
  now recorded once rather than twice, so it no longer consumes two payload values.
  Both shifts ripple: unindexed properties take the next unclaimed value of their
  type in declaration order, so a base-class property that newly joins the list — or
  a duplicate that stops joining it — changes which value the properties after it
  receive, and can leave the last one with none. `[Inject]` and `[InjectSignal]`
  discovery is unchanged.
- A dispatched `null` no longer logs `Signal Param is not found!`. It binds to any
  property whose type can hold it, which is the intended behaviour, but code that
  treated that message as a signal-shape alarm will stop seeing it.

### Fixed

- A command with two `[SignalParam]` properties of the same type received the same
  value in both. Properties without an index now take the next value of their type
  that no other property has claimed.
- Binding failures now name the command, the property and the reason instead of
  logging only the parameter type.

## [1.0.1] - 2026-08-19

### Fixed

- Guarded the `using UnityEditor;` directives in `ComponentReference.cs` and
  `ScreenConfig.cs` behind `#if UNITY_EDITOR`. Both files belong to the runtime
  assembly, which is compiled for players, so the unguarded directives broke player
  builds in consuming projects. Verified by compiling the player script assemblies.

### Changed

- **Breaking:** renamed the namespace `FlowIoC.PoolModule.Addressable.Components` to
  `FlowIoC.PoolModule.Components`. `ComponentReference` and
  `AssetReferenceSpawnableObject` moved with it. Update any `using` directive that
  referenced the old namespace.
- Moved `ScreenManager.prefab` out of the package's `Resources` folder to
  `Assets/Prefabs/`, alongside the other code generator templates. Nothing ever loaded
  it through `Resources.Load`; the generator resolves it with `AssetDatabase`, so the
  `Resources` folder only forced the asset into every consumer build. The asset GUID is
  unchanged, so prefab links in already generated scenes still resolve. The package no
  longer ships a `Resources` folder at all.

## [1.0.0] - 2026-08-19

First tagged release, installable through the Unity Package Manager.

### Added

- `CHANGELOG.md` and package manifest metadata (`license`, `documentationUrl`,
  `changelogUrl`, `licensesUrl`, keywords, author) so the package presents itself
  properly in the Package Manager window.
- Declared runtime dependencies on `com.unity.addressables` and
  `com.unity.render-pipelines.core`. These were used by the code but never declared,
  so installing the package into a project without them failed to compile.

### Changed

- Package name is now `com.flowioc.core` (was `flow-ioc`), following the reverse
  domain naming that the Package Manager and scoped registries require.
- Minimum supported editor is now Unity 6000.0 (the manifest previously claimed 2019.1).
- The code generator resolves the package root from its own assembly instead of
  assuming the package sits at `Packages/FlowIoC`. Template and prefab lookups now
  work for Git URL and registry installs, not just embedded ones.

### Removed

- Dead assembly reference to `com.unity.editorcoroutines`; nothing in the package used it.
- Leftover `Packages/manifest.json` and `Packages/packages-lock.json` from when this
  repository was a standalone Unity project.
