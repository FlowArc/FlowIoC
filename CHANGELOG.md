# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

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
