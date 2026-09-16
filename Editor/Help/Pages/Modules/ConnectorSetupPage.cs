#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>Where the modules of the set meet: one sub-context per crossing.</summary>
    internal class ConnectorSetupPage : ModulePage
    {
        public override string Title => "Connector";

        public override string Subtitle => "Where the modules meet";

        public override FlowIcon Icon => FlowIcon.Link;

        public override string ModuleFolderName => "ConnectorModule";

        public override bool InSetupSet => true;

        public override string BodyHeadline => "One sub-context per crossing.";

        public override string BodyTagline =>
            "ConnectorModule wires MainModule to the main screen, the main screen to the gameplay "
            + "screen, and LoadingConnectorSubContext joins the two loading presentations to the service.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it owns");
            painter.Bullet(
                "A sub-context per counterpart module, each getting the holders it joins with "
                + "InjectionBinderCrossContext.GetInstance and connecting one module's Outgoing to "
                + "another's Incoming - never binding a holder of its own.");
            painter.Bullet(
                "The longest reference list in the project, on purpose: the Connector is the one place "
                + "allowed to know the game's shape.");

            painter.Note(
                "This module is the game's from the day it lands. It carries no version and is never "
                + "updated by the package.");
        }
    }
}

#endif
