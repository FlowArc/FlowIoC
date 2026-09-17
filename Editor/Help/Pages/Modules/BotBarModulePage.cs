#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The bot bar module: the config that drives it, the keyed signal a game wires, and the
    /// setup steps that fail silently when skipped.
    /// </summary>
    internal class BotBarModulePage : ModulePage
    {
        public override string ModuleFolderName => "BotBarModule";

        public override string Title => "BotBar Module";

        public override string Subtitle => "The hub's bottom tab bar, generic: tabs and behaviour from one config, wired by key";

        public override FlowIcon Icon => FlowIcon.Windows;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "The Root in the hub scene, the config, the game's Connector.",
                "Each step fails silently when skipped: a bar on a layer below the pages is covered; a key "
                + "in the Connector that no tab carries does nothing - the Launch warning names it."),
            new HelpTab("Config", DrawConfig,
                "Every tab and every switch, in CD_BotBar.",
                "An option that is off does not animate at all.")
        };

        public override string InstalledHint =>
            "Drop BotBarSystemRoot into your hub scene, edit CD_BotBar, and wire Selected(key) per tab in "
            + "your ConnectorModule; the Setup tab has the steps.";

        public override string BodyHeadline =>
            "Five tabs or six, the bar adjusts; the game wires each by its key.";

        public override string BodyTagline =>
            "The module knows no Shop and no Clan: the keys are in your config, and Outgoing.Selected(\"shop\") "
            + "is the edge your Connector joins to your Shop screen.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "A System with its own screen: tabs from CD_BotBar, the selected one scaled, raised and "
                + "widened by the options you turn on, a highlight, badges, locked and NEW marks, a slide "
                + "off the bottom edge.");
            painter.Bullet("Incoming: Open, SelectTab(key), SetLocked(key, locked), SetBadge(key, count), Show, Hide.");
            painter.Bullet(
                "Outgoing: Selected(key) - one signal per key, created when asked - SelectionChanged(previous, "
                + "current), TabStateChanged, BadgeChanged, Shown, Hidden, LockedTabTapped(key).");
            painter.Bullet("No unlock policy inside: which tab is locked is your decision, announced to SetLocked.");
            painter.Bullet(
                "Install once: a bar is a starter you rewrite - the prefab, the icons, the tab list - so it "
                + "carries no version and the Module Library offers no update.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("1. The Root");
            painter.Bullet(
                "BotBarSystemRoot into the hub scene, in the 0 - 97 band (shipped at 2; its child "
                + "BotBarConnectorRoot at 3). CD_BotBar and RD_BotBar ship filed in its Shared Scriptables.");

            painter.SubHeading("2. Your Connector");
            painter.Paragraph(
                "In your ConnectorModule, a sub-context named after the bar. Each tab is one line, by the "
                + "key your config gives it; the module never holds the name Shop.");
            painter.Code(
                "public class BotBarConnectorSubContext : Context\n"
                + "{\n"
                + "    private const string GROUP = nameof(BotBarConnectorSubContext);\n"
                + "\n"
                + "    private BotBarSignals _botBarSignals;\n"
                + "    private MainSignals _mainSignals;\n"
                + "    private ShopScreenSignals _shopScreenSignals;\n"
                + "    private ClanScreenSignals _clanScreenSignals;\n"
                + "    private ProfileSignals _profileSignals;\n"
                + "\n"
                + "    public override void Setup()\n"
                + "    {\n"
                + "        base.Setup();\n"
                + "\n"
                + "        _botBarSignals = InjectionBinderCrossContext.GetInstance<BotBarSignals>();\n"
                + "        _mainSignals = InjectionBinderCrossContext.GetInstance<MainSignals>();\n"
                + "        _shopScreenSignals = InjectionBinderCrossContext.GetInstance<ShopScreenSignals>();\n"
                + "        _clanScreenSignals = InjectionBinderCrossContext.GetInstance<ClanScreenSignals>();\n"
                + "        _profileSignals = InjectionBinderCrossContext.GetInstance<ProfileSignals>();\n"
                + "\n"
                + "        _mainSignals.Outgoing.Started.Connect(_botBarSignals.Incoming.Open, GROUP);\n"
                + "        _botBarSignals.Outgoing.Selected(\"shop\").Connect(_shopScreenSignals.Incoming.Open, GROUP);\n"
                + "        _botBarSignals.Outgoing.Selected(\"clan\").Connect(_clanScreenSignals.Incoming.Open, GROUP);\n"
                + "        _profileSignals.Outgoing.FeatureLockChanged.Connect(_botBarSignals.Incoming.SetLocked, GROUP);\n"
                + "    }\n"
                + "\n"
                + "    public override void DestroyContext()\n"
                + "    {\n"
                + "        SignalConnector.DisconnectGroup(GROUP);\n"
                + "        base.DestroyContext();\n"
                + "    }\n"
                + "}");
            painter.Note(
                "The keys are yours: whatever CD_BotBar says. A key in a Connector that no tab carries is "
                + "named by a warning at Launch, and a tab with no Connector line does nothing when tapped.");

            painter.SubHeading("3. The layer");
            painter.Note(
                "The bar's screen sits on layer 2; your pages share layer 0 and open with "
                + "ForceOpenAtFullLayer. A bar on a layer below the pages is covered with nothing logged - "
                + "override the layer on the Root's entry with Override Screen if your layout differs.");
        }

        private void DrawConfig(HelpPainter painter)
        {
            painter.SubHeading("Tabs");
            painter.Bullet(
                "Key - the identity a Connector names; Title - what shows; IdleIcon, SelectedIcon; "
                + "LockedLabel - shown while locked, empty shows nothing. Five entries make five tabs, six make six.");
            painter.Bullet("StartTab - the key selected when the bar opens; Open puts the bar back on it every time.");

            painter.SubHeading("Options");
            painter.Bullet(
                "ScaleSelected + SelectedScale, RaiseSelected + SelectedRaise, WidenSelected + SelectedWidth "
                + "(a flexible width against 1; the layout slides the neighbours), Duration and Curve, MoveHighlight.");
            painter.Bullet(
                "Titles: Always, SelectedOnly, Never. NewMarkOnUnlock: a NEW mark on a tab that just unlocked, "
                + "until its first tap. HideDuration and HideCurve: the slide off and on.");

            painter.SubHeading("Locks and badges");
            painter.Bullet(
                "SetLocked(key, locked) from your own decision - a level reached, a purchase made. A locked "
                + "tab stays tappable and answers with LockedTabTapped(key), for your toast.");
            painter.Bullet("SetBadge(key, count) shows a count on the tab; 0 clears it. Nothing is persisted.");
        }
    }
}

#endif
