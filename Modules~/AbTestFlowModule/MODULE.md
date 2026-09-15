# AbTestFlow

## Purpose
Puts a player into one group of each active A/B test once, writes that group's config assets over
the game's own before any other module reads them, and publishes where the player stands.

## Concepts
ab test, a/b test, split test, experiment, variant, control group, rollout, bucketing, override,
IAbTestFlowService, CD_AbTests, RD_AbTestStatus, AbTestStatusRVO, AbTestId

## Decisions
- **The service starts the module, a Command sequence decides.** `AbTestFlowService.PostConstruct`
  dispatches `ResolveAbTests`, and `ReadStoredAbTestsCommand`, `ProcessAbTestCommand` and
  `ApplyOverridesCommand` do the work. It is PostConstruct rather than Setup because PostConstruct
  runs during the binding pass: a Root at `-90` has changed every config asset before any game
  module's own PostConstruct reads it, and Setup would be a frame too late.
- **The model is data.** It reads `CD_AbTests` and `RD_AbTestStatus` off the Root's adapter and
  files what the commands decide; it rolls nothing. It is bound locally, and only the service is
  bound cross-context - the module's single counterpart, and what the Root asks on quit to put the
  Editor's assets back.
- **No sub services.** Nobody outside calls the module's inner work, so there is no surface to
  group; the dice, PlayerPrefs and the copy sit in the three commands.
- **Nothing is announced.** The decision is made at boot, before any other module is listening, so
  an Outgoing signal would reach nobody. Where the player stands is `RD_AbTestStatus` in Shared,
  filed in the Root's Shared Scriptables and read through `ISharedDataModel` by whoever wants it
  when they are ready, and the deciding commands log it in words.
- **Storage.** PlayerPrefs at `flowioc.abtest.<AbTestId>` as `"<version>|<group>"`, `-` for a
  player the rollout left out. Raising a test's `Version` is the only way to decide again, and it
  decides for everybody - a test is restarted, not amended. A stored group the config no longer has
  is decided again rather than kept.
- **The control group carries no overrides.** The first group is the original configuration, not
  a copy of it, so nothing can drift out of sync. Validation reports an override filed there.
- **Overrides are copied with JsonUtility**, not the Newtonsoft stack the save module uses: that
  one serialises for a file and drops every field pointing at another Object, and a variant that
  swaps a prefab or a sprite is a normal experiment. Lists are deep-copied, so an original never
  shares the variant asset's collections.
- **Two panels, because they are used at different moments** (owner, 2026-09-15): `AB Test Editor`
  while the tests are being shaped, `AB Test Selector` before pressing Play to see one variant.
- **`AB Test Editor`, under `Tools/FlowIoC-Modules/AB Test/Editor`.** Every `CD_AbTests` in the
  project, each test's fields, groups and override pairs drawn from the asset's own
  `SerializedProperty`s through the panel painter's `Property` marks - undo, dirtying and saving
  are Unity's, and the panel edits what the Inspector edits, never a Model. *Add test* seeds an
  active, 100% test with `control` and `variant`; *Raise version*, *Add group*, *Add override*,
  the removes with a confirm on the test. The validator's messages sit under the test they name
  (`AbTestAuthoringTools.Messages`), a message about an unnamed test at the asset. Structural
  changes are deferred to the end of the draw, because the rows are walked by index.
- **A panel, `AB Test Selector`, under `Tools/FlowIoC-Modules/AB Test/Selector`.** Every test of every `CD_AbTests` in
  the project with the group this machine's player is in, read from PlayerPrefs; *Force <group>*
  and *Force outside* write the key in the module's own `<version>|<group>` shape so the next
  boot reads a forced group as a rolled one, and *Reset* forgets it. `AbTestPrefsTools` does the
  prefs without a window so the tests reach it; `AbTestSelectorPanel` only draws. The menu entry says
  `AB Test` because a slash in a menu path opens a submenu. The asmdef references `FlowIoC.Editor`
  for the two, under `#if UNITY_EDITOR`; Unity drops the reference for a player build (owner,
  2026-09-15, opening panels to every module).
- **The Editor's assets are put back on quit.** The originals are captured before the first
  override and restored, so a play session leaves no diff on an asset nobody edited.

## Known gaps
- JsonUtility does not copy `[SerializeReference]` fields. Validation warns; the copy omits them.
- Decisions are local. A backend-driven test would replace the two dice in `ProcessAbTestCommand`.
- Two active tests that override the same asset are not detected.
- The group cannot be picked by hand in game yet - the Selector panel does it in the Editor. The
  PlayerPrefs format above is what an SRDebugger module would write; a change takes effect on the
  next launch.
- The test scene's Raise button lasts one session: `CD_AbTests` is put back on quit, so the next
  launch decides once more for a status stored under the raised version.

<!-- FLOWIOC:BEGIN version=1 hash=6ae3e12e | generated by Tools/FlowIoC/Module Scanner - do not edit inside this block -->
**Kind** Main · **Assemblies** Modules.AbTestFlow, Modules.AbTestFlow.Shared, Modules.AbTestFlow.Signals
**Root** AbTestFlowServiceRoot → AbTestFlowServiceContext
**Incoming** ResolveAbTests()
**Publishes** AbTestConstants, AbTestStatusRVO, RD_AbTestStatus
**Services** IAbTestFlowService
**Sub modules** AbTestFlowTestModule (Test)
<!-- FLOWIOC:END -->
