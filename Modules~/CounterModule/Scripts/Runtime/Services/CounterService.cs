using System;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CounterModule.Data.ValueObjects;
using Modules.CounterModule.Models;
using Modules.CounterModule.Shared.Signals;
using Modules.CounterModule.Signals;

namespace Modules.CounterModule.Services
{
    /// <summary>
    /// The module's public surface. It validates what it is asked for and hands the work to the
    /// commands through the internal signals, so every change to a counter is a step the Flow
    /// Console can show rather than a method call buried in here.
    /// </summary>
    public class CounterService : ICounterService
    {
        [Inject] private ICounterModel _counterModel { get; set; }
        [InjectSignal] private CounterServiceInternalSignals _signals { get; set; }

        public bool IsActive() => _counterModel.IsActive;

        public DateTime? GetTime() => IsActive() ? _counterModel.Time : (DateTime?) null;

        public bool CountDownFrom(string id, int duration, DateTime startTime, Action<bool> checkActive,
            bool isPercentageTick = false, Action<float> counterTick = null, Action counterComplete = null,
            Action counterStop = null, Action<float> elapsedTimeTick = null)
        {
            return CountDownFrom(new CounterRequestVO
            {
                Id = id,
                Duration = duration,
                StartTime = startTime,
                IsPercentageTick = isPercentageTick,
                CheckActive = checkActive,
                CounterTick = counterTick,
                ElapsedTimeTick = elapsedTimeTick,
                CounterComplete = counterComplete,
                CounterStop = counterStop
            });
        }

        public bool CountDownFrom(CounterRequestVO request)
        {
            if (request == null || string.IsNullOrEmpty(request.Id)) return false;
            if (request.Duration <= 0) return false;

            Start(request);
            return true;
        }

        public DateTime? CountDownInstantly(string id, int duration, bool isPercentageTick = false,
            Action<float> counterTick = null, Action counterComplete = null,
            Action counterStop = null, Action<float> elapsedTimeTick = null)
        {
            // Starting "now" needs a now to start from, and there is none until a time source has
            // answered. The caller is told so rather than handed the device clock behind its back.
            if (!IsActive()) return null;

            DateTime now = _counterModel.Time;

            bool started = CountDownFrom(id, duration, now, null, isPercentageTick,
                counterTick, counterComplete, counterStop, elapsedTimeTick);

            return started ? now : (DateTime?) null;
        }

        public void AddCallbacks(string id, Action<float> counterTick = null, bool isPercentageTick = false,
            Action counterComplete = null, Action counterStop = null, Action<float> elapsedTimeTick = null)
        {
            _signals.AddCallbacks.Dispatch(new CounterRequestVO
            {
                Id = id,
                IsPercentageTick = isPercentageTick,
                CounterTick = counterTick,
                ElapsedTimeTick = elapsedTimeTick,
                CounterComplete = counterComplete,
                CounterStop = counterStop
            });
        }

        public void RemoveCallbacks(string id, Action<float> counterTick = null, bool isPercentageTick = false,
            Action counterComplete = null, Action counterStop = null, Action<float> elapsedTimeTick = null)
        {
            _signals.RemoveCallbacks.Dispatch(new CounterRequestVO
            {
                Id = id,
                IsPercentageTick = isPercentageTick,
                CounterTick = counterTick,
                ElapsedTimeTick = elapsedTimeTick,
                CounterComplete = counterComplete,
                CounterStop = counterStop
            });
        }

        public bool CountUpFrom(string id, DateTime startTime, Action<bool> checkActive, int duration = 0,
            Action<float> elapsedTimeTick = null, Action counterComplete = null, Action counterStop = null)
        {
            return CountUpFrom(new CounterRequestVO
            {
                Id = id,
                Duration = duration,
                StartTime = startTime,
                IsPercentageTick = false,
                CheckActive = checkActive,
                CounterTick = null,
                ElapsedTimeTick = elapsedTimeTick,
                CounterComplete = counterComplete,
                CounterStop = counterStop
            });
        }

        public bool CountUpFrom(CounterRequestVO request)
        {
            if (request == null || string.IsNullOrEmpty(request.Id)) return false;

            // Measuring forward from a moment that has not happened yet would count backwards.
            // While the module is not active there is no time to compare against, so the request
            // is taken on trust and checked again by the command once the clock exists.
            if (IsActive() && request.StartTime >= _counterModel.Time) return false;

            Start(request);
            return true;
        }

        public void Stop(string id) => _signals.StopCounter.Dispatch(id);

        /// <summary>
        /// Hands the request to the commands, then tells the caller whether it is already
        /// running. A caller told false hears again through the same callback once a time source
        /// answers, which is how a counter asked for during loading starts on its own.
        /// </summary>
        private void Start(CounterRequestVO request)
        {
            _signals.AddCounterData.Dispatch(request);

            request.CheckActive?.Invoke(IsActive());
        }
    }
}
