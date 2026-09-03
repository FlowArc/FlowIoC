using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CounterModule.Data.ValueObjects;
using Modules.CounterModule.Models;

namespace Modules.CounterModule.Controllers
{
    /// <summary>
    /// Puts a counter into the model, unless one is already running under that id or the time
    /// it was given has already passed.
    /// </summary>
    internal class AddCounterDataCommand : Command
    {
        [SignalParam] private CounterRequestVO _request { get; set; }
        [Inject] private ICounterModel _counterModel { get; set; }

        public override void Execute()
        {
            Retain();

            // A counter restored from saved state may have run out while the game was closed.
            // It completes at once, and the rest of the sequence is skipped so nothing is added
            // for a counter that is already over.
            if (HasAlreadyFinished())
            {
                _request.CounterComplete?.Invoke();
                Stop();
                return;
            }

            lock (_counterModel.LockObject)
            {
                // The first caller of an id decides how long it runs; a later one only adds its
                // callbacks, so an id already in the map is left exactly as it is.
                if (!_counterModel.DataMap.ContainsKey(_request.Id))
                {
                    _counterModel.DataMap.Add(_request.Id, new CounterVO
                    {
                        InitialTime = _request.StartTime,
                        Duration = TimeSpan.FromSeconds(_request.Duration)
                    });
                }
            }

            Release();
        }

        private bool HasAlreadyFinished()
        {
            // Without a clock there is nothing to measure against, and an entry with no duration
            // measures forward forever - neither can have run out.
            if (!_counterModel.IsActive) return false;
            if (_request.Duration <= 0) return false;

            return (_counterModel.Time - _request.StartTime).TotalSeconds > _request.Duration;
        }
    }
}
