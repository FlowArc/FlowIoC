#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The analytics module: the socket, the two shipped plugs and their defines, and the three
    /// setup steps that fail silently when skipped.
    /// </summary>
    internal class AnalyticsModulePage : ModulePage
    {
        public override string ModuleFolderName => "AnalyticsModule";

        public override string Title => "Analytics";

        public override string Subtitle => "One Log call from anywhere, every plugged SDK behind it - Firebase, Facebook, or your own";

        public override FlowIcon Icon => FlowIcon.Broadcast;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "The Root in the boot scene, the SDKs with their defines, the plugs on the Root's Sub Context Types.",
                "Each step fails silently when skipped: with no plug listed an event goes nowhere and "
                + "only a plain log says so; with the define missing the plug is not compiled and the "
                + "Root reports a context it cannot find; with the SDK's own setup missing the SDK "
                + "reports what it will."),
            new HelpTab("Usage", DrawUsage,
                "Log from the module that decided the event - a bound step for a fixed one, a Command for one built from state.",
                "Nothing in the Connector. The flow reads from the module's Context, and the Connector "
                + "keeps carrying announcements to orders."),
            new HelpTab("Providers", DrawProviders,
                "The socket, the two shipped plugs, and your own in thirty lines.",
                "A provider sends what it is given, the way its SDK wants it, and decides nothing.")
        };

        public override string InstalledHint =>
            "Drop AnalyticsServiceRoot into your boot scene, add the plugs you have SDKs for to its "
            + "Sub Context Types, and log from a Command; the Setup tab has the steps.";

        public override string BodyHeadline =>
            "An event logged once reaches every SDK that is plugged in.";

        public override string BodyTagline =>
            "The level that started, the screen that opened, the purchase that went through - each is "
            + "one Log call in the module that decided it, and which SDKs receive it is a list on a Root.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "IAnalyticsService: Log an event of a name and parameters, set a user property, set the "
                + "user id, hand consent on. A Service, so any module references Modules.Analytics and "
                + "injects it.");
            painter.Bullet(
                "A socket: IAnalyticsProvider, one per SDK. The shipped Firebase and Facebook plugs "
                + "compile only when their SDK is in the project; a game's own plug is one class and "
                + "one hosted context.");
            painter.Bullet(
                "A queue per plug until its SDK reports ready - capped at 200, the oldest dropped and "
                + "the count reported - and the current consent, user id and properties applied at that "
                + "moment, consent first.");
            painter.Bullet(
                "Three steps a game binds: Commands.Log with a fixed event, Commands.SetUserProperty, "
                + "Commands.SetConsent after the step that resolved it.");
            painter.Bullet(
                "Every event, plug and state change on the module's channel, so a flow reads in the Flow "
                + "Console with no SDK at all.");

            painter.Space();
            painter.Note(
                "The module resolves no consent. A game in the EEA needs a consent flow of its own and "
                + "one SetConsent call; until then each SDK runs with its own default.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("The Root");
            painter.Bullet("AnalyticsServiceRoot into the scene the game boots from, Initialize Order -85. No adapter, no asset.");

            painter.Space();
            painter.SubHeading("The plugs");
            painter.Note(
                "A plug is an entry on AnalyticsServiceRoot's Sub Context Types: Add Sub Context > "
                + "FirebaseAnalyticsServiceContext, FacebookAnalyticsServiceContext, or your own, with "
                + "AutoSetup ticked on the entry - a Root runs a sub-context's Setup only when the entry "
                + "says so, and an unticked plug never plugs. With no entry, or an unticked one, nothing "
                + "is plugged; the first event logs 'no provider plugged' and nothing else complains.");
            painter.Note(
                "The Firebase plug compiles only when com.google.firebase.analytics is in the manifest "
                + "(the SDK's tgz). From the .unitypackage instead, add FLOWIOC_FIREBASE_ANALYTICS to "
                + "Scripting Define Symbols. The Facebook SDK is always a .unitypackage: add "
                + "FLOWIOC_FACEBOOK_SDK. A missing define is a missing class, and the Root reports "
                + "'Context Type couldn't find!' at play.");
            painter.Note(
                "The SDKs' own setup is theirs: google-services.json and GoogleService-Info.plist for "
                + "Firebase, the App ID in FacebookSettings for Facebook. The module cannot see whether "
                + "they are there; the SDK reports what it will at initialize.");

            painter.Space();
            painter.SubHeading("Order");
            painter.Paragraph(
                "Plugs plug in Setup; the service asks them to initialize in Launch; events logged from "
                + "any Launch on queue until the SDK answers. A plug added later - a game's own, from a "
                + "later Root - is initialized at once and sees events from then on.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("A fixed event");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.OpenSettings)\n"
                + "    .ToSequence<OpenSettingsScreenCommand>()\n"
                + "    .ToSequence<IAnalyticsService.Commands.Log>(new AnalyticsEventVO(\"settings_opened\"));",
                "In the module that owns the screen. The flow reads there: open, then log.");

            painter.Space();
            painter.SubHeading("An event built from state");
            painter.Code(
                "[Inject] private IAnalyticsService _analytics { get; set; }\n"
                + "[Inject] private IMatchModel       _match     { get; set; }\n\n"
                + "public override void Execute() =>\n"
                + "    _analytics.Log(new AnalyticsEventVO(\"level_start\")\n"
                + "        .With(\"level\", _match.Level)\n"
                + "        .With(\"mode\", _match.Mode.ToString())\n"
                + "        .With(\"attempt\", _match.Attempt));",
                "A Command of the game's own, bound after the step that decided it. Names as constants beside it.");

            painter.Space();
            painter.SubHeading("Who the player is");
            painter.Code(
                "_analytics.SetUserId(profile.Id);\n"
                + "_analytics.SetUserProperty(\"ab_group\", group);\n\n"
                + ".ToSequence<ResolveConsentCommand>()\n"
                + ".ToSequence<IAnalyticsService.Commands.SetConsent>()",
                "Current values: a plug that comes up later receives them at its flush, consent first.");
        }

        private void DrawProviders(HelpPainter painter)
        {
            painter.SubHeading("The socket");
            painter.Code(
                "public interface IAnalyticsProvider\n"
                + "{\n"
                + "    string Name { get; }\n"
                + "    void Initialize(Action<bool> ready);   // report once, on the main thread\n"
                + "    void Log(AnalyticsEventVO analyticsEvent);\n"
                + "    void SetUserProperty(string name, string value);\n"
                + "    void SetUserId(string userId);\n"
                + "    void SetConsent(AnalyticsConsentVO consent);\n"
                + "}",
                "Until ready reports, the service queues for the plug; a false report drops the queue.");

            painter.Space();
            painter.SubHeading("Your own plug");
            painter.Code(
                "[AllowAsSubContext]\n"
                + "public class AdjustAnalyticsServiceContext : Context\n"
                + "{\n"
                + "    private IAnalyticsService _analytics;\n"
                + "    private AdjustAnalyticsProvider _provider;\n\n"
                + "    public override void Setup()\n"
                + "    {\n"
                + "        base.Setup();\n"
                + "        _analytics = InjectionBinderCrossContext.GetInstance<IAnalyticsService>();\n"
                + "        _provider  = new AdjustAnalyticsProvider();\n"
                + "        _analytics.Plug(_provider);\n"
                + "    }\n\n"
                + "    public override void DestroyContext()\n"
                + "    {\n"
                + "        _analytics?.Unplug(_provider);\n"
                + "        base.DestroyContext();\n"
                + "    }\n"
                + "}",
                "One class behind the socket, one hosted context that plugs it, listed on AnalyticsServiceRoot. "
                + "The test module's RecordingAnalyticsProvider is the worked example.");

            painter.Space();
            painter.SubHeading("The shipped plugs");
            painter.Table(new[] {"Plug", "Assembly", "Define", "Set by"},
                new[]
                {
                    "Firebase Analytics", "Modules.FirebaseAnalytics", "FLOWIOC_FIREBASE_ANALYTICS",
                    "a version define on com.google.firebase.analytics; by hand for the .unitypackage"
                },
                new[] {"Facebook App Events", "Modules.FacebookAnalytics", "FLOWIOC_FACEBOOK_SDK", "by hand in Scripting Define Symbols"});
            painter.Paragraph(
                "Both cut an event to 40 / 40 / 100 / 25 - name, parameter name, string value, parameter "
                + "count - and warn with the event's name when they had to, so the name gets fixed rather "
                + "than the SDK silently rejecting it. Bool travels as 1 / 0. Facebook has no user "
                + "properties and says so once.");
        }
    }
}

#endif