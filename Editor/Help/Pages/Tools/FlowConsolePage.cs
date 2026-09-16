#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Tools
{
    internal class FlowConsolePage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public FlowConsolePage() : base(null)
        {
        }

        public override string Title => "Flow Console";

        public override FlowIcon Icon => FlowIcon.Terminal;

        protected override string BodyHeadline => "The framework logs itself into one window.";

        protected override string BodyTagline =>
            "Tools > FlowIoC > Flow Console - and a tab beside Unity's Console from the day the "
            + "project meets FlowIoC, kept by the layout. Every signal dispatch, command step, "
            + "context phase, screen transition and pool operation, on channels you switch on and "
            + "off independently - which is where most debugging in FlowIoC starts rather than at "
            + "a breakpoint.";

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
                + "opens, and on the tab's right-click menu with the rest of the bar - where "
                + "Unity's Console keeps its line count.");

            painter.SubHeading("A device's rows arrive too");
            painter.Paragraph(
                "Editor in the toolbar is Unity's own attach-to-player picker, the one the Console "
                + "and the Profiler draw. Pick a development player and every row it would have "
                + "recorded arrives here with its channel, its flow, its frame and its source, and "
                + "the player's own Unity lines - an exception, a native warning - come with their "
                + "trace. A line is drawn where the device's rows begin and each row carries a dim "
                + "tag with the device's name after the time.");
            painter.Note(
                "Important: one step fails silently when skipped. The build must be a Development "
                + "Build - a release player never connects, and has nothing to send, because logging "
                + "compiles only in the Editor and in a Development Build. There is no scripting "
                + "define to add.");

            painter.SubHeading("The channels you switch off are yours");
            painter.Paragraph(
                "Clicking a channel in the Filters panel writes EditorPrefs, and nothing committed: "
                + "one developer's filter has no business turning up in everybody else's diff - or "
                + "switching their channels to match when they pull. What a channel shows as for "
                + "somebody who has not touched it is the default it ships with - Signal, Command and "
                + "Unity's three on, the framework's machinery off, every module on - and that is "
                + "what a channel a module added yesterday shows as. Presets > Defaults drops your "
                + "switches and puts you back on it. How much the logger does - logging on or off, "
                + "the Source capture, the mirror into Unity's console, how many rows are kept - is "
                + "yours too, under Edit > Preferences > FlowIoC > Flow Console.");
            painter.SubHeading("A module's colour and tag are the module's own");
            painter.Image(_images.Get("ChannelContextMenu.png"),
                "The Filters panel's Modules group, and the right-click on a module's channel. Every "
                + "module has a colour from the day it is created - the swatch, and the tag on the "
                + "source line of its rows, [Player] for PlayerModule, in that colour.");
            painter.Paragraph(
                "The colour is picked from twelve tones by the module's name, so the same module is "
                + "the same colour on every machine, and it is written beside the channel in the "
                + "module's generated FlowModule part. A module that wants a colour of its own says so "
                + "in its MODULE.md, as a Colour line directly above the generated block, and may "
                + "declare a profile - a tag of its own, and how the message after it is drawn - the "
                + "same way. Right-click the channel in the Filters panel and choose Colour and "
                + "profile... to edit both.");
            painter.Paragraph(
                "A framework row carries its tag on a faint plate of the channel's colour, with a "
                + "stripe of the same colour down the row's edge; a module's rows carry theirs bare, "
                + "and no stripe. That is what tells [Screen] the framework wrote from [Screen] a "
                + "module of that name wrote.");
            painter.Image(_images.Get("ChannelStyleWindow.png"),
                "Right-click a module's channel > Colour and profile... The window opens on the "
                + "colour and profile the module has; Palette puts the colour back on the pick for "
                + "its name. Apply writes the two lines into the module's MODULE.md and regenerates "
                + "the part, and the console follows on the next compile. A colour on the palette's "
                + "pick and a profile on the default tag write no line; a prefix cleared out writes "
                + "Profile: none, for a module whose lines carry no tag. Reset takes both lines off "
                + "the card. Cancel writes nothing.");
            painter.Code(
                "Colour: #E5A50A\n"
                + "Profile: prefix=\"[Analytics]\" prefix-style=bold prefix-colour=#39FF00",
                "The two lines the window writes, above the generated block of MODULE.md. They can be "
                + "written by hand as well.");
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
                "FlowLogger.Log(\"Execute - AddCurrencyCommand\");\n"
                + "FlowLogger.LogWarning(\"Currency clamped to zero.\");\n"
                + "FlowLogger.LogError(\"Currency went negative.\");");
            painter.Paragraph(
                "A line names no channel. The compiler writes the file the call sits in into the "
                + "call, and the module is in the path: a Command under Modules/PlayerModule logs on "
                + "PlayerModule, a screen under zScreenModules/MainScreenModule on MainScreenModule, "
                + "a test module's files on the module they test, and a file outside any module on "
                + "Default. The same call carries the line, so the row knows its file and line "
                + "without a stack being walked. A line copied into another module lands on that "
                + "module by itself.");
            painter.Paragraph(
                "Name the channel by hand only where the file is not the module the line is about "
                + "- a Connector reporting on the module it wires - and then with the module's "
                + "constant. Two strings are channel then message, so a plain word in the first "
                + "place is a channel no module owns.");
            painter.Code(
                "FlowLogger.Log(FlowModule.PlayerModule, \"Execute - AddCurrencyCommand\");");
            painter.Note(
                "Spell the names out. A log message never uses nameof: written inside "
                + "AddCurrencyCommand, nameof(AddCurrencyCommand) is a real reference to the type, "
                + "so Find Usages answers \"where is this Command used\" with the command's own "
                + "logging lines instead of the Context that binds it. A rename then leaves the "
                + "literal stale, and that is the cheaper of the two costs.");
            painter.Note(
                "Logging compiles only in the Editor and in a Development Build, so lines you leave "
                + "in cost a shipped build nothing and there is no scripting define to manage. The "
                + "framework's own channels are always there, so watching a flow does not need any "
                + "log lines of your own.");
            painter.Note(
                "Important: an error is the exception, and it is logged exactly once. "
                + "FlowLogger.LogError carries no [Conditional], so an error reaches the console "
                + "in a release build too - a project with logging switched off is the one that "
                + "most needs to be told something is broken. So never put a Debug.LogError beside "
                + "a FlowLogger.LogError for the same fault: FlowLogger forwards to Debug itself, "
                + "and the pair prints the error twice whenever logging is on.");
            painter.Paragraph(
                "The channel list in FlowModule is generated from the modules present in the "
                + "project. Change the modules, not the generated file.");
        }
    }
}

#endif