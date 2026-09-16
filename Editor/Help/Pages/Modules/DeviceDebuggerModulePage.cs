#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The device debugger module: the on-device panel, what each tab shows, how a game puts an
    /// option on it with one attribute, and the setup steps that fail silently when skipped.
    /// </summary>
    internal class DeviceDebuggerModulePage : ModulePage
    {
        public override string ModuleFolderName => "DeviceDebuggerModule";

        public override string Title => "Device Debugger";

        public override string Subtitle => "An SRDebugger-shaped panel on the phone: the log, the options, every signal, stats and the device";

        public override FlowIcon Icon => FlowIcon.Terminal;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "Put the Root in the boot scene at -95 with the config asset on its adapter.",
                "The panel exists in the Editor and in a Development Build; a release build keeps the "
                + "Root and nothing else. The config asset says how the panel is opened and how much "
                + "it keeps."),
            new HelpTab("Options", DrawOptions,
                "One [DebugOption] on a public signal field or on a shipped step puts a row on the panel.",
                "An Incoming signal is a control by its payload type, an Outgoing one a value row, a "
                + "Service's step a button. Nothing registers by code and nothing reads a Model."),
            new HelpTab("Tabs", DrawTabs,
                "Console, Options, Signals, Stats, Info - what each one shows and where it reads from.",
                "The Console is the Flow Console's rows on the device; Signals lists every public "
                + "Incoming signal and fires it; Stats and Info read the machine.")
        };

        public override string InstalledHint =>
            "Drop DeviceDebuggerServiceRoot into your boot scene and press Play; the pill in the "
            + "corner opens the panel. The Setup tab has the steps.";

        public override string BodyHeadline =>
            "A development build read and steered on the phone, without a cable.";

        public override string BodyTagline =>
            "The corner opens a panel: the log with its errors, the cheats and toggles a game "
            + "chose to expose, every public signal ready to fire, the frame rate and the memory, "
            + "and what the device is - the shape of SRDebugger, on FlowIoC's own terms.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "A Console tab: every row the Flow Console would show - the framework's own lines, "
                + "the game's, Unity's exceptions - with the three kind filters, a search, a detail "
                + "pane with the stack trace, Clear and Copy. The rows come from the same logger, "
                + "so a boot that fails on the phone is readable there.");
            painter.Bullet(
                "An Options tab: the rows a game and its modules put there with [DebugOption] - a "
                + "button, a toggle, a slider, a text field, a dropdown, or a value the module "
                + "announces - grouped by category.");
            painter.Bullet(
                "A Signals tab: every public Incoming signal of every module in the scene, grouped "
                + "by holder, with a Fire button and a field for a primitive payload. Nothing to "
                + "declare; the module's public surface is exactly its signals.");
            painter.Bullet(
                "A Stats tab: frame rate, frame time, the worst frame of the last two seconds as a "
                + "bar strip, allocated and reserved memory, the mono heap, GC runs, uptime, time "
                + "scale.");
            painter.Bullet(
                "An Info tab: the build, the device, the OS, the GPU, the screen and its DPI, the "
                + "safe area, the data path - with Copy.");
            painter.Bullet(
                "An error badge: while the panel is closed, an error or an exception turns the "
                + "corner red with the unread count, and tapping it opens the Console. This is the "
                + "readable stand-in for Unity's tiny development console.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.DeviceDebugger and injects "
                + "IDeviceDebuggerService to Show or Hide the panel from code - a settings screen's "
                + "hidden button, a gesture of the game's own. What a module exposes on the panel "
                + "needs no reference at all: the [DebugOption] attribute lives in the package.");

            painter.SubHeading("Where it differs from SRDebugger");
            painter.Paragraph(
                "SRDebugger reads and writes a property. Here an option is a signal: a control "
                + "dispatches it and the Command bound to it decides, and a control learns the truth "
                + "from an Outgoing value row with the same category and label. The Signals tab has "
                + "no counterpart there at all. And the panel is UI Toolkit, scaled by physical size, "
                + "so the text is the same size on every phone.");

            painter.SubHeading("Trying it out");
            painter.Paragraph(
                "The module ships with a test module beside it, and the scene it runs in arrives "
                + "with it. Open DeviceDebuggerTestScene under the test module's Scenes folder and "
                + "press Play: the pill in the bottom-right corner opens the panel, the badge reads "
                + "1 from the error the scene logs on purpose, and the Options tab carries one row "
                + "of every kind - a toggle that echoes back, a slider, a field, a dropdown, a "
                + "button that throws, a button that logs two hundred lines.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("1. The Root");
            painter.Paragraph(
                "Drop DeviceDebuggerServiceRoot from the module's Prefabs folder into the scene the "
                + "game boots from. It ships at Initialize Order -95, just after the save restore, "
                + "so the binding lines of every Root after it land in its ring; it is persistent, "
                + "so the ring survives a scene load. One Root per game.");
            painter.Note(
                "Important: the Root must be in the first scene, and only there. A second Root in a "
                + "later scene is a second panel and a second hook on the logger; a Root missing "
                + "from the boot scene means the boot's lines are not in the ring, and nothing says "
                + "so.");

            painter.SubHeading("2. The config asset");
            painter.Paragraph(
                "The prefab carries the shipped CD_DeviceDebugger on its adapter. Make your own "
                + "from Create > FlowIoC > DeviceDebuggerModule > Data and put it in the adapter's "
                + "own Scriptables slot to change how the panel is reached: a Button pill, a "
                + "TripleTap zone, or None (the service and the badge only); which corner; how "
                + "many rows the Console keeps; whether the badge shows; whether the pill shows the "
                + "frame rate.");

            painter.SubHeading("3. The build");
            painter.Paragraph(
                "Tick Development Build. The panel compiles behind the same line as logging - the "
                + "Editor and a Development Build - and a release build keeps the Root with a "
                + "service that answers IsAvailable = false; every call is a no-op there and the "
                + "panel object is destroyed at Setup.");
            painter.Note(
                "Important: in a release build there is no panel and no error to say so. A tester "
                + "who reports that the pill is missing has a release build in hand.");

            painter.SubHeading("4. UI Toolkit");
            painter.Paragraph(
                "The panel is a UIDocument under the Root, with its PanelSettings and theme in the "
                + "module's UI folder. Touch reaches it through the scene's EventSystem when there "
                + "is one and through UI Toolkit's own event system when there is not; the game's "
                + "uGUI screens are untouched. The safe area is applied to the panel's root, so a "
                + "notch never covers the bar.");
            painter.Note(
                "Important: the panel draws above every uGUI canvas because it is rendered after "
                + "them; a game that renders its own UI to a texture and composites it later sees "
                + "the panel under that composite. Raise the PanelSettings sort order or draw the "
                + "composite earlier.");
        }

        private void DrawOptions(HelpPainter painter)
        {
            painter.SubHeading("On a signal field");
            painter.Paragraph(
                "The attribute goes on a field of the module's public holder - the one under "
                + "Scripts/Signals, bound across contexts. An Incoming field becomes a control by "
                + "its payload type; an Outgoing field becomes a value row showing the last payload "
                + "it carried.");
            painter.Code(
                "public class GameplaySignalsIncoming\n"
                + "{\n"
                + "    [DebugOption(\"Gameplay\", \"Win level\")] public Signal WinLevel = new();\n"
                + "    [DebugOption(\"Cheats\", \"God mode\")] public Signal<bool> SetGodMode = new();\n"
                + "    [DebugOption(\"Cheats\", \"Coins\", Min = 0, Max = 1000, Step = 50)] public Signal<int> SetCoins = new();\n"
                + "    [DebugOption(\"Cheats\", \"+1000 coins\", Argument = 1000)] public Signal<int> AddCoins = new();\n"
                + "    [DebugOption(\"Gameplay\", \"Difficulty\")] public Signal<DifficultyType> SetDifficulty = new();\n"
                + "}\n"
                + "\n"
                + "public class GameplaySignalsOutgoing\n"
                + "{\n"
                + "    [DebugOption(\"Cheats\", \"God mode\")] public Signal<bool> GodModeChanged = new();\n"
                + "}");
            painter.Table(new[] {"Field", "Row"},
                new[] {"Signal", "a button"},
                new[] {"Signal<bool>", "a toggle"},
                new[] {"Signal<int>, Signal<float>, Signal<double>", "a number - a slider when Min and Max are set, else a field and Set"},
                new[] {"Signal<string>", "a text field and Set"},
                new[] {"Signal<TEnum>", "a dropdown of the names"},
                new[] {"Signal<T> with Argument", "a button that fires the constant"},
                new[] {"an Outgoing signal", "a value row: the last payload, or how many times a payload-less signal fired"},
                new[] {"anything else", "drawn disabled, with the reason"});
            painter.Paragraph(
                "Category and Label may be left out: the category defaults to the module - the "
                + "holder's name without Signals - and the label to the field name spaced at each "
                + "capital. Order sorts rows inside a category. A value row whose category and label "
                + "match a control feeds that control's shown state, which is how a toggle stays "
                + "truthful without reading a Model.");
            painter.Note(
                "Important: only a holder bound across contexts is seen. An internal holder is bound "
                + "locally and is not public surface; an attribute on one of its fields is silently "
                + "never found. A module whose options do not appear has its attribute on the wrong "
                + "holder, or the Root that binds the holder is not in the scene.");

            painter.SubHeading("On a shipped step");
            painter.Paragraph(
                "A Service's step - a Command nested under its interface - carries the attribute on "
                + "the class, and Argument is the parameter the step is bound with; a step may carry it "
                + "more than once, one row per argument. The debugger binds each in its own binder with "
                + "a signal of its own, so a tap runs the step through the ordinary pipeline and shows in "
                + "the Flow Console like any other step. A step whose Service is not bound in the scene "
                + "is left off the panel, because it would inject nothing and throw on the tap.");
            painter.Code(
                "[DebugOption(\"Haptic\", \"Play Success\", Argument = HapticPreset.Success)]\n"
                + "[DebugOption(\"Haptic\", \"Play Failure\", Argument = HapticPreset.Failure)]\n"
                + "public class Play : Command<HapticPreset> { ... }");
            painter.Paragraph(
                "The shipped modules already carry theirs: Haptic's Play (Success, Failure, Heavy "
                + "impact), Local Save's SaveAll, Mobile Notification's RequestPermission and CancelAll, "
                + "and the screen service's HideAll.");
            painter.Note(
                "The scan for annotated steps walks the loaded assemblies once per run, when the "
                + "panel first opens; the holders are walked again on every open, because a scene "
                + "that came and went may have changed them.");

            painter.SubHeading("From code");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.DebugPanelRequested)\n"
                + "    .ToSequence<IDeviceDebuggerService.Commands.Show>(DebugTab.Console);");
            painter.Paragraph(
                "IDeviceDebuggerService.Commands.Show opens the panel on the tab it was bound with, "
                + "DebugTab.Last for whichever was open; Commands.Hide closes it. A Command may also "
                + "inject the service and call Show, Hide or Toggle, and read IsAvailable and IsOpen.");
        }

        private void DrawTabs(HelpPainter painter)
        {
            painter.SubHeading("Console");
            painter.Paragraph(
                "The rows FlowLogger recorded, in a ring the config sizes - a thousand by default, "
                + "the oldest going first - laid out the way the Flow Console lays out a two-line row: "
                + "the kind as an icon, the message, and under it the channel tag in its colour with "
                + "where the line came from. Log, Warn and Error toggle each kind and show its count; "
                + "Filters folds out the channel switches, the game's modules first and the framework's "
                + "after; the search matches the message or the channel; a tapped row opens its detail "
                + "over the list, with the time, the source and the stack trace, and Back returns; Copy "
                + "puts the whole ring on the clipboard, which is the one export a phone has. The "
                + "panel's own traffic is logged too - a tap on an option reads as its dispatch under it.");

            painter.SubHeading("Options");
            painter.Paragraph(
                "The annotated rows, a foldout per category. A control's value goes out as text and "
                + "is parsed to the payload type by the module; a value that cannot be parsed is "
                + "refused with a warning in the Console, not sent.");

            painter.SubHeading("Signals");
            painter.Paragraph(
                "Every Incoming signal of every cross-context holder, a foldout per holder, the "
                + "framework's own last and folded. A signal with no payload fires on the button; one "
                + "with a primitive or enum payload takes it from the field beside the button; any "
                + "other payload is drawn disabled with its type named.");

            painter.SubHeading("Stats");
            painter.Paragraph(
                "Sampled by the view's tick while the panel is open, four times a second on the "
                + "page: the frame rate over the last second, the current and the worst frame time "
                + "of the last 120 frames, the bar strip with a guide at 60 fps, allocated and "
                + "reserved memory, the mono heap, GC runs, uptime and time scale. With Show FPS On "
                + "Trigger ticked in the config, the pill shows the rate while the panel is closed.");

            painter.SubHeading("Info");
            painter.Paragraph(
                "Read once at Setup: product and version, Unity version, whether the build is a "
                + "development one, platform and install mode, device, OS, CPU, RAM, GPU, screen and "
                + "DPI, safe area, language, internet, the persistent data path. Copy puts them on "
                + "the clipboard as label: value lines.");
        }
    }
}

#endif