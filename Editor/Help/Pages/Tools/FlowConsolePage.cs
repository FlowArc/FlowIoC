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

            painter.SubHeading("It is the only console you need open");
            painter.Paragraph(
                "Unity's own output is here too - Debug.Log, exceptions, and compile errors - so "
                + "Clear, Collapse, Error Pause, Clear on Play and Clear on Recompile all behave "
                + "the way they do in Unity's console. Double-clicking a row opens the code that "
                + "wrote it, and where the row is the framework complaining about your code, it "
                + "opens your file rather than the framework's guard clause.");
            painter.Paragraph(
                "Flow groups the rows into the flows they belong to, so a busy frame reads as one "
                + "block per operation rather than four interleaved. Timing swaps the clock for "
                + "the frame and the gap since the row above. Pinning a row keeps it through the "
                + "filters, the trim and every automatic clear.");

            painter.SubHeading("Silencing a noisy loop");
            painter.Paragraph(
                "A tick loop dispatching many times per second drowns everything else. Two flags "
                + "silence the framework's own lines for it without hiding the channel that "
                + "everything else shares.");
            painter.Code(
                "// The signal's dispatch and group lines.\n"
                + "public Signal Tick = new(hideCommandLog: true);\n"
                + "\n"
                + "// The command's execute and pool-return lines.\n"
                + "[HideCommandLog]\n"
                + "internal class AdvanceTimersCommand : Command { }");
            painter.Note(
                "Both are needed for a fully silent loop - the signal flag does not cover the "
                + "command lines and the attribute does not cover the dispatch. Neither touches "
                + "your own FlowLogger calls inside the command body, which is the point: the "
                + "loop stops narrating itself and still says the one thing you asked it to.");
            painter.Paragraph(
                "For a loop you did not write, the search box does the same job without touching "
                + "code: a term starting with '-' hides every row carrying it and leaves the "
                + "channel alone.");

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
            painter.Note(
                "Important: an error is the exception, and it is logged exactly once. "
                + "FlowLogger.LogError carries no [Conditional], so an error reaches the console "
                + "with or without ENABLE_LOG - a project with logging switched off is the one that "
                + "most needs to be told something is broken. So never put a Debug.LogError beside "
                + "a FlowLogger.LogError for the same fault: FlowLogger forwards to Debug itself, "
                + "and the pair prints the error twice whenever logging is on.");
            painter.Paragraph(
                "The channel list in FlowLogType is generated from the modules present in the "
                + "project. Change the modules, not the generated file.");
        }
    }
}

#endif