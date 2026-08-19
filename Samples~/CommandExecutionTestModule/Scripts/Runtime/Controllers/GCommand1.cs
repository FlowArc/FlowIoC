using FlowIoC.BaseModule.Controller;
using UnityEngine;
using System.Collections;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.Signals;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class GCommand1 : Command<bool,Signal,Signal>
    {
        [Inject] public ICoroutineProvider _coroutineProvider { get; set; }

        public override void Execute(bool condition, Signal signalA, Signal signalB)
        {
            Retain();
            Debug.Log("GCommand 1 started");
            _coroutineProvider.StartCoroutine(DelayedComplete(condition,signalA, signalB));
        }
        private IEnumerator DelayedComplete(bool condition,Signal signalA, Signal signalB)
        {
            yield return new WaitForSeconds(1f);
            
            if (condition)
                signalA.Dispatch();
            else
                signalB.Dispatch();
            
            Debug.Log("GCommand 1 completed");
            Release();
        }

    }
}