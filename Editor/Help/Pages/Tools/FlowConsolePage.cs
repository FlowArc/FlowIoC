#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class FlowConsolePage : HelpPage
    {
        public FlowConsolePage() : base(null)
        {
        }

        public override string Title => "Flow Console";

        public override string Icon => "UnityEditor.ConsoleWindow";

        protected override string BodyHeadline => "The framework logs itself into one window.";

        protected override string BodyTagline =>
            "Tools > FlowIoC > Flow Console. Every signal dispatch, command step, context phase, "
            + "screen transition and pool operation, on channels you switch on and off "
            + "independently - which is where most debugging in FlowIoC starts rather than at a "
            + "breakpoint.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A signal that never arrives, a command that never ran, a context that launched "
                + "before the one it depends on - each of those is visible as a gap in the flow.");

            painter.SubHeading("Logging from your own code");
            painter.Code(
                "FlowLogger.Log(FlowLogType.PlayerModule,\n"
                + "    \"Execute - AddCurrencyCommand\");\n"
                + "\n"
                + "FlowLogger.LogError(FlowLogType.PlayerModule, \"Currency went negative.\");");
            painter.Note(
                "Spell the names out. A log message never uses nameof: written inside "
                + "AddCurrencyCommand, nameof(AddCurrencyCommand) is a real reference to the type, "
                + "so Find Usages answers \"where is this Command used\" with the command's own "
                + "logging lines instead of the Context that binds it. A rename then leaves the "
                + "literal stale, and that is the cheaper of the two costs.");
            painter.Note(
                "Logging compiles out unless the ENABLE_LOG scripting define is set, so lines you "
                + "leave in cost a shipped build nothing. The framework's own channels are always "
                + "there, so watching a flow does not need any log lines of your own.");
            painter.Paragraph(
                "The channel list in FlowLogType is generated from the modules present in the "
                + "project. Change the modules, not the generated file.");
        }
    }
}

#endif