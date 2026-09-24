using System;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Services
{
    /// <summary>
    /// The module's one counterpart. A module that plays sound declares its keys in AudioKey,
    /// keeps its clips in a bank of its own, and plays them through here - usually as a step,
    /// <c>.ToSequence&lt;IAudioService.Commands.Play&gt;(AudioKey.Gameplay.Jump)</c>, so the flow
    /// reads from its Context. The steps sit under <see cref="Commands"/>, one file each beside
    /// this one.
    /// </summary>
    public partial interface IAudioService
    {
        /// <summary>Plays a sound effect flat, or 3D at the Root when its row says 3D.</summary>
        void Play(AudioKey key);

        /// <summary>Plays a sound effect from a point in the world; its row's spatial blend decides how 3D it sounds.</summary>
        void PlayAt(AudioKey key, Vector3 position);

        /// <summary>Crossfades the music to this track. The track already playing is left alone.</summary>
        void PlayMusic(AudioKey key);

        /// <summary>Fades the music out.</summary>
        void StopMusic();

        /// <summary>Loads a module's bank, named as FlowModule names the module, and answers true once it is ready.</summary>
        void LoadBank(string module, Action<bool> done = null);

        /// <summary>Stops the bank's sounds and hands its clips back.</summary>
        void UnloadBank(string module);

        bool IsBankLoaded(string module);

        /// <summary>Moves the mixer to a snapshot - <see cref="AudioSnapshot.Ducked"/>, or one of the game's own mixer.</summary>
        void ApplySnapshot(AudioSnapshot snapshot);

        /// <summary>The player's choice. True until a game turns it off.</summary>
        bool IsEnabled(AudioBus bus);

        /// <summary>The player's level, 0 to 1. Full until a game moves it.</summary>
        float GetVolume(AudioBus bus);

        /// <summary>Stores the choice and applies it.</summary>
        void SetEnabled(AudioBus bus, bool on);

        /// <summary>Stores the level, 0 to 1, and applies it.</summary>
        void SetVolume(AudioBus bus, float volume);

        /// <summary>Holds every sound where it is. Counted: two Mutes need two Unmutes.</summary>
        void Mute();

        void Unmute();

        /// <summary>
        /// The steps a game binds in a sequence of its own. They sit inside the interface so that
        /// the one name a game knows - the Service it injects - is also where its steps are found,
        /// and the flow reads from the Context: which sound, after which step. Each step is a file
        /// of its own, <c>IAudioService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}
