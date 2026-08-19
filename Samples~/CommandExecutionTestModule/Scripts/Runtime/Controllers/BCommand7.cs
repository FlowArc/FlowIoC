using System.Collections;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using UnityEngine;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class BCommand7 : Command
    {
        [Inject] public ICoroutineProvider _coroutineProvider { get; set; }
        
        public override void Execute()
        {
            Retain();
            Debug.Log("BCommand 7 started");
            _coroutineProvider.StartCoroutine(DelayedComplete());
        }

        private IEnumerator DelayedComplete()
        {
            yield return new WaitForSeconds(7f);
            Debug.Log("BCommand 7 completed");
            Release();
        }
    }
}