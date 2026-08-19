using FlowIoC.BaseModule.Controller;
using UnityEngine;
using System.Collections;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class DCommand12 : Command
    {
        [Inject] public ICoroutineProvider _coroutineProvider { get; set; }

        public override void Execute()
        {
            Retain();
            Debug.Log("DCommand 12 started");
            _coroutineProvider.StartCoroutine(DelayedComplete());
        }

        private IEnumerator DelayedComplete()
        {
            yield return new WaitForSeconds(12f);
            Debug.Log("DCommand 12 completed");
            Release();
        }
    }
}