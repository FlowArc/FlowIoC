#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class FlowConsolePage : HelpPage
    {
        public FlowConsolePage() : base(null)
        {
        }

        public override string Title => "Flow Console";

        public override FlowIcon Icon => FlowIcon.Terminal;

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
                + "the way they do in Unity's console. A line Unity wrote carries an icon - the Unity "
                + "logo, the script icon, the shader icon - where the framework's channels carry "
                + "their tag. Double-clicking a row opens the code that "
                + "wrote it, and where the row is the framework complaining about your code, it "
                + "opens your file rather than the framework's guard clause.");
            painter.Paragraph(
                "Flow groups the rows into the flows they belong to, so a busy frame reads as one "
                + "block per operation rather than four interleaved. Time picks what leads a row: "
                + "the clock to the second as Unity shows it, the clock with its milliseconds, or "
                + "the frame and the gap since the row above. Pinning a row keeps it through the "
                + "filters, the trim and every automatic clear. All three sit on the bar the gear "
                + "opens.");

            painter.SubHeading("A device's rows arrive too");
            painter.Paragraph(
                "Editor in the toolbar is Unity's own attach-to-player picker, the one the Console "
                + "and the Profiler draw. Pick a development player and every row it would have "
                + "recorded arrives here with its channel, its flow, its frame and its source, and "
                + "the player's own Unity lines - an exception, a native warning - come with their "
                + "trace. A line is drawn where the device's rows begin and each row carries a dim "
                + "tag with the device's name after the time.");
            painter.Note(
                "Important: two steps fail silently when skipped. The build must be a Development "
                + "Build - a release player never connects. And the flow is visible only when the "
                + "build's scripting defines carry ENABLE_LOG, the same define the editor needs; "
                + "without it only errors and Unity's own lines arrive from the device.");

            painter.SubHeading("The channels you switch off are yours");
            painter.Paragraph(
                "Clicking a channel in the Filters panel writes EditorPrefs, not CD_FlowConsole. "
                + "The asset is committed, and one developer's filter has no business turning up in "
                + "everybody else's diff - or switching their channels to match when they pull. What "
                + "the asset carries is the project default, set in its inspector as Default on or "
                + "Default off per channel: what somebody sees who has not touched it, and what a "
                + "channel a module added yesterday shows as. Presets > Project defaults drops your "
                + "switches and puts you back on what the asset says.");
            painter.Paragraph(
                "A switch hides the channel's plain logs. A warning and an error show whatever "
                + "its channel's switch, its group's mute or an isolation says - Context is off by "
                + "default, and the shared data reports are written on it. The Warning and Error "
                + "toggles on the bar are what hide a report, and Send Logs To Unity Console "
                + "follows the same rule: a warning is mirrored whatever the switch says, a plain "
                + "log only while its channel is on, an error always.");
            painter.Note(
                "Important: every channel switched off looks exactly like a quiet game. The list "
                + "says so in its middle when that is the case, and counts the rows the filters "
                + "hold back when rows exist and none is passing - so an empty console is never "
                + "silently one of the two.");

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