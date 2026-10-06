using System;
using Modules.ResourceFlyModule.Data.UnityObjects;
using Modules.ResourceFlyModule.Data.ValueObjects;
using Modules.ResourceFlyModule.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.ResourceFlyModule.Entities
{
    /// <summary>
    /// The ready counter: put it on a screen beside the counter's icon, with the icons' parent just
    /// below the icon in the hierarchy so they pass under it and its landing is seen over them, and
    /// register it from the Command that opens the screen. It counts up to each landing's value and
    /// plays a landing on the icon - a punch, a flash; its three events are for the screen - a
    /// badge to slide in, a haptic, a sound, a particle.
    /// </summary>
    public class ResourceFlyCounterDisplay : MonoBehaviour, IResourceFlyCounter
    {
        [SerializeField] [Tooltip("The counter's icon: where the icons land, and what a landing scales.")]
        private RectTransform _target;

        [SerializeField] [Tooltip("Optional: what a landing's colour is laid on - the counter icon's Image. Empty: landings change no colour.")]
        private Graphic _tinted;

        [SerializeField] [Tooltip("Optional: the count the icons add to. Empty: the counter shows no number - an inventory box - and its landings still play.")]
        private TMP_Text _count;

        [SerializeField] [Tooltip("What the icons fly in. Place it just below the target in the hierarchy.")]
        private RectTransform _iconParent;

        [SerializeField] [Tooltip("The pool item the icons are, when a flight's look names none.")]
        private string _iconPoolKey;

        [SerializeField] [Tooltip("Optional: the motion flights into this counter play when their look names none. Empty plays CD_ResourceFly's.")]
        private CD_ResourceFlyMotion _motion;

        [SerializeField] [Tooltip("Optional: how this counter answers a landing when the flight's look names none. Empty plays CD_ResourceFly's.")]
        private CD_ResourceFlyLanding _landing;

        [SerializeField] [Min(0)] [Tooltip("Optional: the most icons one flight into this counter uses when its look names none. 0 uses CD_ResourceFly's.")]
        private int _maxIcons;

        [SerializeField]
        [Min(0)]
        [Tooltip("Optional: how much one icon into this counter carries when the flight's look names none - 100 flies 1000 "
                 + "in ten icons. 0 uses CD_ResourceFly's.")]
        private int _unitsPerIcon;

        private int _flights;
        private bool _endPending;
        private float _countFrom;
        private int _countTo;
        private float _countClock;
        private float _countSeconds;
        private float _landingClock = -1f;
        private CD_ResourceFlyLanding _playing;
        private Vector3 _restScale = Vector3.one;
        private Color _restColor = Color.white;
        private bool _restRead;

        public event Action FlightStarted;
        public event Action Landed;
        public event Action FlightEnded;

        public RectTransform Target => _target;
        public RectTransform IconParent => _iconParent;
        public string IconPoolKey => _iconPoolKey;
        public CD_ResourceFlyMotion Motion => _motion;
        public CD_ResourceFlyLanding Landing => _landing;
        public int MaxIcons => _maxIcons;
        public int UnitsPerIcon => _unitsPerIcon;

        public void ShowValue(int value)
        {
            _countFrom = value;
            _countTo = value;
            _countClock = _countSeconds = 0f;

            if (_count != null)
                _count.text = value.ToString();
        }

        public void Land(int value, float countUpSeconds, CD_ResourceFlyLanding landing)
        {
            _countFrom = CurrentCount();
            _countTo = value;
            _countClock = 0f;
            _countSeconds = countUpSeconds;

            // Read only at rest: mid-landing the icon shows the landing, not its own look.
            if (_landingClock < 0f)
                ReadRest();

            _playing = landing;
            _landingClock = landing != null ? 0f : -1f;

            if (landing == null)
                ShowLanding(ResourceFlyLandingVO.Rest);

            Landed?.Invoke();
        }

        public void BeginFlight()
        {
            if (_flights++ > 0)
                return;

            // A flight starting while the last one's end waits is the same stretch of flying: no new start.
            if (_endPending)
                _endPending = false;
            else
                FlightStarted?.Invoke();
        }

        /// <summary>The last flight is down; FlightEnded waits until the count and the punch are at rest, so the total is seen before the counter goes.</summary>
        public void EndFlight()
        {
            if (_flights > 0 && --_flights == 0)
                _endPending = true;
        }

        private void Update() => Step(Time.unscaledDeltaTime);

        /// <summary>One frame of the count and the landing.</summary>
        internal void Step(float deltaTime)
        {
            if (_countSeconds > 0f && _countClock < _countSeconds)
            {
                _countClock += deltaTime;

                if (_count != null)
                    _count.text = Mathf.RoundToInt(CurrentCount()).ToString();
            }

            if (_landingClock >= 0f)
            {
                _landingClock += deltaTime;
                float seconds = _playing.Seconds;
                float t = seconds <= 0f ? 1f : Mathf.Clamp01(_landingClock / seconds);

                if (t >= 1f)
                {
                    // Back to rest exactly, whatever the landing's last frame said.
                    _landingClock = -1f;
                    ShowLanding(ResourceFlyLandingVO.Rest);
                }
                else
                    ShowLanding(_playing.Evaluate(t));
            }

            if (_endPending && IsAtRest())
                AnnounceEnd();
        }

        private bool IsAtRest() => (_countSeconds <= 0f || _countClock >= _countSeconds) && _landingClock < 0f;

        private void ShowLanding(ResourceFlyLandingVO look)
        {
            if (!_restRead)
                return;

            _target.localScale = _restScale * look.Scale;

            if (_tinted != null)
            {
                // The icon's own alpha stays: a tint changes the colour, never how visible the icon is.
                Color tinted = Color.Lerp(_restColor, look.Tint, look.Tint.a);
                tinted.a = _restColor.a;
                _tinted.color = tinted;
            }
        }

        private void AnnounceEnd()
        {
            _endPending = false;
            FlightEnded?.Invoke();
        }

        private float CurrentCount() =>
            _countSeconds <= 0f ? _countTo : Mathf.Lerp(_countFrom, _countTo, Mathf.Clamp01(_countClock / _countSeconds));

        private void ReadRest()
        {
            _restScale = _target.localScale;

            if (_tinted != null)
                _restColor = _tinted.color;

            _restRead = true;
        }

        private void OnDisable()
        {
            if (_landingClock >= 0f && _target != null)
                ShowLanding(ResourceFlyLandingVO.Rest);

            _landingClock = -1f;

            // Switched off - the screen hid or went: its flights are over, and their icons report lost.
            _flights = 0;

            // Switched off with an end waiting: nothing will tick it, so it is announced now.
            if (_endPending)
                AnnounceEnd();
        }
    }
}