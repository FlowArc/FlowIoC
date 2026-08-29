using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CountdownServiceModule.Data.ValueObjects;
using Modules.CountdownServiceModule.Models;

namespace Modules.CountdownServiceModule.Controllers
{
    /// <summary>
    /// Puts a countdown into the model, unless one is already running under that id or the time
    /// it was given has already passed.
    /// </summary>
    internal class AddCountdownDataCommand : Command
    {
        [SignalParam] private CountdownRequestVO _request { get; set; }
        [Inject] private ICountdownModel _countdownModel { get; set; }

        public override void Execute()
        {
            Retain();

            // A countdown restored from saved state may have run out while the game was closed.
            // It completes at once, and the rest of the sequence is skipped so nothing is added
            // for a countdown that is already over.
            if (HasAlreadyFinished())
            {
                _request.CountdownComplete?.Invoke();
                Stop();
                return;
            }

            lock (_countdownModel.LockObject)
            {
                // The first caller of an id decides how long it runs; a later one only adds its
                // callbacks, so an id already in the map is left exactly as it is.
                if (!_countdownModel.DataMap.ContainsKey(_request.Id))
                {
                    _countdownModel.DataMap.Add(_request.Id, new CountdownVO
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
            if (!_countdownModel.IsActive) return false;
            if (_request.Duration <= 0) return false;

            return (_countdownModel.Time - _request.StartTime).TotalSeconds > _request.Duration;
        }
    }
}
