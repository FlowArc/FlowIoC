namespace Modules.AudioModule.Constants
{
    internal static class AudioConstants
    {
        /// <summary>Where under a module's Resources its bank sits: Resources/Audio/CD_AudioBank.asset.</summary>
        public const string BANK_RESOURCES_FOLDER = "Audio";

        public const string PREFS_MUSIC_ENABLED = "flowioc.audio.music.enabled";
        public const string PREFS_MUSIC_VOLUME = "flowioc.audio.music.volume";
        public const string PREFS_SFX_ENABLED = "flowioc.audio.sfx.enabled";
        public const string PREFS_SFX_VOLUME = "flowioc.audio.sfx.volume";

        /// <summary>The level an exposed volume takes for silence; the mixer's own floor.</summary>
        public const float SILENT_DECIBELS = -80f;
    }
}
