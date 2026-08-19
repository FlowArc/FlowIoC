using FlowIoC.BaseModule.Controller;
using UnityEngine;
using System.Collections;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class FCommand2 : Command
    {
        [Inject] public ICoroutineProvider _coroutineProvider { get; set; }

        public override void Execute()
        {
            Retain();
            Debug.Log("FCommand 2 started");
            _coroutineProvider.StartCoroutine(DelayedComplete());
        }

        private IEnumerator DelayedComplete()
        {
            yield return new WaitForSeconds(2f);
            Debug.Log("FCommand 2 completed");
            Release();
        }
    }
}