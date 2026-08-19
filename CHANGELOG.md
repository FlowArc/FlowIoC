# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
