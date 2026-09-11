#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages
{
    internal class DataTypesPage : HelpPage
    {
        public DataTypesPage() : base(null)
        {
        }

        public override string Title => "Data Types";

        public override FlowIcon Icon => FlowIcon.Braces;

        /// <summary>
        /// Exposed for the same reason the folder tree is: a test can check the names here against
        /// the prefixes and suffixes the shipped code style declares legal.
        /// </summary>
        public HelpTreeNode Root { get; } = new HelpTreeNode("Data", "everything a module stores",
            new HelpTreeNode("UnityObjects", "the ScriptableObject assets",
                new HelpTreeNode("CD_Maps", "config data - authored in the Editor, constant at runtime"),
                new HelpTreeNode("RD_MapPool", "runtime data - produced by play, gone when it stops"),
                new HelpTreeNode("PD_Maps", "player data - loaded at startup, saved again on every change"),
                new HelpTreeNode("ED_MapTools", "editor data - only editor tooling reads it"),
                new HelpTreeNode("DD_Maps", "database data - a copy of what a backend owns")),
            new HelpTreeNode("ValueObjects", "the plain classes those assets are built out of",
                new HelpTreeNode("MapVO", "belongs to no one asset - a payload, a return shape"),
                new HelpTreeNode("MapCVO", "what CD_Maps holds"),
                new HelpTreeNode("MapRVO", "what RD_MapPool holds"),
                new HelpTreeNode("MapPVO", "what PD_Maps holds"),
                new HelpTreeNode("MapEVO", "what ED_MapTools holds"),
                new HelpTreeNode("MapDVO", "what DD_Maps holds")));

        private readonly HelpImages _images = new HelpImages();

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Root Adapter", DrawRootAdapter,
                "One component beside the Root hands the module what the scene holds for it.",
                "Assets by name in two slots, scene components by name in two more. The module's own "
                + "slots are read off the adapter by its Model; the Shared ones reach every module "
                + "through ISharedDataModel."),
            new HelpTab("Rules", DrawRules,
                "The rules, in one list.",
                "Which prefix a type takes, and which of the two folders it belongs in.")
        };

        protected override string BodyHeadline => "A name says which kind of data it is before the file is opened.";

        protected override string BodyTagline =>
            "Data/UnityObjects holds the ScriptableObject assets and Data/ValueObjects the plain "
            + "[Serializable] classes they are built out of. The prefix says where the contents "
            + "come from and the suffix matches it, so a name tells you what is safe to regenerate "
            + "and what has to survive a restart.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Space();
            painter.Tree(Root);

            painter.SubHeading("The five kinds");
            painter.Bullet("CD_ is config data. A designer types it in and the game only ever reads it.");
            painter.Bullet("RD_ is runtime data. Play produces it, and nothing keeps it once play stops.");
            painter.Bullet("PD_ is player data. It is loaded at startup and written back to the save system whenever it changes.");
            painter.Bullet("ED_ is editor data. Settings and caches that only editor tooling reads; nothing in a build touches it.");
            painter.Bullet("DD_ is database data. A local copy of something a backend owns, filled by a download.");

            painter.Space();
            painter.Note(
                "A plain VO suffix is the right name when the data belongs to no one asset in "
                + "particular - a payload passed between commands, the shape a Function returns.");

            painter.Separator();
            painter.SubHeading("A family of your own");
            painter.Paragraph(
                "The five are what FlowIoC ships, not the whole vocabulary. A project that needs "
                + "another kind adds a prefix and its matching suffix, and declares both in the "
                + "solution code style so the IDE stops flagging the name.");

            painter.Separator();
            painter.SubHeading("Where an asset is reached from");
            painter.Paragraph(
                "An asset gets to the code through the RootAdapter on the module's Root - its own "
                + "assets from one slot, the ones other modules read from another. The Root Adapter "
                + "tab walks through the four slots, with the Model and the Command that read each.");
        }

        private void DrawRootAdapter(HelpPainter painter)
        {
            painter.SubHeading("What the adapter is");
            painter.Paragraph(
                "One component beside the Root, with four maps. Each files something by name - an "
                + "asset or a scene component - and the name is usually the type name, so the "
                + "parameterless overloads find it. Two of the maps are the module's own; the two "
                + "Shared ones reach every module in the scene through ISharedDataModel.");
            painter.Image(_images.Get("RootAdapterInspector.png"),
                "AbTestFlowServiceRoot's adapter: the config it reads in Scriptable Map, the status it "
                + "publishes in Shared Scriptable Map. The mono maps hold scene components the same way.");

            painter.Separator();
            painter.SubHeading("Scriptable Map - the module's own assets");
            painter.Paragraph(
                "The Model reads them. It is handed the Root's GameObject by the context's name, "
                + "takes the adapter off it, and asks for each asset by type - in PostConstruct, "
                + "so the data is there before any Command runs. A miss is an error naming the "
                + "asset and the Root, and the Model only has to notice the null.");
            painter.Code(
                "public class MatchModel : IMatchModel, IConstructable\n"
                + "{\n"
                + "    [Inject(nameof(MatchContext))] private GameObject _root { get; set; }\n"
                + "\n"
                + "    private CD_MatchRules _rules;\n"
                + "    private RD_Match      _match;\n"
                + "\n"
                + "    public MatchRulesCVO Rules => _rules.Rules;\n"
                + "    public RD_Match      Match => _match;\n"
                + "\n"
                + "    public void PostConstruct()\n"
                + "    {\n"
                + "        RootAdapter adapter = _root.GetComponent<RootAdapter>();\n"
                + "\n"
                + "        _rules = adapter.GetScriptable<CD_MatchRules>();             // filed as CD_MatchRules\n"
                + "        _match = adapter.GetScriptable<RD_Match>(\"RD_Match_Ranked\"); // filed under a name\n"
                + "    }\n"
                + "}",
                "The Model reads the adapter once and owns what it found.");
            painter.Paragraph(
                "A Command never reaches for the adapter. It injects the Model and asks it - the "
                + "Model owns the module's data, and the Command is a step that uses it:");
            painter.Code(
                "public class StartMatchCommand : Command\n"
                + "{\n"
                + "    [Inject]       private IMatchModel  _matchModel { get; set; }\n"
                + "    [InjectSignal] private MatchSignals _signals    { get; set; }\n"
                + "\n"
                + "    public override void Execute()\n"
                + "    {\n"
                + "        _matchModel.Begin(_matchModel.Rules.RoundSeconds);\n"
                + "        _signals.Outgoing.MatchStarted.Dispatch();\n"
                + "    }\n"
                + "}");

            painter.Separator();
            painter.SubHeading("Shared Scriptable Map - the assets other modules read");
            painter.Paragraph(
                "The Shared assembly settles the type; the instance is filed once. A ScriptableObject "
                + "other modules read goes in this slot on one Root, and any injectable in any module "
                + "reads it through ISharedDataModel - the same two overloads the adapter has. The "
                + "slot says the asset is common, not who produces it: a test Root files a ready-made "
                + "RD_ asset here when the producer is not in the scene, and the reader cannot tell.");
            painter.Code(
                "public class ShowMatchResultCommand : Command\n"
                + "{\n"
                + "    [Inject] private ISharedDataModel _sharedDataModel { get; set; }\n"
                + "    [Inject] private IHudModel        _hudModel        { get; set; }\n"
                + "\n"
                + "    public override void Execute()\n"
                + "    {\n"
                + "        RD_Match match = _sharedDataModel.GetScriptable<RD_Match>();\n"
                + "        if (match == null)\n"
                + "            return; // already reported: nobody filed it\n"
                + "\n"
                + "        _hudModel.SetResult(match.Winner, match.Score);\n"
                + "    }\n"
                + "}",
                "A Command in HudModule reading MatchModule's data. Modules.Hud references Modules.Match.Shared and nothing else of Match.");
            painter.Paragraph(
                "A Model may read one too, in its PostConstruct: the Root filed its slot at Awake, "
                + "before any binding phase, so nothing waits for Setup. A Mediator cannot - it "
                + "injects nothing but its View - so a screen dispatches and a Command reads.");

            painter.Space();
            painter.Note(
                "Important: do not drag a shared asset onto the reader's own adapter and read it from "
                + "there. It works while the producer is in the scene, and reads an asset nobody fills "
                + "when it is not - nothing reports it. Read it through ISharedDataModel, which reports "
                + "an asset nobody filed and points at the fix.");

            painter.Separator();
            painter.SubHeading("Mono Map - the scene components the module drives");
            painter.Paragraph(
                "What the module needs from the scene and cannot make itself: a Canvas, a spawn "
                + "point, a camera rig, an EventSystem. They sit under the Root, and the adapter files "
                + "them by name so the Model finds them the same way it finds an asset.");
            painter.Code(
                "public void PostConstruct()\n"
                + "{\n"
                + "    RootAdapter adapter = _root.GetComponent<RootAdapter>();\n"
                + "\n"
                + "    _spawner = adapter.GetMonoBehaviour<EnemySpawner>();              // filed as EnemySpawner\n"
                + "    _arena   = adapter.GetMonoBehaviour<WorldPose>(\"ArenaCentre\"); // filed under a name\n"
                + "}",
                "The slot holds MonoBehaviours. A bare Transform is not one - a marker component of the module's own, a WorldPose, is what gets filed.");

            painter.Separator();
            painter.SubHeading("Shared Mono Map - the components other modules read");
            painter.Paragraph(
                "The same rule for scene components: filed once, on one Root, read anywhere. A shared "
                + "Canvas every module parents its overlays to, the one Camera a pointer projects "
                + "through, an EventSystem. The component's type has to be one the reader can name - "
                + "Unity's own, or one from a Shared assembly.");
            painter.Code(
                "[Inject] private ISharedDataModel _sharedDataModel { get; set; }\n"
                + "\n"
                + "Canvas overlay = _sharedDataModel.GetMonoBehaviour<Canvas>(\"OverlayCanvas\");");

            painter.Separator();
            painter.SubHeading("What goes wrong");
            painter.Bullet(
                "Nothing filed under the name: an error naming it and the slot to fix, and the caller gets null. The adapter says the same for its own slots.");
            painter.Bullet("The same asset filed as shared on two Roots: a warning naming both; the first filing answers. Remove the second.");
            painter.Bullet("Two different assets shared under one name: an error naming both Roots and both assets; the first answers. Rename one.");
            painter.Bullet(
                "A shared asset read off the reader's own adapter: works until the producer is missing, then reads empty data in silence.");
            painter.Image(_images.Get("SharedAssetNotFiledError.png"),
                "The report for an asset nobody filed. Double-clicking it opens the reader that asked.");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Rule("The suffix inside an asset matches the prefix on it.");
            painter.Paragraph(
                "CD_Maps holds MapCVO, PD_Maps holds MapPVO. Mixing them breaks the one thing the "
                + "convention buys you: reading a name and knowing the lifetime.");

            painter.Space();
            painter.Code(
                "[CreateAssetMenu(fileName = \"CD_Maps\", menuName = \"Game/Data/CD_Maps\")]\n"
                + "internal class CD_Maps : ScriptableObject\n"
                + "{\n"
                + "    public List<MapCVO> Maps = new();\n"
                + "}\n"
                + "\n"
                + "[Serializable]\n"
                + "public class MapCVO\n"
                + "{\n"
                + "    public string Id;\n"
                + "    public int    StarTarget;\n"
                + "}");

            painter.Separator();
            painter.SubHeading("A value object that carries two kinds");
            painter.Paragraph(
                "Sometimes the authored half and the runtime half are wanted in the same place. The "
                + "holder is then named after neither of them, and the halves keep their own "
                + "suffixes. Calling this GameHexCVO would be a lie about half its contents.");

            painter.Space();
            painter.Code(
                "[Serializable]\n"
                + "public class GameHexVO\n"
                + "{\n"
                + "    public GameHexCVO Config;   // what the level author placed\n"
                + "    public GameHexRVO Runtime;  // what play produced\n"
                + "}");

            painter.Separator();
            painter.SubHeading("What goes wrong");
            painter.Bullet("MapData, MapConfig, MapSO. A descriptive name says nothing about lifetime; the suffix family is the convention.");
            painter.Bullet(
                "Writing to a CD_ asset at runtime. Config is constant - if it changes during play it is RD_, and if it must survive a restart it is PD_.");
            painter.Bullet("A CVO list inside a PD_ asset. The suffix has to match the asset it lives in.");
            painter.Bullet(
                "A data class dropped anywhere. It belongs in Data/UnityObjects or Data/ValueObjects; the generators and the namespace tools depend on it.");

            painter.Space();
            painter.Note(
                "Which prefixes and suffixes are legal is declared in <Solution>.sln.DotSettings, "
                + "written by Tools > FlowIoC > Module Scanner.");
        }
    }
}

#endif