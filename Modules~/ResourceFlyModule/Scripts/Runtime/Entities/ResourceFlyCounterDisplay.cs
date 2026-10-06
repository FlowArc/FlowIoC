using System;
using Modules.ResourceFlyModule.Data.UnityObjects;
using Modules.ResourceFlyModule.Data.ValueObjects;
using Modules.ResourceFlyModule.Services;
using TMPro;
using UnityEngine;

namespace Modules.ResourceFlyModule.Entities
{
    /// <summary>
    /// The ready counter: put it on a screen beside the counter's icon, with the icons' parent just
    /// below the icon in the hierarchy so they pass under it and its punch is seen over them, and
    /// register it from the Command that opens the screen. It counts up to each landing's value and
    /// punches the icon; its three events are for the screen - a badge to slide in, a haptic, a sound.
    /// </summary>
    public class ResourceFlyCounterDisplay : MonoBehaviour, IResourceFlyCounter
    {
        [SerializeField] [Tooltip("The counter's icon: where the icons land, and what punches.")]
        private RectTransform _target;

        [SerializeField] [Tooltip("The count the icons add to.")]
        private TMP_Text _count;

        [SerializeField] [Tooltip("What the icons fly in. Place it just below the target in the hierarchy.")]
        private RectTransform _iconParent;

        [SerializeField] [Tooltip("The pool item the icons are.")]
        private string _iconPoolKey;

        [SerializeField] [Tooltip("Optional: the motion flights into this counter play. Empty plays CD_ResourceFly's default.")]
        private CD_ResourceFlyMotion _motion;

        private int _flights;
        private bool _endPending;
        private float _countFrom;
        private int _countTo;
        private float _countClock;
        private float _countSeconds;
        private float _punchClock = -1f;
        private ResourceFlyOptionsCVO _punch;
        private Vector3 _targetScale = Vector3.one;
        private bool _scaleRead;

        public event Action FlightStarted;
        public event Action Landed;
        public event Action FlightEnded;

        public RectTransform Target => _target;
        public RectTransform IconParent => _iconParent;
        public string IconPoolKey => _iconPoolKey;
        public CD_ResourceFlyMotion Motion => _motion;

        public void ShowValue(int value)
        {
            _countFrom = value;
            _countTo = value;
            _countClock = _countSeconds = 0f;
            _count.text = value.ToString();
        }

        public void Land(int value, ResourceFlyOptionsCVO options)
        {
            _countFrom = CurrentCount();
            _countTo = value;
            _countClock = 0f;
            _countSeconds = options.CountUpSeconds;

            ReadScale();
            _punch = options;
            _punchClock = 0f;

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

        private void Update()
        {
            if (_countSeconds > 0f && _countClock < _countSeconds)
            {
                _countClock += Time.unscaledDeltaTime;
                _count.text = Mathf.RoundToInt(CurrentCount()).ToString();
            }

            if (_punchClock >= 0f)
            {
                _punchClock += Time.unscaledDeltaTime;
                float t = _punch.PunchSeconds <= 0f ? 1f : Mathf.Clamp01(_punchClock / _punch.PunchSeconds);
                // Up and back down in one punch: 0 at both ends, the full scale in the middle.
                _target.localScale = _targetScale * Mathf.Lerp(1f, _punch.PunchScale, Mathf.Sin(t * Mathf.PI));

                if (t >= 1f)
                    _punchClock = -1f;
            }

            if (_endPending && IsAtRest())
                AnnounceEnd();
        }

        private bool IsAtRest() => (_countSeconds <= 0f || _countClock >= _countSeconds) && _punchClock < 0f;

        private void AnnounceEnd()
        {
            _endPending = false;
            FlightEnded?.Invoke();
        }

        private float CurrentCount() =>
            _countSeconds <= 0f ? _countTo : Mathf.Lerp(_countFrom, _countTo, Mathf.Clamp01(_countClock / _countSeconds));

        private void ReadScale()
        {
            if (_scaleRead)
                return;

            _targetScale = _target.localScale;
            _scaleRead = true;
        }

        private void OnDisable()
        {
            if (_scaleRead && _target != null)
                _target.localScale = _targetScale;

            _punchClock = -1f;

            // Switched off - the screen hid or went: its flights are over, and their icons report lost.
            _flights = 0;

            // Switched off with an end waiting: nothing will tick it, so it is announced now.
            if (_endPending)
                AnnounceEnd();
        }
    }
}