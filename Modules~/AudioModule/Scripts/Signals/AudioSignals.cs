using FlowIoC.BaseModule.Signals;

namespace Modules.AudioModule.Signals
{
    /// <summary>
    /// What a Connector hands the module from elsewhere in the game: a settings screen's toggles
    /// and sliders, and an ad that needs the sound held while it plays. Playing a sound is not
    /// here - that is <c>IAudioService.Commands</c>, bound as a step by the module that plays it.
    /// </summary>
    public class AudioSignals : ISignalHolder
    {
        public AudioSignalsIncoming Incoming = new();
        public AudioSignalsOutgoing Outgoing = new();
    }

    public class AudioSignalsIncoming
    {
        /// <summary>The player turned music on or off. Stored, and applied at once.</summary>
        public Signal<bool> SetMusicEnabled = new();

        /// <summary>The player turned sound effects on or off. Stored, and applied at once.</summary>
        public Signal<bool> SetSfxEnabled = new();

        /// <summary>The player moved the music level, 0 to 1. Stored, and applied at once.</summary>
        public Signal<float> SetMusicVolume = new();

        /// <summary>The player moved the sound effects level, 0 to 1. Stored, and applied at once.</summary>
        public Signal<float> SetSfxVolume = new();

        /// <summary>Hold every sound where it is - an ad opened. Counted: two Mutes need two Unmutes.</summary>
        public Signal Mute = new();

        /// <summary>Let the sound go on from where it was held.</summary>
        public Signal Unmute = new();
    }

    public class AudioSignalsOutgoing
    {
    }
}
