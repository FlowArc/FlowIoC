#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The asset delivery module: store-delivered content on the device before the boot needs
    /// it, the one setting per group that decides how, what the boot draws while it waits, and
    /// what each store asks of the build.
    /// </summary>
    internal class AssetDeliveryModulePage : ModulePage
    {
        private readonly HelpImages _images = new HelpImages();

        public override string ModuleFolderName => "AssetDeliveryModule";

        public override string Title => "Asset Delivery Module";

        public override string Subtitle => "Store-delivered content on the device before the boot needs it";

        public override FlowIcon Icon => FlowIcon.Layers;

        public override IReadOnlyList<string> RequiredPackages => new[] { "com.unity.addressables.android" };

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "One setting per group, then each store's build.",
                "The Play Asset Delivery schema on an Addressables group is the one place a pack's "
                + "delivery type is chosen; Android and iOS both read it. Android builds an App "
                + "Bundle with asset packs; iOS builds the packs beside the project and uploads "
                + "them to App Store Connect."),
            new HelpTab("Usage", DrawUsage,
                "One line in the boot, one row in the loading sets, one step for an on-demand pack.",
                "EnsurePromised is bound after Begin and reports the Content step; Ensure holds a "
                + "flow until one pack is there. Loads stay what they were: IAssetService asks for "
                + "a key, and the pack is where the bundle happens to be.")
        };

        public override string InstalledHint =>
            "Drop AssetDeliveryServiceRoot into MainScene, bind IAssetDeliveryService.Commands.EnsurePromised "
            + "after Begin in MainContext's boot, and add a Content step to CD_LoadingSets' Boot set; "
            + "the Usage tab shows both.";

        public override string BodyHeadline =>
            "The stores deliver the content; the boot waits for what they promised.";

        public override string BodyTagline =>
            "Play Asset Delivery on Android, Apple-hosted Background Assets on iOS: content that ships "
            + "with a release and lands on the device before the first open - and, on an open that "
            + "comes too early, a download drawn on the loading bar instead of a load that hangs.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "One setting per Addressables group - Unity's own Play Asset Delivery schema - read by "
                + "both platforms: Install Time, Fast Follow or On Demand.");
            painter.Bullet(
                "A boot step, IAssetDeliveryService.Commands.EnsurePromised, that asks the store for "
                + "every pack it promised - all but the on-demand ones - and draws what is still "
                + "missing on the Content step of the loading bar, in megabytes. Nothing missing skips "
                + "the step; a pack that will not come fails it, and the loading screen's retry runs "
                + "the boot again.");
            painter.Bullet(
                "A flow step, IAssetDeliveryService.Commands.Ensure, bound with a pack name where a "
                + "level or a feature needs its on-demand pack, holding the sequence until it is there.");
            painter.Bullet(
                "A service - GetPacksAsync, GetPendingSizeAsync, EnsureAsync with a progress, "
                + "RemoveAsync - for a Command that wants to draw its own step or free space.");
            painter.Bullet(
                "The build side: a manifest of the packs written beside the catalog, the iOS packs "
                + "folded out of the app with their Manifest.json files, the Xcode downloader "
                + "extension and keys added after the build, and a panel that shows what the next "
                + "build will send.");

            painter.SubHeading("The three delivery types");
            painter.Paragraph(
                "Install Time ships inside the app on Android, in one AddressablesAssetPack with the "
                + "catalog; on iOS it is an essential pack, downloaded as part of the install. Fast "
                + "Follow lands right after the install, before the first open - Play fetches it in "
                + "the background, iOS as a prefetch pack - and the boot waits for it if the player "
                + "is quicker than the store. On Demand waits for an Ensure. Both stores re-deliver a "
                + "changed pack with an update, so what a release changes stays in a pack and never "
                + "in a hand-written download.");

            painter.SubHeading("What it is not");
            painter.Paragraph(
                "A remote catalogue. Content that changes with no release - a CDN, Build Remote "
                + "Catalog, the content-update workflow - is Addressables' own and lives beside this, "
                + "not in it; a group with the schema is packed by the store, a group without it "
                + "loads from wherever its Load Path says.");
            painter.Note(
                "Important: the iOS half is written against Apple's Background Assets reference and "
                + "has not run on a device yet. It ships so that a project is set up ahead - the "
                + "extension, the keys, the packs, the plugin - and the first iOS build with a device "
                + "is where it is proved. The selectors FlowAssetPacks.mm uses and the platforms list "
                + "in each Manifest.json are the first things to check; the Setup tab says how to test "
                + "a pack locally before an upload.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("The Root");
            painter.Paragraph(
                "AssetDeliveryServiceRoot goes into every scene whose boot ensures packs - MainScene "
                + "in the setup set - at Initialize Order -40, a free ten among the services; the "
                + "seat does not matter, because the service acts only inside the boot sequence.");

            painter.SubHeading("The groups");
            painter.Paragraph(
                "Run Window > Asset Management > Addressables > Init Play Asset Delivery once; it adds "
                + "the Play Asset Delivery build script and offers the schema to every group. Then "
                + "set each delivery group's Delivery Type in its Inspector. A group without the "
                + "schema, or with the type None, ships inside the app as before.");
            painter.Image(_images.Get("AssetDeliverySchema.png"),
                "The Play Asset Delivery schema on an Addressables group, Delivery Type set to Fast Follow.");
            painter.Note(
                "Important: name a delivery group the way Google names a pack - a letter first, then "
                + "letters, digits and underscores. Unity renames one that breaks the rule at build "
                + "time and the module reads the name it chose, so nothing breaks; but the name on "
                + "the device is then not the name in the Groups window. iOS takes the group name "
                + "with every other character turned into an underscore, and two groups that end up "
                + "the same fail the build with both names in the error.");

            painter.SubHeading("Android");
            painter.Bullet("Build App Bundle on, in the Build Profiles window's Android platform settings. An APK carries no asset packs.");
            painter.Image(_images.Get("AssetDeliveryAppBundle.png"),
                "Build Profiles > Android > Platform Settings, Build App Bundle (Google Play) on.");
            painter.Bullet(
                "Split Application Binary on, in Player Settings > Publishing Settings. Unity generates "
                + "asset packs only when it is on; off, every delivery group is inlined into "
                + "StreamingAssets and nothing is reported.");
            painter.Bullet(
                "Build Addressables with Build > New Build > Play Asset Delivery, or set Build "
                + "Addressables on Player Build. The module writes FlowAssetPacks.json beside the "
                + "catalog with the pack names Unity gave the groups.");
            painter.Bullet(
                "Play-less testing: bundletool build-apks --local-testing on the .aab, then "
                + "install-apks on a connected device; fast-follow and on-demand packs then come from "
                + "the device's own storage instead of the store. Unity ships bundletool under "
                + "PlaybackEngines/AndroidPlayer/Tools.");
            painter.Note(
                "Important: an Addressables build for Android is checked, not fixed. PAD not "
                + "initialised, App Bundle off, Split Application Binary off and a group in no pack are "
                + "each an error in the console at the end of the build.");

            painter.SubHeading("iOS");
            painter.Bullet(
                "The Addressables build for iOS moves each delivery group's bundles out of the build "
                + "folder into ServerData/iOS/AssetPacks/<Pack>/, with a Manifest.json per pack: "
                + "Install Time is Apple's essential policy, Fast Follow its prefetch, On Demand its "
                + "onDemand, every one on the first install and on every update.");
            painter.Bullet(
                "On a Mac the build runs xcrun ba-package per pack and leaves a .aar beside each "
                + "folder. On Windows it writes package-asset-packs.sh beside them; run it on a Mac "
                + "with Xcode 26.");
            painter.Bullet(
                "Upload the .aar files to App Store Connect with Transporter or altool, independent "
                + "of the build; submit them for review with the build that uses them. Every Apple "
                + "Developer Program membership hosts 200 GB per app.");
            painter.Bullet(
                "The player build adds to the Xcode project an ExtensionKit downloader extension "
                + "(one Swift file that takes the system's managed implementation), the App Group "
                + "group.<bundle id>.assets on the app and the extension, and BAAppGroupID, "
                + "BAHasManagedAssetPacks and BAUsesAppleHosting in the app's Info.plist. The step "
                + "changes nothing the second time it runs.");
            painter.Bullet(
                "The module's ED_AssetPackInitialization is listed under the Addressables settings' "
                + "Initialization Objects at the first iOS build; at runtime it points every bundle in "
                + "a pack at the file Background Assets holds it in.");
            painter.Note(
                "Important: managed asset packs need iOS 26. The extension declares that deployment "
                + "target on its own; the app may keep a lower one, and on an older device the boot's "
                + "step fails with that reason. Before an upload, Apple's ba-serve mock server - "
                + "documented under Testing asset packs locally - serves .aar files over HTTPS to a "
                + "device that trusts a self-signed root.");

            painter.SubHeading("The panel");
            painter.Paragraph(
                "Tools > FlowIoC-Modules > Asset Delivery > Panel shows what the next build sends: "
                + "whether Play Asset Delivery is initialised, App Bundle and Split Application Binary, "
                + "the iOS deployment target and App Group, and every delivery group with its type and "
                + "its name on each store. It reads and never edits.");
            painter.Image(_images.Get("AssetDeliveryPanel.png"),
                "The Asset Delivery panel: the two Android switches, the iOS target, every delivery group.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("The boot");
            painter.Paragraph(
                "One line in MainContext, after Begin - so the bar is up to draw on - and before "
                + "anything loads, so a screen that sits in a pack finds its bundle there:");
            painter.Code(
                "CommandBinder.Bind(_internalSignals.Launch)\n"
                + "    .ToSequence<IScreenService.Commands.LoadByTag>(LoadingConstants.SCREEN_TAG)\n"
                + "    .ToSequence<ILoadingService.Commands.Begin>(MainConstants.BOOT_SET)\n"
                + "    .ToSequence<SignalDispatchCommand>(_mainSignals.Outgoing.BootStarted)\n"
                + "    .ToSequence<IAssetDeliveryService.Commands.EnsurePromised>()\n"
                + "    .ToParallel<PreloadScreensCommand>()\n"
                + "    .ToSequence<FillPoolsCommand>()\n"
                + "    .ToSequence<ILoadingService.Commands.Await>(MainConstants.BOOT_SET)\n"
                + "    .ToSequence<SignalDispatchCommand>(_mainSignals.Outgoing.Started);");
            painter.Paragraph(
                "And one row in CD_LoadingSets, in the Boot set: a step with the key Content, a "
                + "weight of about 3 and a message such as \"Downloading content\". The step reports "
                + "\"20 / 40 MB\" as its detail while it fetches, is skipped when nothing is missing, "
                + "and fails when a pack will not come - which the loading screen shows with its "
                + "retry, wired to RetryBoot as it already is.");
            painter.Image(_images.Get("AssetDeliveryContentFailed.png"),
                "On a phone with the pack out of reach: the Content step failed, the set stopped, Retry runs the boot again.");
            painter.Note(
                "The module's assembly references Modules.Loading for ILoadingService - a Service "
                + "crossing, the same one Main makes. Without a Content row in the set the report "
                + "lands on no set and the loading service says so.");

            painter.SubHeading("An on-demand pack");
            painter.Paragraph(
                "Where a flow needs its pack - a level, a chapter, a language - one step holds the "
                + "sequence until the pack is on the device, the pack named where the step is bound:");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.OpenChapter)\n"
                + "    .ToSequence<IAssetDeliveryService.Commands.Ensure>(\"Chapter2\")\n"
                + "    .ToSequence<OpenChapterScreenCommand>();");
            painter.Paragraph(
                "The pack's name is the group's - sanitised on iOS, Unity's on Android when the group "
                + "name breaks Google's rule - and the Asset Delivery panel lists both. Ensure reports "
                + "no loading step, because only the flow knows which set it is in.");

            painter.SubHeading("From a Command");
            painter.Code(
                "[Inject] private IAssetDeliveryService _delivery { get; set; }\n"
                + "[Inject] private ILoadingService _loading { get; set; }\n"
                + "\n"
                + "public override async void Execute()\n"
                + "{\n"
                + "    Retain();\n"
                + "    ILoadingStep step = _loading.Report(\"Chapter\");\n"
                + "    step.Start();\n"
                + "\n"
                + "    bool landed = await _delivery.EnsureAsync(new[] { \"Chapter2\" },\n"
                + "        new ImmediateProgress<AssetPackProgressRVO>(p => step.Progress(p.Fraction)));\n"
                + "\n"
                + "    if (!landed) { step.Fail(\"chapter did not download\"); Stop(); return; }\n"
                + "    step.Complete();\n"
                + "    Release();\n"
                + "}");
            painter.Paragraph(
                "GetPacksAsync answers with every pack the build declared, its policy and whether it "
                + "is on the device; GetPendingSizeAsync with the bytes still to fetch for a set of "
                + "packs; RemoveAsync frees a pack the player is done with, and the next Ensure "
                + "fetches it again.");

            painter.SubHeading("In the Editor");
            painter.Paragraph(
                "There is no store, so every pack counts as on the device: the Content step is "
                + "skipped and an Ensure returns at once. The module's test scene, "
                + "AssetDeliveryTestScene, runs the boot's shape with the step in it; on a device "
                + "with a fast-follow pack not yet landed, the same scene shows the download.");
        }
    }
}

#endif
