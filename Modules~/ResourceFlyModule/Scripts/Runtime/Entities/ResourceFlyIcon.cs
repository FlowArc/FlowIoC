using System;
using FlowIoC.PoolModule.Entities;
using Modules.ResourceFlyModule.Data.UnityObjects;
using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Entities
{
    /// <summary>
    /// One icon of a flight. It waits its turn hidden, then plays its motion towards the target
    /// wherever the target is that frame, and goes back to the pool. Its clock is the look of it
    /// and decides nothing: it reports arrived, or lost when the target is gone or it is returned
    /// on the way. Unscaled time, so a paused game still pays out.
    /// </summary>
    public class ResourceFlyIcon : PoolableItem
    {
        private float _clock;
        private Vector3 _from;
        private int _seed;
        private RectTransform _target;
        private Action<bool> _done;

        internal bool IsFlying { get; private set; }
        internal CD_ResourceFlyMotion Motion { get; private set; }

        /// <summary>Starts the icon; done(true) when it reaches the target, done(false) if it is lost before.</summary>
        public void Launch(Vector3 from, RectTransform target, CD_ResourceFlyMotion motion, float delay, Action<bool> done)
        {
            _from = from;
            _target = target;
            Motion = motion;
            _done = done;
            _seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            transform.position = from;
            transform.localScale = Vector3.zero;
            _clock = -delay;
            IsFlying = true;
        }

        private void Update() => Step(Time.unscaledDeltaTime);

        /// <summary>One frame of the flight.</summary>
        internal void Step(float deltaTime)
        {
            if (!IsFlying)
                return;

            _clock += deltaTime;

            if (_clock < 0f)
                return;

            if (_target == null)
            {
                Finish(false);
                return;
            }

            float seconds = Motion.Seconds;
            float t = seconds <= 0f ? 1f : Mathf.Clamp01(_clock / seconds);
            float scale = transform.parent != null ? transform.parent.lossyScale.x : 1f;
            var path = new ResourceFlyPathVO(_from, _target.position, _seed, scale);
            transform.position = Motion.Evaluate(path, t);
            transform.localScale = Vector3.one * Motion.EvaluateScale(path, t);

            if (t >= 1f)
                Finish(true);
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();

            // Returned by somebody else mid-flight: its share is reported lost, once.
            if (IsFlying)
                Report(false);
        }

        /// <summary>
        /// Switched off mid-flight - the screen it flies in hid, or went with its counter: nothing
        /// will step it again, so its share is reported lost now, once.
        /// </summary>
        private void OnDisable()
        {
            if (IsFlying)
                Report(false);
        }

        /// <summary>Back to the pool first, then reported: whatever the report sets going cannot leave the icon on screen.</summary>
        private void Finish(bool arrived)
        {
            Action<bool> done = TakeDone();
            Dismiss();
            done?.Invoke(arrived);
        }

        private void Report(bool arrived) => TakeDone()?.Invoke(arrived);

        private Action<bool> TakeDone()
        {
            IsFlying = false;
            Action<bool> done = _done;
            _done = null;
            return done;
        }
    }
}