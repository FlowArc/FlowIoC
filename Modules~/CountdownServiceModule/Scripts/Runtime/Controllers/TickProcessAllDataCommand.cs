using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using Modules.CountdownServiceModule.Data.ValueObjects;

namespace Modules.CountdownServiceModule.Controllers
{
    /// <summary>
    /// One second of work for every countdown: report the new time, or finish the ones that have
    /// run out. Runs behind <see cref="TimeTickCommand"/>, so the clock has already moved.
    /// </summary>
    [HideCommandLog]
    internal class TickProcessAllDataCommand : TickProcessBaseCommand
    {
        public override void Execute()
        {
            Retain();

            List<string> ids;
            var completedIds = new List<string>();

            // A callback is free to start or stop a countdown, which would change the map while
            // it was being walked. Copying the ids first means the walk is over before any
            // callback runs.
            lock (_countdownModel.LockObject)
            {
                ids = new List<string>(_countdownModel.DataMap.Keys);
            }

            foreach (string id in ids)
            {
                CountdownVO countdown;

                lock (_countdownModel.LockObject)
                {
                    if (!_countdownModel.DataMap.TryGetValue(id, out countdown))
                        continue;
                }

                Process(id, countdown, completedIds);
            }

            lock (_countdownModel.LockObject)
            {
                foreach (string completedId in completedIds)
                    _countdownModel.DataMap.Remove(completedId);
            }

            Release();
        }

        private void Process(string id, CountdownVO countdown, List<string> completedIds)
        {
            int elapsed = ElapsedSeconds(countdown);
            int remaining = RemainingSeconds(countdown);

            // An entry nobody asked a remaining time of is measuring forward, and finishes on
            // its duration only if it was given one. Everything else finishes at zero.
            bool isCompleted = countdown.IsElapsedOnly
                ? countdown.Duration.TotalSeconds > 0 && elapsed >= countdown.Duration.TotalSeconds
                : remaining <= 0;

            if (isCompleted)
            {
                foreach (var complete in countdown.CompleteCallbacks.ToArray())
                    complete.Invoke();

                countdown.Clear();
                completedIds.Add(id);
                return;
            }

            foreach (var tick in countdown.TickCallbacks.ToArray())
                tick.Invoke(remaining);

            float fraction = RemainingFraction(countdown, remaining);

            foreach (var tickPercentage in countdown.TickPercentageCallbacks.ToArray())
                tickPercentage.Invoke(fraction);

            foreach (var tickElapsed in countdown.TickElapsedTimeCallbacks.ToArray())
                tickElapsed.Invoke(elapsed);
        }
    }
}
