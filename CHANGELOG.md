# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
