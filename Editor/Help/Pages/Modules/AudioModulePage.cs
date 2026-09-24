#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The audio module: keys and banks that live in the module that plays them, the steps a
    /// Context binds, the mixer and its two settings, and why an update never meets the game's
    /// sounds.
    /// </summary>
    internal class AudioModulePage : ModulePage
    {
        public override string ModuleFolderName => "AudioModule";

        public override string Title => "Audio Module";

        public override string Subtitle => "Effects, music and 3D sound from banks each module owns, and the player's settings";

        public override FlowIcon Icon => FlowIcon.Speaker;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "Put the Root in the scene, keep an AudioListener on the camera, and give each module that plays sound its keys and a bank.",
                "Tools/FlowIoC-Modules/Audio/Panel writes a module's keys and bank inside that module. "
                + "Nothing a game authors goes into the Audio module's own folder."),
            new HelpTab("Usage", DrawUsage,
                "Bind IAudioService.Commands.Play as a step with its key; load a bank before the music that needs it.",
                "A sound is named where the step is bound - ToSequence<IAudioService.Commands.Play>"
                + "(AudioKey.Gameplay.Jump). A 3D sound takes its point off the signal. Settings and "
                + "a mute arrive through a Connector on the module's Incoming."),
            new HelpTab("Updates", DrawUpdates,
                "What an update of the module touches, and what it never touches.",
                "Keys and banks live in the modules that own them, and the game's mixer and settings "
                + "are filed on the Root in its scene. An update replaces the module's own files only.")
        };

        public override string InstalledHint =>
            "Drop AudioServiceRoot into your scene, then give a module sound from Tools/FlowIoC-Modules/Audio/Panel; the Setup tab has the steps.";

        public override string BodyHeadline =>
            "Each module plays its own sounds, by name, from a bank it keeps itself.";

        public override string BodyTagline =>
            "Gameplay knows it jumped, landed and died; so Gameplay declares those three keys, keeps "
            + "their clips, and binds the step that plays one. Audio knows how to play a sound, and "
            + "nothing about the game.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "AudioKey, one partial struct every module adds its keys to. AudioKey.Gameplay.Jump is "
                + "declared in Gameplay's own folder and compiled into Audio's Shared assembly, so "
                + "typing AudioKey. lists every module's sounds, and a misspelt key does not compile.");
            painter.Bullet(
                "Banks that live in the module that plays them - Resources/Audio/CD_AudioBank.asset - "
                + "found by the service on its own. A bank row picks its key from a list of the keys "
                + "the code declares; a key with no row, or a row with no clip, is reported at start.");
            painter.Bullet(
                "Sound effects on pooled voices per channel - Sfx, Ui, Ambient - with a limit each and "
                + "a rule for a full channel: the oldest voice gives way, or the new sound is dropped.");
            painter.Bullet(
                "Variants: a row with several clips plays one at random and never the same one twice "
                + "in a row. A minimum interval keeps a sound fired every frame from stacking.");
            painter.Bullet(
                "Music on two voices of its own, crossfading from one track to the next. Asking for "
                + "the track already playing leaves it alone.");
            painter.Bullet(
                "3D sound: a row's spatial blend and distances say how it carries, and PlayAt plays it "
                + "from a point in the world.");
            painter.Bullet(
                "A mixer, Mixer_Audio, with the player's music and sound levels and three snapshots - "
                + "Default, Ducked, Paused. Levels and on/off are stored in PlayerPrefs under flowioc.audio.");
            painter.Bullet(
                "Mute and Unmute for an ad, counted: two Mutes need two Unmutes.");
            painter.Bullet(
                "An optional click for every Button: ButtonClickSound under the Root plays one key for "
                + "any button pressed; SuppressClickSound on a button leaves it out.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.Audio and Modules.Audio.Shared "
                + "and binds the steps under IAudioService.Commands directly. The panel adds both "
                + "references when it gives a module its keys.");

            painter.SubHeading("Trying it out");
            painter.Paragraph(
                "Open AudioTestScene under the test module's Scenes folder and press Play. Theme A starts "
                + "after the test module's bank loads; the buttons play a two-variant beep, a boom to "
                + "the left and to the right, the other theme, the snapshots and an unload, and the "
                + "toggles and sliders drive the settings and a mute. AudioTestContext is the sample: "
                + "each button's flow is the binding a game would write.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("1. The Root");
            painter.Paragraph(
                "Drop AudioServiceRoot from the module's Prefabs folder into the scene. It ships at "
                + "Initialize Order -55, in the Service band, and files the module's CD_AudioSettings, "
                + "which is wired to Mixer_Audio.");
            painter.Note(
                "Important: nothing is heard without an AudioListener, and the module does not add one. "
                + "Keep the one on the game's camera. A 3D sound is heard from wherever that listener is.");

            painter.SubHeading("2. A module's keys and bank");
            painter.Paragraph(
                "Open Tools/FlowIoC-Modules/Audio/Panel, pick the module under Give a module sound, and "
                + "press Add audio keys and a bank. Inside that module it writes Scripts/AudioKeys/ "
                + "with AudioKey.<Module>.cs and the Modules.Audio.Shared.asmref beside it, and "
                + "Resources/Audio/CD_AudioBank.asset, and adds Modules.Audio and Modules.Audio.Shared "
                + "to the module's assembly.");
            painter.Code(
                "public readonly partial struct AudioKey\n"
                + "{\n"
                + "    public static class Gameplay\n"
                + "    {\n"
                + "        public static readonly AudioKey Jump = new(\"GameplayModule/Jump\");\n"
                + "        public static readonly AudioKey Die  = new(\"GameplayModule/Die\");\n"
                + "    }\n"
                + "}");
            painter.Paragraph(
                "A key's id starts with the module's name - that is the bank it loads from. Then add a "
                + "row per key to the bank: pick the key, drop in one clip or several, set the channel, "
                + "the level and, for a 3D sound, the spatial blend.");
            painter.Note(
                "Important: a bank's clips load through Addressables. Picking a clip in the bank makes "
                + "it addressable, and a bank that arrives with an installed module has its clips put "
                + "into an Addressables group called Audio when it is imported. A clip taken out of "
                + "Addressables by hand stops loading, and LoadBank reports it.");

            painter.SubHeading("3. Your own mixer or settings");
            painter.Paragraph(
                "To change the voices, the fade times or the mixer, make a CD_AudioSettings of your own "
                + "in your own folder and put it in the Scriptables of the AudioServiceRoot in your "
                + "scene. Do not edit the one in the module: an update would have to choose between "
                + "your edit and its own. A mixer of your own exposes its two levels as MusicVolume and "
                + "SfxVolume, or names them on the settings.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("A sound as a step");
            painter.Paragraph(
                "Bind the module's own step and give it the key where the step is bound; the flow reads "
                + "from the Context, which sound after which step.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.Jumped)\n"
                + "    .ToSequence<ApplyJumpCommand>()\n"
                + "    .ToSequence<IAudioService.Commands.Play>(AudioKey.Gameplay.Jump);");

            painter.SubHeading("A sound at a point");
            painter.Paragraph(
                "PlayAt takes the key where it is bound and the point off the signal the sequence is "
                + "bound to, which carries a Vector3.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.Exploded)   // Signal<Vector3>\n"
                + "    .ToSequence<IAudioService.Commands.PlayAt>(AudioKey.Gameplay.Boom);");

            painter.SubHeading("A bank, then its music");
            painter.Paragraph(
                "A bank ticked Preload At Boot is loading from the start. Any other is loaded by a step "
                + "that holds the sequence until its clips are in, so the music after it has something "
                + "to play; UnloadBank hands them back when the module is done.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.EnterLevel)\n"
                + "    .ToSequence<IAudioService.Commands.LoadBank>(FlowModule.GameplayModule)\n"
                + "    .ToSequence<IAudioService.Commands.PlayMusic>(AudioKey.Gameplay.LevelTheme)\n"
                + "    .ToSequence<IAudioService.Commands.ApplySnapshot>(AudioSnapshot.Default);");
            painter.Note(
                "Important: a sound asked for before its bank is loaded does not play. The Flow Console "
                + "says so on the Audio channel, naming the key and the bank.");

            painter.SubHeading("Settings and an ad");
            painter.Paragraph(
                "A settings screen and an ad reach the module through a Connector, on its Incoming: "
                + "SetMusicEnabled, SetSfxEnabled, SetMusicVolume and SetSfxVolume take the player's "
                + "choice and store it; Mute and Unmute hold every sound where it is.");
            painter.Code(
                "_settingsSignals.Outgoing.MusicToggled.Connect(_audioSignals.Incoming.SetMusicEnabled);\n"
                + "_adsSignals.Outgoing.Opened.Connect((_, _) => _audioSignals.Incoming.Mute.Dispatch());\n"
                + "_adsSignals.Outgoing.Closed.Connect((_, _) => _audioSignals.Incoming.Unmute.Dispatch());");

            painter.SubHeading("A call from a Command");
            painter.Paragraph(
                "Where the sound is part of a decision, the game's Command injects IAudioService and "
                + "calls Play, PlayAt or PlayMusic at the point where it decided. IsEnabled and "
                + "GetVolume give a settings screen what to draw.");
        }

        private void DrawUpdates(HelpPainter painter)
        {
            painter.SubHeading("What an update replaces");
            painter.Paragraph(
                "The module's own folder: its code, Mixer_Audio, the shipped CD_AudioSettings and the "
                + "AudioServiceRoot prefab. None of those is the game's, so none of them meets an edit "
                + "of the game's.");

            painter.SubHeading("What an update never touches");
            painter.Bullet(
                "Every module's Scripts/AudioKeys/ and Resources/Audio/CD_AudioBank.asset - they live "
                + "in the module that owns them, and the update looks only at the Audio module's folder.");
            painter.Bullet(
                "A CD_AudioSettings or a mixer of the game's own, filed on the Root in its scene: a "
                + "prefab override stays with the scene when the prefab is replaced.");
            painter.Note(
                "Important: an edit made inside the Audio module's folder - to Mixer_Audio or its "
                + "CD_AudioSettings - is one the update has to ask about. Put your own copy on the Root "
                + "instead, and the update has nothing to ask.");
        }
    }
}

#endif
