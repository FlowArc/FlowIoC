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

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "Tools > FlowIoC > Console > Flow Console. The framework logs itself into this "
                + "window: every signal dispatch, command step, context phase, screen transition "
                + "and pool operation, on channels you can switch on and off independently.");
            painter.Paragraph(
                "Most debugging in FlowIoC starts here rather than at a breakpoint. A signal that "
                + "never arrives, a command that never ran, a context that launched before the one "
                + "it depends on - each of those is visible as a gap in the flow.");

            painter.SubHeading("Logging from your own code");
            painter.Code(
                "FlowLogger.Log(FlowLogType.PlayerModule,\n"
                + "    $\"{nameof(Execute)} - {nameof(AddCurrencyCommand)}\");\n"
                + "\n"
                + "FlowLogger.LogError(FlowLogType.PlayerModule, \"Currency went negative.\");");
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
