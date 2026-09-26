#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Features
{
    internal class PoolServicePage : HelpPage
    {
        // The marks the Module Scanner wears for a pass and a failure, in rich text for a table cell.
        private const string Yes = "<color=#5CB85C>✔</color>";
        private const string No = "<color=#E05252>✖</color>";

        private readonly HelpImages _images = new HelpImages();

        public PoolServicePage() : base(null)
        {
        }

        public override string Title => "Pool Service";

        public override FlowIcon Icon => FlowIcon.Recycle;

        protected override string BodyTabTitle => "Setup";

        protected override string BodyHeadline => "Reusable objects, registered by the module that spawns them.";

        protected override string BodyTagline =>
            "Bullets, damage numbers, world labels: objects a game makes over and over are kept alive "
            + "between uses rather than instantiated and destroyed. One service keeps them; each module "
            + "says which ones it uses, on its own Root.";

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Config", DrawConfig, "A group asset, and an entry on the Root that uses it.",
                "What the pool holds is written in a CD_PoolGroup; which groups a module uses is written "
                + "on that module's Root."),
            new HelpTab("Usage", DrawUsage, "Ask for an object, give it back.",
                "From a Command or a System: Get hands an object out, Dismiss or Return sends it home, "
                + "and a sequence step fills a group before it is needed.")
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("The service, once per scene");
            painter.Paragraph(
                "PoolServiceRoot binds IPoolService and sits with the other services at the top of "
                + "the scene. Nothing is configured on it: it is dropped in once and left alone.");
            painter.Image(_images.Get("MainSceneHierarchy.png"),
                "MainScene: PoolServiceRoot among the services, above the game's own Roots.");

            painter.SubHeading("The module that spawns, registers");
            painter.Paragraph(
                "The pools a module uses are an entry on that module's own Root: Add Sub Context, "
                + "then PoolSubContext. The entry registers its groups in Setup, which runs once every "
                + "Root has bound, so a Launch step that fills a group finds it configured.");
            painter.Note(
                "A Root never goes under another Root. The groups are an entry on the module's own "
                + "Root, not a child Root beside its objects, and PoolServiceRoot itself is never edited.");
        }

        private void DrawConfig(HelpPainter painter)
        {
            painter.SubHeading("A group of pooled objects");
            painter.Paragraph(
                "A CD_PoolGroup asset lists the objects one use of the pool needs: a key to ask for "
                + "each by, the prefab, how many to make up front, and whether the pool may grow past "
                + "that. Group by lifetime - what is warmed and returned together - rather than by what "
                + "the objects look like.");
            painter.Image(_images.Get("PoolGroupInspector.png"),
                "Pool_PointerSample, the group the World Pointer module's sample draws its "
                + "labels from.");

            painter.Separator();

            painter.SubHeading("The groups on the Root");
            painter.Paragraph(
                "The PoolSubContext entry's Groups pair a group key on the left with a CD_PoolGroup on "
                + "the right. The key is what InitializeGroup and Return.Group take.");
            painter.Image(_images.Get("PoolSubContextInspector.png"),
                "WorldPointerTestRoot registers the sample's group. The POOL badge marks the entry; "
                + "folded, it still says how many groups it holds.");
            painter.Bullet(
                "Auto Initialize creates the group's objects as soon as it is registered. Off, the "
                + "group waits until something fills it.");
            painter.Bullet(
                "Group Specific Pools keeps this group's pools apart from another group that uses the "
                + "same keys - see below.");
            painter.Bullet(
                "Unregister When Root Destroyed takes the groups down with the Root - for a scene "
                + "loaded on top of another, whose pools should leave with it.");

            painter.Separator();

            painter.SubHeading("The same keys in every group: Group Specific Pools");
            painter.Paragraph(
                "Some groups are meant to hold the same keys. A game with themes gives every theme a "
                + "group of its own - Pool_Forest, Pool_Beach - and every one lists a "
                + "Gate and an Exit, each theme with its own prefab. Off, two groups with one key share "
                + "one pool: the group registered last overwrites the other, and Get(\"Gate\") hands out "
                + "whichever theme won.");
            painter.Table(new[] {"Group Specific Pools", "Pools made", "Asked for with", "Answers"},
                new[] {"Off", "Gate", "Get(\"Gate\")", $"{Yes} the Gate of whichever group registered last"},
                new[] {"On", "Forest_Gate, Beach_Gate", "Get(\"Forest_Gate\")", $"{Yes} the Forest theme's Gate"},
                new[] {"On", "Forest_Gate, Beach_Gate", "Get(\"Gate\")", $"{No} nothing - no pool has that name"});
            painter.Paragraph(
                "On, each item's pool is named after its group - Forest_Gate, Beach_Gate - so every "
                + "theme keeps its own. The code that spawns works out the group from the theme and asks "
                + "with both halves, and one path serves every theme:");
            painter.Code(
                "string group = _themeModel.Name;   // \"Forest\", \"Beach\"\n"
                + "\n"
                + "var gate = _poolService.Get<GateView>($\"{group}_Gate\", _boardParent);",
                "With Group Specific Pools on, the key is <group key>_<pool key>");
            painter.Note(
                "With it on, Get(\"Gate\") finds nothing: the pool is Forest_Gate. Leave it off when the "
                + "keys are yours to choose, and keep them apart by name instead - gameplay.bullet, "
                + "menu.bullet.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("Asking for an object, and giving it back");
            painter.Code(
                "[Inject] private IPoolService _poolService { get; set; }\n"
                + "\n"
                + "var label = _poolService.Get<SampleLabelIndicator>(\"SampleLabel\", _labelsParent);\n"
                + "\n"
                + "label.Dismiss();                          // the object goes home by itself\n"
                + "_poolService.Return.Group(\"PointerSample\"); // or everything in a group at once",
                "Get parents and activates the object; Dismiss and Return send it back");
            painter.Paragraph(
                "An object handed out has been used before: reset it in OnGetFromPool, not in Awake, "
                + "which runs once per instance. Never Destroy a pooled object - the pool still counts "
                + "it and hands the dead reference out later.");

            painter.Separator();

            painter.SubHeading("Filled while the game boots");
            painter.Paragraph(
                "Filling a group is a step a sequence binds, so it can run under the loading bar. The "
                + "step holds the sequence until the pools are full:");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.EnterMatch)\n"
                + "    .ToSequence<IPoolService.Commands.InitializeGroup>(\"Combat\")\n"
                + "    .ToSequence<SignalDispatchCommand>(_signals.Outgoing.MatchReady);",
                "The group is named where the step is bound");
            painter.Paragraph(
                "The setup set's Main module fills the groups MainConstants.BootPoolGroups names as a "
                + "step of its boot, reported on the loading bar.");
            painter.PageLink("Ordering Roots", "Read: Ordering Roots");
        }
    }
}

#endif
