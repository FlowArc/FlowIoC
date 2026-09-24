#if UNITY_EDITOR

// ReSharper disable once CheckNamespace
namespace Modules.AudioModule.Shared
{
    /// <summary>
    /// The sounds AudioTestModule plays. This file lives in AudioTestModule and is compiled into
    /// Modules.Audio.Shared by the asmref beside it, so its keys join every other module's under
    /// AudioKey. Each id starts with "AudioTestModule/" - the bank the sound loads from, which is
    /// AudioTestModule/Resources/Audio/CD_AudioBank.asset.
    /// </summary>
    public readonly partial struct AudioKey
    {
        public static class AudioTest
        {
            public static readonly AudioKey Beep = new("AudioTestModule/Beep");
            public static readonly AudioKey Boom = new("AudioTestModule/Boom");
            public static readonly AudioKey Click = new("AudioTestModule/Click");
            public static readonly AudioKey ThemeA = new("AudioTestModule/ThemeA");
            public static readonly AudioKey ThemeB = new("AudioTestModule/ThemeB");
        }
    }
}

#endif
