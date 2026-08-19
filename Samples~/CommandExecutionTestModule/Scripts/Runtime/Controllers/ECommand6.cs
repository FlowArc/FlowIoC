using FlowIoC.BaseModule.Controller;
using UnityEngine;
using System.Collections;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class ECommand6 : Command
    {
        [Inject] public ICoroutineProvider _coroutineProvider { get; set; }

        public override void Execute()
        {
            Retain();
            Debug.Log("ECommand 6 started");
            _coroutineProvider.StartCoroutine(DelayedComplete());
        }

        private IEnumerator DelayedComplete()
        {
            yield return new WaitForSeconds(6f);
            Debug.Log("ECommand 6 completed");
            // Release();
            Stop();
        }
    }
}