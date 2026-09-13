#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.Icons;
using FlowIoC.Editor.ModuleInstall;
using UnityEditor;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The haptic module: the nine presets, what each one sends on each platform, where the
    /// on/off choice lives, and the button that puts it in the project.
    /// </summary>
    internal class HapticModulePage : HelpPage
    {
        private const string ModuleFolderName = "HapticModule";

        private readonly ModuleInstaller _installer =
            new ModuleInstaller(new ProjectRoot().Resolve(), new ModulesSource());

        private readonly HelpAction _install;

        private bool _isInstalled;
        private double _checkedAt = double.NegativeInfinity;

        public HapticModulePage() : base(null)
        {
            // The label and the enabled state are read every repaint rather than fixed here, so
            // the button turns itself off the moment the module lands in the project.
            _install = new HelpAction(
                () => IsInstalled() ? "Installed" : "Install",
                () => !IsInstalled(),
                Install);
        }

        public override string Title => "Haptic";

        public override string Subtitle => "Nine presets through the platforms' own haptics, and the player's on/off choice";

        public override FlowIcon Icon => FlowIcon.Broadcast;

        public override HelpAction Action => _install;

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "Put the Root in the scene. Android and iOS need nothing added by hand.",
                "The VIBRATE permission is written into the Gradle project by the module, and the "
                + "iOS file ships inside it. The Editor never vibrates; a device build is where a "
                + "preset is felt."),
            new HelpTab("Usage", DrawUsage,
                "Bind PlayHapticCommand as a step with its preset, or inject the service and call Play.",
                "A fixed haptic is a step read from the Context - ToSequence<PlayHapticCommand>"
                + "(HapticPreset.Success). A preset that depends on a decision is a Play call in the "
                + "Command that made it. A settings toggle binds SetHapticsEnabledCommand."),
            new HelpTab("Presets", DrawPresets,
                "What each preset sends, and why two of them feel alike on some Android phones.",
                "iOS plays the system haptic of the same name. Android plays an envelope through "
                + "the Vibrator, and a firmware may replace a short one with its own click.")
        };

        /// <summary>
        /// Whether the module is in the project, answered from a cache that goes stale after a
        /// second. The underlying check walks every asmdef under Assets, and the banner asks twice
        /// per repaint - often enough that doing the walk each time would cost real frames.
        /// </summary>
        private bool IsInstalled()
        {
            if (EditorApplication.timeSinceStartup - _checkedAt < 1d)
                return _isInstalled;

            _isInstalled = _installer.IsInstalled(ModuleFolderName);
            _checkedAt = EditorApplication.timeSinceStartup;

            return _isInstalled;
        }

        private void Install()
        {
            // Whatever happened, what the cache holds is now a guess about a project that has
            // changed underneath it.
            _checkedAt = double.NegativeInfinity;

            if (_installer.TryInstall(ModuleFolderName, out string error))
            {
                EditorUtility.DisplayDialog(
                    "Haptic installed",
                    $"The module is now at {ModuleInstaller.TargetFolder}/{ModuleFolderName}.\n\n"
                    + "It is yours to edit from here - the copy in the package is only the one "
                    + "installs are made from. Drop HapticServiceRoot into your scene and call "
                    + "IHapticService.Play from a Command; the Setup tab has the steps.",
                    "OK");

                return;
            }

            EditorUtility.DisplayDialog("Haptic", error, "OK");
        }

        protected override string BodyHeadline =>
            "One call plays a preset on the platform's own haptics.";

        protected override string BodyTagline =>
            "A tap on a button, a coin landing, a wave cleared, a run lost - each is one Play call "
            + "with a preset, and the module knows what that preset is on iOS and on Android.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "Nine presets, the ones iOS names: Selection, Success, Warning, Failure, and the "
                + "Light, Medium, Heavy, Rigid and Soft impacts. HapticPreset.None plays nothing, so "
                + "data-driven code says \"no haptic\" without a branch.");
            painter.Bullet(
                "On iOS a preset is UIKit's feedback generator of the same name - the haptic the "
                + "system itself plays, which cannot be tuned and needs no tuning.");
            painter.Bullet(
                "On Android a preset is an envelope played through the Vibrator as a waveform, with "
                + "no native library: the Vibrator is reached through JNI. A device without "
                + "amplitude control gets the envelope's breakpoints as an off/on pattern at full "
                + "strength; a device with no vibrator gets nothing, silently.");
            painter.Bullet(
                "The on/off choice is the module's, in PlayerPrefs under flowioc.haptic.enabled, on "
                + "by default. Turning haptics off also stops a vibration in progress.");
            painter.Bullet(
                "The VIBRATE permission Android needs is added to the Gradle project by the module "
                + "at build time. There is nothing to put in a manifest.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.Haptic and injects "
                + "IHapticService directly. It has no signals: the preset is chosen by the Command "
                + "that decided the event, and that Command calls Play. Nothing outside the module "
                + "needs telling that it did.");

            painter.SubHeading("Where the presets come from");
            painter.Paragraph(
                "The Android envelopes are Nice Vibrations' nine tables (Lofelt, MIT), with its "
                + "25 ms interpolation reproduced, so a game moving off that asset feels the same. "
                + "One cell was changed after a device run - Medium runs 120 ms rather than 80 - "
                + "and the Presets tab says why.");

            painter.SubHeading("Trying it out");
            painter.Paragraph(
                "The module ships with a test module beside it, and the scene it runs in arrives "
                + "with it. Open HapticTestScene under the test module's Scenes folder and press "
                + "Play: nine buttons, one per preset, and a toggle for the on/off choice. In the "
                + "Editor nothing vibrates - each press logs Play - <preset> on the HapticModule "
                + "channel of the Flow Console, so the flow reads there. A device build is where a "
                + "preset is felt.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("1. The Root");
            painter.Paragraph(
                "Drop HapticServiceRoot from the module's Prefabs folder into the scene. It ships "
                + "at Initialize Order -50, in the Service band; it reaches no other module, so "
                + "nothing depends on where it sits among the other Roots.");

            painter.SubHeading("2. Android");
            painter.Paragraph(
                "Nothing to add. The module carries an IPostGenerateGradleAndroidProject that "
                + "writes android.permission.VIBRATE into the generated unityLibrary manifest, once, "
                + "whatever the game or another plugin already declared. Amplitude control needs "
                + "Android 8 (API 26); below it, or on a device without it, the envelope plays as "
                + "an off/on pattern.");
            painter.Note(
                "Important: the permission is written when Gradle generates the project, so a "
                + "build made with Export Project and then edited by hand keeps it only if the "
                + "unityLibrary manifest is not replaced afterwards. Without it Android refuses the "
                + "first Play with a SecurityException, which reaches the log as an "
                + "AndroidJavaException from the Command - the build is not silent, but it is "
                + "already on the phone.");

            painter.SubHeading("3. iOS");
            painter.Paragraph(
                "Nothing to add either. Plugins/iOS/FlowHaptics.mm ships inside the module with an "
                + "iOS-only importer and compiles into the Xcode project with the rest. Rigid and "
                + "Soft need iOS 13; below it Heavy and Light stand in for them.");
            painter.Note(
                "The iOS side was written against Lofelt's SystemHaptics.m on a machine that cannot "
                + "build for iOS, and has not been through an Xcode build yet. The first one is "
                + "its test; the calls are UIKit's own and there is nothing else in the file.");

            painter.SubHeading("4. The Editor");
            painter.Paragraph(
                "The Editor never vibrates. A SilentHapticPlayer takes the place of the device "
                + "player there and logs Play - <preset> on the module's channel, so a sequence "
                + "that plays a haptic still reads in the Flow Console and a test scene runs "
                + "without a phone.");
            painter.Note(
                "Important: a test module is Editor-only by rule, so HapticTestScene cannot go into "
                + "a build. To feel the presets on a phone, build a scene of your own with the Root "
                + "and a few buttons - the probe used while the module was written was exactly "
                + "that, deleted once the presets had been felt.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("A step in a sequence");
            painter.Paragraph(
                "Where a haptic is one fixed step of a flow, bind the module's own PlayHapticCommand "
                + "and give it the preset where the step is bound. The flow then reads from the "
                + "Context - which preset, after which step - without opening a Command to find the "
                + "Play call. It is the same shape as DispatchSignalCommand bound with its signal.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.LevelCompleted)\n"
                + "    .ToSequence<GrantRewardCommand>()\n"
                + "    .ToSequence<PlayHapticCommand>(HapticPreset.Success);");
            painter.Paragraph(
                "PlayHapticCommand is a Command<HapticPreset>: the value in the binding reaches its "
                + "Execute(HapticPreset). A step before it may also choose the preset at runtime and "
                + "hand it on with Release(preset) instead.");

            painter.SubHeading("A call from a Command");
            painter.Paragraph(
                "Where the preset is part of a decision - the same coin is a LightImpact when it "
                + "lands and a Success when it completes the set - the game's own Command injects "
                + "the service and calls Play at the point where it decided. Nothing happens while "
                + "haptics are off, for None, or on a platform with nothing to vibrate; none of "
                + "those is an error.");
            painter.Code(
                "[Inject] private IHapticService _haptics { get; set; }\n"
                + "\n"
                + "public override void Execute()\n"
                + "{\n"
                + "    _playerModel.AddCurrency(_amount);\n"
                + "    _haptics.Play(_amount >= _setSize ? HapticPreset.Success : HapticPreset.LightImpact);\n"
                + "}");
            painter.Note(
                "Important: a Mediator may inject nothing but its View, so a button that should "
                + "click dispatches a signal and a Command plays the preset. A Play call written in "
                + "a Mediator is refused at runtime with the injection error, not by the compiler.");

            painter.SubHeading("The on/off choice");
            painter.Paragraph(
                "A settings screen reads IsEnabled to draw its toggle and dispatches the new value "
                + "on a Signal<bool>. The module's SetHapticsEnabledCommand reads that bool off the "
                + "signal and calls SetEnabled, so the sequence needs no Command of the game's own. "
                + "The choice is stored at once in PlayerPrefs and applies from the next Play; "
                + "turning haptics off also stops whatever is vibrating.");
            painter.Code(
                "CommandBinder.Bind(_signals.HapticsToggled)\n"
                + "    .ToSequence<SetHapticsEnabledCommand>();");

            painter.SubHeading("Reading the flow");
            painter.Paragraph(
                "Every call crosses the module's internal signals into a step of its own - "
                + "PlayPresetCommand, ApplyHapticsEnabledCommand - so a haptic shows in the Flow "
                + "Console as a step rather than a method that vanishes into a Service.");
        }

        private void DrawPresets(HelpPainter painter)
        {
            painter.SubHeading("The nine presets");
            painter.Paragraph(
                "What each one is on iOS, and what Android plays for it: a pulse of so many "
                + "milliseconds at an amplitude of 0..1 of the device's range, or a ramp - the "
                + "envelope interpolated every 25 ms.");
            painter.Bullet("Selection: UISelectionFeedbackGenerator on iOS; 40 ms at 0.471 on Android.");
            painter.Bullet("LightImpact: UIImpactFeedbackStyleLight; 40 ms at 0.156.");
            painter.Bullet("MediumImpact: UIImpactFeedbackStyleMedium; 120 ms at 0.471.");
            painter.Bullet("RigidImpact: UIImpactFeedbackStyleRigid, iOS 13 and up, Heavy below it; 40 ms at 1.0.");
            painter.Bullet("HeavyImpact: UIImpactFeedbackStyleHeavy; 160 ms at 1.0.");
            painter.Bullet("SoftImpact: UIImpactFeedbackStyleSoft, iOS 13 and up, Light below it; 160 ms at 0.156.");
            painter.Bullet("Success: UINotificationFeedbackTypeSuccess; a 240 ms ramp in 8 steps.");
            painter.Bullet("Warning: UINotificationFeedbackTypeWarning; a 280 ms ramp in 9 steps.");
            painter.Bullet("Failure: UINotificationFeedbackTypeError; a 480 ms ramp in 16 steps.");
            painter.Space();

            painter.SubHeading("What Android actually plays");
            painter.Paragraph(
                "The envelope goes to the Vibrator as one waveform - createWaveform with the step "
                + "timings and amplitudes - and a device with amplitude control plays it as written. "
                + "What the actuator then does with it is the firmware's, and on a OnePlus (Android "
                + "16) the firmware performs every step of about 100 ms or under as its own prebaked "
                + "45 ms click, scaling only the strength. Light, Selection and Rigid are then one "
                + "click at three strengths, and Medium at Lofelt's 80 ms was the same click as "
                + "Selection - which is why Medium runs 120 ms here: over that threshold it plays as "
                + "the real waveform, apart from Light below it and from the 160 ms Heavy above it. "
                + "Heavy and Soft play as waveforms on that phone too, and the three notification "
                + "ramps become a run of clicks - alike between Success and Warning, longer for "
                + "Failure.");
            painter.Note(
                "Light and Selection stay one click apart in strength on such a phone, by design: "
                + "pushing Selection over the threshold would turn a tick into a buzz. Android 10's "
                + "predefined effects were measured on the same phone and gained nothing - TICK felt "
                + "as Light, CLICK weaker than Medium, HEAVY_CLICK a double hit - and it reports no "
                + "Android 12 primitives, so neither road is taken. A Pixel plays the envelopes as "
                + "written, and every phone feels a little different; that is Android haptics, and "
                + "no vendor asset gets past it either.");

            painter.SubHeading("Devices with less");
            painter.Paragraph(
                "Without amplitude control - Android 7 and under, or a vibrator that reports none - "
                + "the envelope's breakpoint times play as an off/on pattern at full strength, so a "
                + "Light and a Heavy differ only in length. With no vibrator at all, Play returns "
                + "and nothing is logged, because there is nothing wrong to report.");
        }
    }
}

#endif