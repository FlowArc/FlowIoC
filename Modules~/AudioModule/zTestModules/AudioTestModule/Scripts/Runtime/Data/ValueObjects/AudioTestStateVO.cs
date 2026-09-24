#if UNITY_EDITOR

namespace Modules.AudioModule.AudioTestModule.Data.ValueObjects
{
    public readonly struct AudioTestStateVO
    {
        public readonly bool MusicOn;
        public readonly float MusicVolume;
        public readonly bool SfxOn;
        public readonly float SfxVolume;
        public readonly bool BankLoaded;

        public AudioTestStateVO(bool musicOn, float musicVolume, bool sfxOn, float sfxVolume, bool bankLoaded)
        {
            MusicOn = musicOn;
            MusicVolume = musicVolume;
            SfxOn = sfxOn;
            SfxVolume = sfxVolume;
            BankLoaded = bankLoaded;
        }
    }
}

#endif
