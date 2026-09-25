using System.Collections;
using FlowIoC.BaseModule.Provider.Coroutine;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Data.ValueObjects;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Entities
{
    /// <summary>
    /// One AudioSource under AudioServiceRoot, reused for sound after sound. It knows which key it
    /// plays and when it started, so a full channel can take the oldest one back; the fades run
    /// on unscaled time, so a paused game still finishes a crossfade.
    /// </summary>
    internal class AudioVoice
    {
        private readonly ICoroutineProvider _coroutines;
        private Coroutine _fade;

        public AudioVoice(AudioSource source, AudioChannel channel, ICoroutineProvider coroutines)
        {
            Source = source;
            Channel = channel;
            _coroutines = coroutines;
        }

        public AudioSource Source { get; }

        public AudioChannel Channel { get; }

        public AudioKey Key { get; private set; }

        public float StartedAt { get; private set; }

        /// <summary>The level the sound plays at when no fade is running.</summary>
        public float Volume { get; private set; }

        /// <summary>
        /// Playing, or fading out on its way to silence. While AudioListener.pause holds the sound
        /// a source reports that it is not playing, so a clip part-way through counts as busy too.
        /// </summary>
        public bool IsBusy =>
            _fade != null
            || (Source != null && (Source.isPlaying || (AudioListener.pause && Source.clip != null && Source.timeSamples > 0)));

        /// <summary>
        /// A source that is off plays nothing and the voice stays free. Leaving play mode disables
        /// the sources a frame before the game stops asking for sounds, and Unity would answer each
        /// ask with a warning.
        /// </summary>
        public void Play(AudioKey key, AudioClip clip, AudioClipCVO sound, Vector3 position, float startVolume)
        {
            if (Source == null || !Source.isActiveAndEnabled)
                return;

            StopFade();

            Key = key;
            Volume = sound.Volume;
            StartedAt = Time.unscaledTime;

            Source.clip = clip;
            Source.loop = sound.Loop;
            Source.volume = startVolume;
            Source.spatialBlend = sound.SpatialBlend;
            Source.minDistance = sound.MinDistance;
            Source.maxDistance = Mathf.Max(sound.MinDistance, sound.MaxDistance);
            Source.transform.position = position;
            Source.Play();
        }

        /// <summary>Moves the level to <paramref name="target"/>; stops the source at the end when asked.</summary>
        public void FadeTo(float target, float seconds, bool stopAtEnd)
        {
            StopFade();

            if (seconds <= 0f)
            {
                Source.volume = target;

                if (stopAtEnd)
                    Stop();

                return;
            }

            _fade = _coroutines.StartCoroutine(Fade(target, seconds, stopAtEnd));
        }

        public void Stop()
        {
            StopFade();

            if (Source == null)
                return;

            Source.Stop();
            Source.clip = null;
            Key = default;
        }

        private IEnumerator Fade(float target, float seconds, bool stopAtEnd)
        {
            float from = Source.volume;
            float elapsed = 0f;

            while (elapsed < seconds && Source != null)
            {
                elapsed += Time.unscaledDeltaTime;
                Source.volume = Mathf.Lerp(from, target, elapsed / seconds);
                yield return null;
            }

            _fade = null;

            if (Source == null)
                yield break;

            Source.volume = target;

            if (stopAtEnd)
                Stop();
        }

        private void StopFade()
        {
            if (_fade == null)
                return;

            _coroutines.StopCoroutine(_fade);
            _fade = null;
        }
    }
}
