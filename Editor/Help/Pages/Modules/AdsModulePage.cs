#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The ads module: the socket, the MAX plug with its define and its asset, the two steps and
    /// what each does to the sequence, and the setup steps that fail silently when skipped.
    /// </summary>
    internal class AdsModulePage : ModulePage
    {
        public override string ModuleFolderName => "AdsModule";

        public override string Title => "Ads";

        public override string Subtitle => "Rewarded and interstitial ads from any module, the mediation SDK behind a plug";

        public override FlowIcon Icon => FlowIcon.Ad;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "The Root in the boot scene, the SDK with its define and its ad units, the plug on the Root's Sub Context Types.",
                "Each step fails silently when skipped: with no plug listed every show answers NotReady "
                + "and only the result says so; with the define missing the plug is not compiled and the "
                + "Root reports a context it cannot find; with the ad units missing the plug never initializes."),
            new HelpTab("Usage", DrawUsage,
                "Show from the module that decided it - a bound step, or a Command that reads the result.",
                "The rewarded step stops the sequence on every outcome but Rewarded, so the grant behind "
                + "it needs no check; the interstitial step always releases."),
            new HelpTab("Providers", DrawProviders,
                "The socket, the shipped MAX plug, and your own in a hundred lines.",
                "A provider loads and shows what it is told, reports what happened, and decides nothing.")
        };

        public override string InstalledHint =>
            "Drop AdsServiceRoot into your boot scene, add the plug for the SDK you installed to its "
            + "Sub Context Types, file its ad units, and bind the shows as steps; the Setup tab has the steps.";

        public override string BodyHeadline =>
            "One ShowRewarded from anywhere, and the answer comes back to whoever asked.";

        public override string BodyTagline =>
            "The chest that offers a video, the level that ends on an interstitial - each is one step in the "
            + "module that decided it, and which SDK shows the ad is one entry on a Root.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "IAdsService: ShowRewarded and ShowInterstitial with a placement and a callback, IsReady "
                + "for a button, SetConsent, SetAdsRemoved, SetMuted. A Service, so any module references "
                + "Modules.Ads and injects it.");
            painter.Bullet(
                "A socket: IAdsProvider, one mediation SDK. The shipped AppLovin MAX plug compiles only "
                + "when the SDK is in the project; a game's own plug is one class and one hosted context.");
            painter.Bullet(
                "The policy an SDK leaves to the app: one ad of each format kept loaded, a failed load "
                + "retried with backoff, the next ad loaded after every close, interstitials paced and "
                + "skipped once ads are removed, a show nobody answers timed out.");
            painter.Bullet(
                "Five steps a game binds: Commands.ShowRewarded (releases only when the reward was "
                + "earned), Commands.ShowInterstitial (always releases), Commands.SetConsent, "
                + "Commands.SetAdsRemoved, Commands.Initialize.");
            painter.Bullet(
                "Outgoing signals for what the whole game cares about: ReadyChanged, Opened, Closed, "
                + "Failed, RevenuePaid.");

            painter.Space();
            painter.Note(
                "The module resolves no consent. A game in the EEA runs its consent flow - or MAX's own, "
                + "enabled in the Integration Manager - and one SetConsent call; until then the SDK runs "
                + "with its own default.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("The Root");
            painter.Bullet(
                "AdsServiceRoot into the scene the game boots from, Initialize Order -84, right behind "
                + "Analytics. CD_Ads ships filed on its adapter: InitializeOnLaunch (on), "
                + "InterstitialMinIntervalSeconds (0), ShowTimeoutSeconds (10).");

            painter.Space();
            painter.SubHeading("The plug");
            painter.Note(
                "A plug is an entry on AdsServiceRoot's Sub Context Types: Add Sub Context > "
                + "AppLovinMaxAdsServiceContext, or your own. Leave AutoSetup on - a Root runs a "
                + "sub-context's Setup only when the entry says so, and an unticked plug never plugs. With "
                + "no entry, or an unticked one, every show answers NotReady: no provider plugged, and "
                + "nothing else complains.");
            painter.Note(
                "The MAX plug compiles only when com.applovin.mediation.ads is in the manifest (AppLovin's "
                + "scoped registry). From the .unitypackage instead, add FLOWIOC_APPLOVIN_MAX to Scripting "
                + "Define Symbols. A missing define is a missing class, and the Root reports 'Context Type "
                + "couldn't find!' at play.");
            painter.Note(
                "Create > FlowIoC > AppLovinMaxAdsModule > Data > CD_AppLovinMaxAds, fill the four ad unit "
                + "ids from the AppLovin dashboard, and file the asset in AdsServiceRoot's Shared Scriptables. "
                + "Without it the plug reports at Setup and never initializes; the SDK key itself lives in "
                + "AppLovin's Integration Manager, where the module cannot see it.");

            painter.Space();
            painter.SubHeading("Order");
            painter.Paragraph(
                "The plug plugs in Setup; the service asks it to initialize in Launch when "
                + "InitializeOnLaunch is on, or when the game binds Commands.Initialize behind its consent "
                + "step; once the SDK reports ready, both formats load and the first show can follow.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("A rewarded ad");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.WatchAdForChest)\n"
                + "    .ToSequence<IAdsService.Commands.ShowRewarded>(ChestPlacements.CHEST)\n"
                + "    .ToSequence<GrantChestRewardCommand>();");
            painter.Note(
                "The rewarded step stops the sequence on every outcome but Rewarded - dismissed, not ready, "
                + "failed, skipped. Bind the grant after it, never before it, and put nothing after it that "
                + "must run regardless.");

            painter.Space();
            painter.SubHeading("An interstitial");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.LevelFinished)\n"
                + "    .ToSequence<SaveLevelResultCommand>()\n"
                + "    .ToSequence<IAdsService.Commands.ShowInterstitial>(LevelPlacements.LEVEL_END)\n"
                + "    .ToSequence<OpenLevelEndScreenCommand>();");
            painter.Paragraph(
                "The interstitial step releases on every outcome, the AdResultVO handed on: the level-end "
                + "flow goes on whether the ad showed, was skipped, or failed.");

            painter.Space();
            painter.SubHeading("Both paths");
            painter.Code(
                "public class WatchAdForChestCommand : Command\n"
                + "{\n"
                + "    [Inject] private IAdsService _ads { get; set; }\n"
                + "    [InjectSignal] private ChestInternalSignals _signals { get; set; }\n"
                + "\n"
                + "    public override void Execute()\n"
                + "    {\n"
                + "        Retain();\n"
                + "        _ads.ShowRewarded(ChestPlacements.CHEST, result =>\n"
                + "        {\n"
                + "            if (result.IsRewarded) _signals.GrantChest.Dispatch();\n"
                + "            else _signals.ShowNoAdPopup.Dispatch(result.Reason);\n"
                + "            Release();\n"
                + "        });\n"
                + "    }\n"
                + "}");

            painter.Space();
            painter.SubHeading("The button, the audio, the revenue");
            painter.Bullet("A watch-ad button reads IsReady(AdFormat.Rewarded) when it opens and follows Outgoing.ReadyChanged while it is open.");
            painter.Bullet("A Connector carries Outgoing.Opened and Closed to the audio module's mute and unmute.");
            painter.Bullet("A Connector carries Outgoing.RevenuePaid to the module that logs analytics, whose Command builds the ad_impression event from AdRevenueVO.");
            painter.Bullet("SetAdsRemoved(true) after the purchase and again at boot from the save; the module persists nothing.");
        }

        private void DrawProviders(HelpPainter painter)
        {
            painter.SubHeading("The socket");
            painter.Code(
                "public interface IAdsProvider\n"
                + "{\n"
                + "    string Name { get; }\n"
                + "    void Initialize(IAdsProviderListener listener);\n"
                + "    void Load(AdFormat format);\n"
                + "    bool IsReady(AdFormat format);\n"
                + "    void Show(AdFormat format, string placement);\n"
                + "    void SetConsent(AdsConsentVO consent);\n"
                + "    void SetMuted(bool muted);\n"
                + "    void ShowDebugger();\n"
                + "}\n"
                + "\n"
                + "public interface IAdsProviderListener\n"
                + "{\n"
                + "    void OnInitialized(bool ready);\n"
                + "    void OnLoaded(AdFormat format);\n"
                + "    void OnLoadFailed(AdFormat format, string error);\n"
                + "    void OnDisplayed(AdFormat format);\n"
                + "    void OnDisplayFailed(AdFormat format, string error);\n"
                + "    void OnClicked(AdFormat format);\n"
                + "    void OnRewardEarned(AdRewardVO reward);   // before OnClosed\n"
                + "    void OnClosed(AdFormat format);\n"
                + "    void OnRevenuePaid(AdRevenueVO revenue);\n"
                + "}");
            painter.Paragraph(
                "A provider reports on the main thread and decides nothing. RewardEarned comes before "
                + "Closed; a plug over an SDK that cannot promise the order holds its Closed until the "
                + "reward answer has arrived.");

            painter.Space();
            painter.SubHeading("Your own plug");
            painter.Code(
                "[AllowAsSubContext]\n"
                + "public class MyAdsServiceContext : Context\n"
                + "{\n"
                + "    private IAdsService _ads;\n"
                + "    private MyAdsProvider _provider;\n"
                + "\n"
                + "    public override void Setup()\n"
                + "    {\n"
                + "        base.Setup();\n"
                + "        _ads = InjectionBinderCrossContext.GetInstance<IAdsService>();\n"
                + "        _provider = new MyAdsProvider();\n"
                + "        _ads.Plug(_provider);\n"
                + "    }\n"
                + "\n"
                + "    public override void DestroyContext()\n"
                + "    {\n"
                + "        _ads?.Unplug(_provider);\n"
                + "        base.DestroyContext();\n"
                + "    }\n"
                + "}");
            painter.Paragraph(
                "The test module's FakeAdsProvider is the shape: a class behind IAdsProvider, a hosted "
                + "context that plugs it, listed on AdsServiceRoot.");

            painter.Space();
            painter.SubHeading("Shipped: AppLovin MAX");
            painter.Bullet("Assembly Modules.AppLovinMaxAds, define FLOWIOC_APPLOVIN_MAX from a version define on com.applovin.mediation.ads; references MaxSdk.Scripts.");
            painter.Bullet("CD_AppLovinMaxAds with the four ad unit ids, filed in AdsServiceRoot's Shared Scriptables.");
            painter.Bullet("Consent as SetHasUserConsent and SetDoNotSell; the age-restricted flag is the dashboard's in MAX 8.");
            painter.Bullet("In the Editor MAX shows its stub ads, so the whole callback path runs without a phone.");
        }
    }
}

#endif
