#if UNITY_EDITOR

using System.Collections.Generic;

namespace FlowIoC.Editor.Help.Pages
{
    internal class DataTypesPage : HelpPage
    {
        public DataTypesPage() : base(null)
        {
        }

        public override string Title => "Data Types";

        public override string Icon => "ScriptableObject Icon";

        /// <summary>
        /// Exposed for the same reason the folder tree is: a test can check the names here against
        /// the prefixes and suffixes the shipped code style declares legal.
        /// </summary>
        public HelpTreeNode Root { get; } = new HelpTreeNode("Datas", "everything a module stores",
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

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Rules", DrawRules)
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A module keeps its data in two folders, and the name of a type says which kind of "
                + "data it is before you open the file. Datas/UnityObjects holds the ScriptableObject "
                + "assets; Datas/ValueObjects holds the plain [Serializable] classes those assets are "
                + "built out of.");
            painter.Paragraph(
                "The prefix on an asset says where its contents come from. The value objects it "
                + "carries take the matching suffix, so a name tells you what is safe to regenerate "
                + "and what has to survive a restart.");

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

            painter.SubHeading("A family of your own");
            painter.Paragraph(
                "The five are what FlowIoC ships, not the whole vocabulary. A project that needs "
                + "another kind adds a prefix and its matching suffix, and declares both in the "
                + "solution code style so the IDE stops flagging the name.");
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

            painter.SubHeading("What goes wrong");
            painter.Bullet("MapData, MapConfig, MapSO. A descriptive name says nothing about lifetime; the suffix family is the convention.");
            painter.Bullet("Writing to a CD_ asset at runtime. Config is constant - if it changes during play it is RD_, and if it must survive a restart it is PD_.");
            painter.Bullet("A CVO list inside a PD_ asset. The suffix has to match the asset it lives in.");
            painter.Bullet("A data class dropped anywhere. It belongs in Datas/UnityObjects or Datas/ValueObjects; the generators and the namespace tools depend on it.");

            painter.Space();
            painter.Note(
                "Which prefixes and suffixes are legal is declared in <Solution>.sln.DotSettings, "
                + "written by Tools > FlowIoC > Module Configuration > Update Namespace Settings.");
        }
    }
}

#endif
