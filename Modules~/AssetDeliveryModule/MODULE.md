# AssetDelivery

## Purpose
Brings store-delivered content onto the device before the boot needs it - Play Asset Delivery on Android, Apple-hosted Background Assets on iOS - and draws what is still missing on the loading bar.

## Concepts
asset delivery, play asset delivery, PAD, background assets, asset pack, fast follow, on demand, install time, download, IAssetDeliveryService, EnsurePromised, Content step

## Using it
1. `AssetDeliveryServiceRoot` into the scene whose boot ensures packs (MainScene), Initialize Order -40.
2. `MainContext`: `.ToSequence<IAssetDeliveryService.Commands.EnsurePromised>()` after
   `ILoadingService.Commands.Begin` and before `PreloadScreensCommand`.
3. `CD_LoadingSets`, Boot set: a step `Content`, weight 3, "Downloading content".
4. Each delivery group: `Window > Asset Management > Addressables > Init Play Asset Delivery` once,
   then the Play Asset Delivery schema's Delivery Type on the group (Install Time / Fast Follow /
   On Demand); a name Google accepts (`[A-Za-z][A-Za-z0-9_]*`), or Unity renames it.
5. Android: Build App Bundle on (Build Profiles), Split Application Binary on (Player Settings >
   Publishing Settings); Addressables `Build > New Build > Play Asset Delivery`; Play-less test with
   `bundletool build-apks --local-testing` + `install-apks`.
6. iOS: the Addressables build leaves `ServerData/iOS/AssetPacks/<Pack>/` and
   `package-asset-packs.sh` (run on a Mac with Xcode 26), upload the `.aar` files to App Store
   Connect; the player build adds the extension and keys itself. iOS 26 on the device.
7. An on-demand pack in a flow: `.ToSequence<IAssetDeliveryService.Commands.Ensure>("Pack")`.

The detail, with images, is the Help page: Tools > FlowIoC > Help > Modules > Asset Delivery
(`Editor/Help/Pages/Modules/AssetDeliveryModulePage.cs`); `Tools/FlowIoC-Modules/Asset Delivery/Panel`
shows what the next build sends. The device check lives in
`Assets/Modules/DeviceCheckModule/zSubModules/AssetDeliveryCheckModule` (host only).

## Decisions
- **One setting per group, both stores.** Unity's own `PlayAssetDeliverySchema` on the Addressables
  group is where a pack's delivery type is chosen; iOS reads the same value. Install Time is Apple's
  essential, Fast Follow its prefetch, On Demand its onDemand - each on the first install and on
  every update. A schema of our own would have been a second tick carrying the same answer.
- **The boot waits for what the store promised, with no config.** `EnsurePromised` ensures every
  pack whose policy is not On Demand: install-time packs are there already and cost one state
  query, fast-follow packs are what an early open finds missing. On-demand packs are the game's
  own `Ensure("Pack")` step where a flow needs them. No `CD_` list of boot packs to keep in step
  with the groups.
- **The step draws the bar itself.** `EnsurePromised` injects `ILoadingService` and reports the
  `Content` step - skipped when nothing is missing, megabytes as the detail while it fetches,
  failed and the sequence stopped when a pack will not come - so a game adds one line to
  `MainContext` and one row to `CD_LoadingSets`. That makes `Modules.AssetDelivery` the first
  ready-made module to reference a setup module (`Modules.Loading`), the same Service crossing
  `Modules.Main` makes; the owner chose it over handing the game a Command to paste (2026-09-15).
- **Android pack names are read, never guessed.** A group named `Local_Screen-Loading` breaks
  Google's rule and Unity's build renames it by an algorithm the package keeps internal, so the
  build hook joins the group's bundles with `CustomAssetPacksData.json` and writes the name Unity
  chose into `FlowAssetPacks.json`. iOS pack ids are the group name sanitised to
  `[A-Za-z0-9_]`, and two groups that sanitise alike fail the build.
- **One manifest, one reader.** `FlowAssetPacks.json` sits beside the catalog on both platforms and
  the runtime reads only it - not Unity's Android file, whose shape is the package's to change,
  and not the group schema, which is Editor-only.
- **iOS bundles leave the app at the Addressables build.** The packager moves each delivery
  group's bundles out of `Addressables.BuildPath` before the player build copies the folder, so
  they never enter the app; the catalog keeps their local ids and `AssetPackInitialization`
  rewrites them to Background Assets' `url(for:)` path at runtime, the way the Android package's
  own transform does for Play. Addressables' `AddPathToStreamingAssets` hook is internal and
  all-or-nothing, so moving the files is the one door.
- **The Xcode step is a template, not a capability.** `AssetDeliveryXcodeProject` writes what
  Xcode's "Background Download" template writes: an ExtensionKit target
  (`com.apple.product-type.extensionkit-extension`, embedded into `$(EXTENSIONS_FOLDER_PATH)`),
  one Swift file taking `ManagedDownloaderExtension`'s defaults, the App Group on both targets,
  the three `BA*` keys. Unity types the product by its extension, so the product type is fixed
  by a targeted replace after the write; the trampoline test proves it idempotent.
- **No `Scripts/Signals`.** The service answers its callers; a failure reaches the player through
  the loading step's `Fail` and the loading screen's retry, which already runs the boot again.

- **Proved on a phone, 2026-09-15.** OnePlus CPH2747, Android 16, an AAB of
  `AssetDeliveryCheckScene` installed with `bundletool --local-testing`: the boot began at
  t=2.26 s, `EnsurePromised` asked Play for `Local_ScreenLoading` (fast follow), the download
  ran (`startDownload` → `notifyModuleCompleted`, 21 ms from local storage) and the set
  completed; with the pack removed from local storage the step failed with
  `Unknown: InternalError`, the loading screen showed "Loading stopped at Content." with Retry,
  and Retry ran the boot again. The Editor path (nothing missing → skipped) runs in the same
  scene. Play in local testing reports no pack size, so the detail stays empty then; a real Play
  install reports bytes.

## Known gaps
- The iOS half has not run on a device: `FlowAssetPacks.mm`, the extension, the packs and the
  transform were written against Apple's Background Assets reference on a Windows machine. The
  first iOS build with a device is its test; the selectors in the plugin and the `platforms` list
  in `Manifest.json` are the first things to check.
- Below iOS 26 managed asset packs do not exist; the gateway says so and the boot's step fails with
  that reason. A game that supports older iOS keeps the content it needs inside the app.
- Android learns a pack's size only during its download (`GetAssetPackStateAsync` reports one total
  per request), so `GetPendingSizeAsync` is exact for one pack and a total for several.
- A remote catalogue - content that changes with no release - is not part of this; it is
  Addressables' own and a separate item.

<!-- FLOWIOC:BEGIN version=1 hash=62f461e1 | generated by Tools/FlowIoC/Module Scanner - do not edit inside this block -->
**Kind** Main · **Assemblies** Modules.AssetDelivery
**Root** AssetDeliveryServiceRoot → AssetDeliveryServiceContext
**Services** IAssetDeliveryService
**Sub modules** AssetDeliveryTestModule (Test)
<!-- FLOWIOC:END -->
