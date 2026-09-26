#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
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
                new HelpTreeNode("SD_Maps", "saveable data - loaded at startup, saved again on every change"),
                new HelpTreeNode("ED_MapTools", "editor data - only editor tooling reads it"),
                new HelpTreeNode("DD_Maps", "database data - a copy of what a backend owns")),
            new HelpTreeNode("ValueObjects", "the plain classes those assets are built out of",
                new HelpTreeNode("MapVO", "belongs to no one asset - a payload, a return shape"),
                new HelpTreeNode("MapCVO", "what CD_Maps holds"),
                new HelpTreeNode("MapRVO", "what RD_MapPool holds"),
                new HelpTreeNode("MapSVO", "what SD_Maps holds"),
                new HelpTreeNode("MapEVO", "what ED_MapTools holds"),
                new HelpTreeNode("MapDVO", "what DD_Maps holds")));

        /// <summary>
        /// The asset file prefixes, one row per kind of file: what it is, its prefix, an example.
        /// Exposed so a test can hold the table to the prefixes the agent rules name.
        /// </summary>
        public IReadOnlyList<string[]> AssetPrefixes { get; } = new[]
        {
            new[] {"Texture a material samples", "TX_", "TX_Rock_Normal"},
            new[] {"Sprite, for UI and 2D alike", "SPR_", "SPR_Icon_Coin"},
            new[] {"Material", "MT_", "MT_Rock"},
            new[] {"Shader, Shader Graph, Sub Graph", "Shader_", "Shader_Water"},
            new[] {"Prefab", "PB_", "PB_Coin"},
            new[] {"Prefab variant", "PBV_", "PBV_Coin_Gold"},
            new[] {"Mesh with no rig (FBX)", "SM_", "SM_Road_Straight"},
            new[] {"Rigged, skinned mesh (FBX)", "RM_", "RM_Hero_Fox"},
            new[] {"Animation clip, and an animation-only FBX", "Anim_", "Anim_Hero_Fox_Run"},
            new[] {"Animator Controller", "Animator_", "Animator_Hero_Fox"},
            new[] {"Animator Override Controller", "AnimOverride_", "AnimOverride_Hero_Fox_Skin"},
            new[] {"Avatar Mask", "AvatarMask_", "AvatarMask_UpperBody"},
            new[] {"Audio clip", "SND_", "SND_SFX_GunShot_01"},
            new[] {"Audio Mixer", "Mixer_", "Mixer_Master"},
            new[] {"VFX Graph", "VFX_", "VFX_Explosion"},
            new[] {"Render Texture", "RT_", "RT_Minimap"},
            new[] {"Sprite Atlas", "Atlas_", "Atlas_Shop"},
            new[] {"Timeline", "Timeline_", "Timeline_Intro"},
            new[] {"Physics Material", "PhysicsMat_", "PhysicsMat_Ice"},
            new[] {"Volume Profile", "Volume_", "Volume_Night"},
            new[] {"Font, and its TextMesh Pro asset", "Font_", "Font_Lexend_Bold_SDF"},
            new[] {"Pool group, a CD_PoolGroup asset", "Pool_", "Pool_Forest"}
        };

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
                "Which prefix a type takes, and which of the two folders it belongs in."),
            new HelpTab("Asset Files", DrawAssetFiles,
                "Every other asset a project makes carries a prefix too.",
                "A texture, a sprite, a material, a prefab, a mesh, a sound. The prefix says what "
                + "the file is, never where it is used.")
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
            painter.Table(new[] {"Prefix", "Kind", "Where it comes from"},
                new[] {"CD_", "config data", "A designer types it in and the game only ever reads it."},
                new[] {"RD_", "runtime data", "Play produces it, and nothing keeps it once play stops."},
                new[]
                {
                    "SD_", "saveable data",
                    "It is loaded at startup and written back to the save system whenever it changes."
                },
                new[]
                {
                    "ED_", "editor data",
                    "Settings and caches that only editor tooling reads; nothing in a build touches it."
                },
                new[] {"DD_", "database data", "A local copy of something a backend owns, filled by a download."});

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
                "The same rule for scene components: filed once, on one Root, read anywhere. The "
                + "bounds of the arena the enemies spawn in, an EventSystem. The component has to be "
                + "a MonoBehaviour the reader can name - Unity's own, or one from a Shared assembly. A "
                + "Canvas or a Camera is a Behaviour and cannot be filed, and no module shares a canvas "
                + "for overlays: an overlay canvas exists only through the ScreenManager.");
            painter.Code(
                "[Inject] private ISharedDataModel _sharedDataModel { get; set; }\n"
                + "\n"
                + "ArenaBounds arena = _sharedDataModel.GetMonoBehaviour<ArenaBounds>(\"Arena\");");

            painter.Separator();
            painter.SubHeading("A test scene's own values");
            painter.Paragraph(
                "An asset's own values ship, and an SD_ asset's are what a new player starts with. A test "
                + "scene that needs a state of its own - always level 10 - keeps a copy in its test "
                + "module's Scriptables folder, named after the original with the test's suffix "
                + "(SD_Player_Test, the way the Loading test module keeps CD_LoadingSets_Test), and files "
                + "it on that scene's Roots in place of the original, so the modules read it the way the "
                + "game does. It does not edit the original, and it does not put a Level field on its "
                + "test Root.");
            painter.Note(
                "Important: a scene that files an SD_ copy ticks IsTest on its LocalSaveServiceRoot. "
                + "Left unticked, the save file is read over the copy and the copy's values are written "
                + "into your save; ticked, the save is neither read nor written, and the copy is back to "
                + "its own values after every Play.");

            painter.Separator();
            painter.SubHeading("What goes wrong");
            painter.Table(new[] {"What happened", "What you get"},
                new[]
                {
                    "Nothing filed under the name",
                    "An error naming it and the slot to fix, and the caller gets null. The adapter says "
                    + "the same for its own slots."
                },
                new[]
                {
                    "The same asset filed as shared on two Roots",
                    "A warning naming both; the first filing answers. Remove the second."
                },
                new[]
                {
                    "Two different assets shared under one name",
                    "An error naming both Roots and both assets; the first answers. Rename one."
                },
                new[]
                {
                    "A shared asset read off the reader's own adapter",
                    "Works until the producer is missing, then reads empty data in silence."
                });
            painter.Image(_images.Get("SharedAssetNotFiledError.png"),
                "The report for an asset nobody filed. Double-clicking it opens the reader that asked.");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Rule("The suffix inside an asset matches the prefix on it.");
            painter.Paragraph(
                "CD_Maps holds MapCVO, SD_Maps holds MapSVO. Mixing them breaks the one thing the "
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
                "Writing to a CD_ asset at runtime. Config is constant - if it changes during play it is RD_, and if it must survive a restart it is SD_.");
            painter.Bullet("A CVO list inside an SD_ asset. The suffix has to match the asset it lives in.");
            painter.Bullet(
                "A data class dropped anywhere. It belongs in Data/UnityObjects or Data/ValueObjects; the generators and the namespace tools depend on it.");

            painter.Space();
            painter.Note(
                "Which prefixes and suffixes are legal is declared in <Solution>.sln.DotSettings, "
                + "written by Tools > FlowIoC > Module Scanner.");
        }

        private void DrawAssetFiles(HelpPainter painter)
        {
            painter.Rule("The prefix says what the file is, never where it is used.");
            painter.Paragraph(
                "A type a project holds many of takes a two or three letter abbreviation. A rare "
                + "type, or one whose abbreviation would not read, takes the whole word. No prefix "
                + "is a single letter.");

            painter.Space();
            painter.Table(new[] {"Asset", "Prefix", "Example"}, AssetPrefixes.ToArray());

            painter.Separator();
            painter.SubHeading("After the prefix");
            painter.Bullet("A category is the second token: SND_Music_Combat_01, SND_SFX_GunShot_01, SND_UI_Click_01.");
            painter.Bullet("A particle effect is a prefab, so it is PB_FX_TorchFire. FX is its category, not its prefix.");
            painter.Bullet("A variant number has two digits: _01, _02.");

            painter.Separator();
            painter.SubHeading("What goes wrong");
            painter.Bullet("T_, M_, S_, P_. No prefix is a single letter - TX_, MT_, Shader_, PB_.");
            painter.Bullet(
                "AC_ on an audio clip and on an animator controller. The two collide; an audio clip is "
                + "SND_ and an animator controller Animator_.");
            painter.Bullet(
                "UI_ or IMG_ on a sprite. A sprite and a texture are told apart by their import, so a "
                + "2D game's character art is SPR_ although it is not UI.");
            painter.Bullet(
                "SM_ because the mesh is static in the scene. Static is a flag in one scene; SM_ means "
                + "the FBX has no rig and RM_ that it has one.");
            painter.Bullet("A material named after its shader. MT_Grass is named after what it dresses.");
            painter.Bullet("A prefab variant unpacked or made a base prefab keeps PBV_. Rename it with the change.");
            painter.Bullet("CD_PoolGroup_Forest. The class is CD_PoolGroup; a pool group asset is Pool_Forest.");

            painter.Separator();
            painter.SubHeading("What keeps its own name");
            painter.Bullet("A prefab named after the class it carries - GameplaySystemRoot, a screen's prefab.");
            painter.Bullet("A scene, which keeps the <Name>Scene that Create Module writes.");

            painter.Space();
            painter.Note(
                "A vendor package's assets are never renamed. The next update brings the old names "
                + "back, and every reference to the renamed copy breaks.");
        }
    }
}

#endif