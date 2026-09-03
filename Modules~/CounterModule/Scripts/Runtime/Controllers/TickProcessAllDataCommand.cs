using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using Modules.CounterModule.Data.ValueObjects;

namespace Modules.CounterModule.Controllers
{
    /// <summary>
    /// One second of work for every counter: report the new time, or finish the ones that have
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

            // A callback is free to start or stop a counter, which would change the map while
            // it was being walked. Copying the ids first means the walk is over before any
            // callback runs.
            lock (_counterModel.LockObject)
            {
                ids = new List<string>(_counterModel.DataMap.Keys);
            }

            foreach (string id in ids)
            {
                CounterVO counter;

                lock (_counterModel.LockObject)
                {
                    if (!_counterModel.DataMap.TryGetValue(id, out counter))
                        continue;
                }

                Process(id, counter, completedIds);
            }

            lock (_counterModel.LockObject)
            {
                foreach (string completedId in completedIds)
                    _counterModel.DataMap.Remove(completedId);
            }

            Release();
        }

        private void Process(string id, CounterVO counter, List<string> completedIds)
        {
            int elapsed = ElapsedSeconds(counter);
            int remaining = RemainingSeconds(counter);

            // An entry nobody asked a remaining time of is measuring forward, and finishes on
            // its duration only if it was given one. Everything else finishes at zero.
            bool isCompleted = counter.IsElapsedOnly
                ? counter.Duration.TotalSeconds > 0 && elapsed >= counter.Duration.TotalSeconds
                : remaining <= 0;

            if (isCompleted)
            {
                foreach (var complete in counter.CompleteCallbacks.ToArray())
                    complete.Invoke();

                counter.Clear();
                completedIds.Add(id);
                return;
            }

            foreach (var tick in counter.TickCallbacks.ToArray())
                tick.Invoke(remaining);

            float fraction = RemainingFraction(counter, remaining);

            foreach (var tickPercentage in counter.TickPercentageCallbacks.ToArray())
                tickPercentage.Invoke(fraction);

            foreach (var tickElapsed in counter.TickElapsedTimeCallbacks.ToArray())
                tickElapsed.Invoke(elapsed);
        }
    }
}
