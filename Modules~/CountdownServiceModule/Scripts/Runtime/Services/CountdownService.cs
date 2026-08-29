using System;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CountdownServiceModule.Data.ValueObjects;
using Modules.CountdownServiceModule.Models;
using Modules.CountdownServiceModule.Signals;

namespace Modules.CountdownServiceModule.Services
{
    /// <summary>
    /// The module's public surface. It validates what it is asked for and hands the work to the
    /// commands through the internal signals, so every change to a countdown is a step the Flow
    /// Console can show rather than a method call buried in here.
    /// </summary>
    public class CountdownService : ICountdownService
    {
        [Inject] private ICountdownModel _countdownModel { get; set; }
        [InjectSignal] private CountdownServiceSignalsInternal _signals { get; set; }

        public bool IsActive() => _countdownModel.IsActive;

        public DateTime? GetTime() => IsActive() ? _countdownModel.Time : (DateTime?) null;

        public bool CountDownFrom(string id, int duration, DateTime startTime, Action<bool> checkActive,
            bool isPercentageTick = false, Action<float> countdownTick = null, Action countdownComplete = null,
            Action countdownStop = null, Action<float> elapsedTimeTick = null)
        {
            return CountDownFrom(new CountdownRequestVO
            {
                Id = id,
                Duration = duration,
                StartTime = startTime,
                IsPercentageTick = isPercentageTick,
                CheckActive = checkActive,
                CountdownTick = countdownTick,
                ElapsedTimeTick = elapsedTimeTick,
                CountdownComplete = countdownComplete,
                CountdownStop = countdownStop
            });
        }

        public bool CountDownFrom(CountdownRequestVO request)
        {
            if (request == null || string.IsNullOrEmpty(request.Id)) return false;
            if (request.Duration <= 0) return false;

            Start(request);
            return true;
        }

        public DateTime? CountDownInstantly(string id, int duration, bool isPercentageTick = false,
            Action<float> countdownTick = null, Action countdownComplete = null,
            Action countdownStop = null, Action<float> elapsedTimeTick = null)
        {
            // Starting "now" needs a now to start from, and there is none until a time source has
            // answered. The caller is told so rather than handed the device clock behind its back.
            if (!IsActive()) return null;

            DateTime now = _countdownModel.Time;

            bool started = CountDownFrom(id, duration, now, null, isPercentageTick,
                countdownTick, countdownComplete, countdownStop, elapsedTimeTick);

            return started ? now : (DateTime?) null;
        }

        public void AddCallbacks(string id, Action<float> countdownTick = null, bool isPercentageTick = false,
            Action countdownComplete = null, Action countdownStop = null, Action<float> elapsedTimeTick = null)
        {
            _signals.AddCallbacks.Dispatch(new CountdownRequestVO
            {
                Id = id,
                IsPercentageTick = isPercentageTick,
                CountdownTick = countdownTick,
                ElapsedTimeTick = elapsedTimeTick,
                CountdownComplete = countdownComplete,
                CountdownStop = countdownStop
            });
        }

        public void RemoveCallbacks(string id, Action<float> countdownTick = null, bool isPercentageTick = false,
            Action countdownComplete = null, Action countdownStop = null, Action<float> elapsedTimeTick = null)
        {
            _signals.RemoveCallbacks.Dispatch(new CountdownRequestVO
            {
                Id = id,
                IsPercentageTick = isPercentageTick,
                CountdownTick = countdownTick,
                ElapsedTimeTick = elapsedTimeTick,
                CountdownComplete = countdownComplete,
                CountdownStop = countdownStop
            });
        }

        public bool EvaluateElapsedTime(string id, DateTime startTime, Action<bool> checkActive, int duration = 0,
            Action<float> elapsedTimeTick = null, Action countdownComplete = null, Action countdownStop = null)
        {
            return EvaluateElapsedTime(new CountdownRequestVO
            {
                Id = id,
                Duration = duration,
                StartTime = startTime,
                IsPercentageTick = false,
                CheckActive = checkActive,
                CountdownTick = null,
                ElapsedTimeTick = elapsedTimeTick,
                CountdownComplete = countdownComplete,
                CountdownStop = countdownStop
            });
        }

        public bool EvaluateElapsedTime(CountdownRequestVO request)
        {
            if (request == null || string.IsNullOrEmpty(request.Id)) return false;

            // Measuring forward from a moment that has not happened yet would count backwards.
            // While the module is not active there is no time to compare against, so the request
            // is taken on trust and checked again by the command once the clock exists.
            if (IsActive() && request.StartTime >= _countdownModel.Time) return false;

            Start(request);
            return true;
        }

        public void Stop(string id) => _signals.StopCountdown.Dispatch(id);

        /// <summary>
        /// Hands the request to the commands, then tells the caller whether it is already
        /// running. A caller told false hears again through the same callback once a time source
        /// answers, which is how a countdown asked for during loading starts on its own.
        /// </summary>
        private void Start(CountdownRequestVO request)
        {
            _signals.AddCountdownData.Dispatch(request);

            request.CheckActive?.Invoke(IsActive());
        }
    }
}
