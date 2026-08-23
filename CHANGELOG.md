# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

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

### Fixed

- Reading a folder name for a type the project's `CodeGeneratorSettings.asset` has never
  heard of no longer throws `KeyNotFoundException`. That asset is written once in the
  consuming project, so any folder type added by a later FlowIoC version is missing from
  every existing copy, and indexing the dictionary directly took module creation down with
  it. Names now fall back to the built-in default.

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
