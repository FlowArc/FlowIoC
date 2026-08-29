using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CountdownServiceModule.Data.ValueObjects;
using Modules.CountdownServiceModule.Models;

namespace Modules.CountdownServiceModule.Controllers
{
    /// <summary>
    /// Ends a countdown before it runs out. Its stop callbacks are told; its complete callbacks
    /// are not, because it never completed.
    /// </summary>
    internal class StopCountdownCommand : Command
    {
        [SignalParam] private string _id { get; set; }
        [Inject] private ICountdownModel _countdownModel { get; set; }

        public override void Execute()
        {
            Retain();

            lock (_countdownModel.LockObject)
            {
                if (_countdownModel.DataMap.TryGetValue(_id, out CountdownVO countdown))
                {
                    _countdownModel.DataMap.Remove(_id);

                    foreach (var stop in countdown.StopCallbacks.ToArray())
                        stop.Invoke();

                    countdown.Clear();
                }
            }

            Release();
        }
    }
}
