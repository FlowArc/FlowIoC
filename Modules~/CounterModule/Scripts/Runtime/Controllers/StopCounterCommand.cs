using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CounterModule.Data.ValueObjects;
using Modules.CounterModule.Models;

namespace Modules.CounterModule.Controllers
{
    /// <summary>
    /// Ends a counter before it runs out. Its stop callbacks are told; its complete callbacks
    /// are not, because it never completed.
    /// </summary>
    internal class StopCounterCommand : Command
    {
        [SignalParam] private string _id { get; set; }
        [Inject] private ICounterModel _counterModel { get; set; }

        public override void Execute()
        {
            Retain();

            lock (_counterModel.LockObject)
            {
                if (_counterModel.DataMap.TryGetValue(_id, out CounterVO counter))
                {
                    _counterModel.DataMap.Remove(_id);

                    foreach (var stop in counter.StopCallbacks.ToArray())
                        stop.Invoke();

                    counter.Clear();
                }
            }

            Release();
        }
    }
}
