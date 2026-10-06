#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The resource fly module: icons that fly from a source into a counter while the counter
    /// shows the saved value less what is still on its way, the three steps a game binds, and the
    /// motions an icon can play.
    /// </summary>
    internal class ResourceFlyModulePage : ModulePage
    {
        private readonly HelpImages _images = new HelpImages();

        public override string ModuleFolderName => "ResourceFlyModule";

        public override string Title => "Resource Fly Module";

        public override string Subtitle => "Coins, gems and stars that fly into their counter, and a counter that waits for them";

        public override FlowIcon Icon => FlowIcon.Wand;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "Put the Root in the scene, put a counter on a screen, and file the icons' pool group.",
                "The Root carries CD_ResourceFly, whose default motion is Scatter. A counter is a "
                + "ResourceFlyCounterDisplay on a screen, registered by the Command that opens it, and "
                + "its icons come from a pool group a Root in the scene files."),
            new HelpTab("Usage", DrawUsage,
                "Bind Reserve, then the save, then Fly - or call the service from a Command.",
                "A grant is reserved before the save that announces it, so the counter stays where it "
                + "was until the icons land. The Fly step holds its sequence until the last one is down."),
            new HelpTab("Motions", DrawMotions,
                "Scatter, Direct and Curved ship. A motion of your own is one class and one asset.",
                "A motion only says where an icon is at a moment. The icon plays it and reports when it "
                + "landed, so a motion you write cannot leave a flight unfinished.")
        };

        public override string InstalledHint =>
            "Drop ResourceFlyServiceRoot into your scene, put a ResourceFlyCounterDisplay on a screen and bind "
            + "the Reserve and Fly steps; the Setup tab has the steps.";

        public override string BodyHeadline =>
            "A reward flies into its counter, and the counter waits for it.";

        public override string BodyTagline =>
            "The coins of a win leave the reward on screen, fly into the top bar and count up as they "
            + "land - while the save already holds the new total, and a quit loses nothing.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("ResourceFlyTestScene.png"),
                "ResourceFlyTestScene in play: one lane per ready-made motion, each flight on its way to its counter.");

            painter.Separator();
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "A counter per resource key - Coin, Gem, Star - that shows the saved value less what "
                + "is still pending. The saved value is kept only as last told and the shown one is "
                + "worked out from it, so the two cannot drift apart.");
            painter.Bullet(
                "Pooled icons that leave a named source, play a motion and land on the counter, which "
                + "counts up to each landing's value and punches its icon. Everything runs on unscaled "
                + "time, so a paused game still pays out.");
            painter.Bullet(
                "Three motions - Scatter, Direct and Curved - chosen per counter, with CD_ResourceFly's "
                + "as the default, and room for a motion of your own.");
            painter.Bullet(
                "Three steps a game binds in its own sequence: IResourceFlyService.Commands.Reserve, "
                + "SetValue and Fly. Fly holds the sequence until the last icon is down.");
            painter.Bullet(
                "A flight that cannot play settles at once rather than hanging: no counter, no source, "
                + "no motion, no pool item, an icon lost on the way - its screen hidden or destroyed "
                + "under it. The setup mistakes among those are logged, and the counter shows the "
                + "saved value.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.ResourceFly and injects "
                + "IResourceFlyService directly. It plays no sound and no haptic: the counter raises "
                + "FlightStarted, Landed and FlightEnded, and the screen that hosts it binds what "
                + "should play.");

            painter.Separator();
            painter.SubHeading("How a grant reaches the counter");
            painter.Paragraph(
                "The grant is reserved first, so pending rises. The save follows and the counter is "
                + "told the new saved value - and still shows the old one, because the difference is "
                + "pending. Fly then flies up to that much from the source; each icon carries a share, "
                + "and as it lands its share stops being pending and the counter counts up. A second "
                + "Fly while the first is out flies only what is not already on its way.");

            painter.Separator();
            painter.SubHeading("Trying it out");
            painter.Paragraph(
                "The module ships with a test module beside it, and the scene it runs in arrives with "
                + "it. Open ResourceFlyTestScene under the test module's Scenes folder and press Play: "
                + "three lanes, one per motion, each with a counter, a source and a Fly button. A press "
                + "runs Reserve, the save and Fly in the order a game binds them, and the Flow Console "
                + "reads Reserve - <key> and Fly - <key> on the ResourceFlyModule channel.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("1. The Root");
            painter.Paragraph(
                "Drop ResourceFlyServiceRoot from the module's Prefabs folder into the scene. It ships "
                + "at Initialize Order -45, in the Service band, after the pool service its icons come "
                + "from.");
            painter.Image(_images.Get("ResourceFlyRootHierarchy.png"),
                "ResourceFlyServiceRoot in MainScene, in the Services band after PoolServiceRoot.");
            painter.Paragraph(
                "Its adapter files CD_ResourceFly: the default motion, the most icons one flight uses, "
                + "the gap between icons, and how the counter punches and counts up.");
            painter.Image(_images.Get("ResourceFlyRootAdapter.png"),
                "The Root's adapter with CD_ResourceFly in its slot.");

            painter.Separator();
            painter.SubHeading("2. A counter on a screen");
            painter.Paragraph(
                "Put a ResourceFlyCounterDisplay beside the counter's icon on the screen that shows the "
                + "resource. Target is the icon the flight lands on, Count the text that counts up, Icon "
                + "Parent what the icons fly in, Icon Pool Key the pool item they are, and Motion the "
                + "motion flights into this counter play - empty plays CD_ResourceFly's.");
            painter.Image(_images.Get("ResourceFlyCounterDisplay.png"),
                "A ResourceFlyCounterDisplay with the Curved motion in its Motion slot.");
            painter.Note(
                "Important: Icon Parent is the sibling just before Target in the hierarchy, so the "
                + "target and its punch draw over the icons that land on it. Placed after it, the icons "
                + "cover the counter as they arrive, and nothing reports it.");
            painter.Paragraph(
                "The Command that opens the screen registers the counter and the screen's source with "
                + "the service, from the view Show returns. The Command that closes it unregisters "
                + "them, passing the counter, so a counter that took the key since - a popup over the "
                + "top bar - keeps it.");
            painter.Code(
                "_resourceFly.RegisterCounter(\"Coin\", screen.CoinCounter);\n"
                + "_resourceFly.SetValue(\"Coin\", player.Coin);\n"
                + "_resourceFly.RegisterSource(\"WinReward\", screen.RewardSource);");

            painter.Separator();
            painter.SubHeading("3. The icons' pool group");
            painter.Paragraph(
                "The icons are a prefab with a ResourceFlyIcon on it, listed in a pool group under the "
                + "key the counter names. The module that owns the screen files the group on its Root - "
                + "a PoolSubContext entry, the way the test module files Pool_ResourceFlySample.");
            painter.Note(
                "Important: without the group in the scene every flight logs an error and settles at "
                + "once - the counter jumps to the saved value and no icon is seen.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("Steps in a sequence");
            painter.Paragraph(
                "Where a grant is part of a flow, bind the module's steps with the key or the route "
                + "they work on; the amount comes on the signal. Reserve goes before the save, so the "
                + "announcement leaves the counter where it was, and Fly holds the sequence until the "
                + "last icon is down.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.CompleteLevel)\n"
                + "    .ToSequence<IResourceFlyService.Commands.Reserve>(\"Coin\")\n"
                + "    .ToSequence<GrantCoinsCommand>()\n"
                + "    .ToSequence<SavePlayerCommand>();\n"
                + "\n"
                + "CommandBinder.Bind(_signals.Incoming.CollectReward)\n"
                + "    .ToSequence<IResourceFlyService.Commands.Fly>(new ResourceFlyRouteVO(\"WinReward\", \"Coin\"))\n"
                + "    .ToSequence<CloseWinScreenCommand>();");
            painter.Paragraph(
                "Reserve, SetValue and Fly each read an int off the signal. A counter that should follow "
                + "the saved value binds SetValue to the signal that announces it.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.SetCoins).ToSequence<IResourceFlyService.Commands.SetValue>(\"Coin\");");

            painter.Separator();
            painter.SubHeading("A call from a Command");
            painter.Paragraph(
                "Where the amount or the route is part of a decision, inject the service and call it. "
                + "Fly takes a callback for when the last icon is down; a Command that waits on it "
                + "retains its step and releases in the callback.");
            painter.Code(
                "[Inject] private IResourceFlyService _resourceFly { get; set; }\n"
                + "\n"
                + "public override void Execute()\n"
                + "{\n"
                + "    Retain();\n"
                + "    _resourceFly.Fly(new ResourceFlyRouteVO(\"Chest\", \"Gem\"), _amount, () => Release());\n"
                + "}");

            painter.Separator();
            painter.SubHeading("The counter's events");
            painter.Paragraph(
                "FlightStarted comes when the first flight into a counter starts, Landed with every "
                + "icon, and FlightEnded once the last one is down and the count has come to rest - a "
                + "top bar that slides in for the flight slides out on it. A flight starting before "
                + "FlightEnded continues the same stretch, and the end goes to the counter the flight "
                + "began on. A listener that throws is logged and the icons land anyway.");
        }

        private void DrawMotions(HelpPainter painter)
        {
            painter.SubHeading("The ready-made motions");
            painter.Table(new[] {"Motion", "What it does", "Settings"},
                new[]
                {
                    "Scatter", "Bursts out around the source, then gathers into the counter.",
                    "Scatter radius, seconds and curve; gather seconds and curve"
                },
                new[] {"Direct", "Flies straight from the source to the counter.", "Seconds and curve"},
                new[] {"Curved", "Arcs to the counter, every icon bending to a side of its own.", "Seconds, curve and bend"});
            painter.Paragraph(
                "Each is a CD_ResourceFlyMotion asset in the module's Scriptables folder. Put one in "
                + "CD_ResourceFly for every counter, or in a counter's Motion slot for that counter "
                + "alone. Make another asset of the same class from the Create menu to tune it.");

            painter.Separator();
            painter.SubHeading("A motion of your own");
            painter.Paragraph(
                "Derive from CD_ResourceFlyMotion, say how long a flight takes and where the icon is at "
                + "t, from 0 at the source to 1 on the target. The path hands you where the icon left "
                + "from, where the target is this frame, a random point fixed for the icon's flight, "
                + "and the canvas scale.");
            painter.Code(
                "[CreateAssetMenu(menuName = \"Game/Data/CD_ResourceFlyMotionHop\")]\n"
                + "public class CD_ResourceFlyMotionHop : CD_ResourceFlyMotion\n"
                + "{\n"
                + "    public float FlySeconds = 0.5f;\n"
                + "    public float Height = 200f;\n"
                + "\n"
                + "    public override float Seconds => FlySeconds;\n"
                + "\n"
                + "    public override Vector3 Evaluate(in ResourceFlyPathVO path, float t) =>\n"
                + "        Vector3.Lerp(path.From, path.To, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * Height * path.Scale);\n"
                + "}");
            painter.Note(
                "Keep a motion stateless: it is asked every frame, for every icon in flight, and two "
                + "counters may share one asset. Evaluate(0) should be the source and Evaluate(1) the "
                + "target, or the icon jumps as it starts or lands.");
        }
    }
}

#endif