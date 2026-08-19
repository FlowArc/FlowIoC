using System.Collections;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using UnityEngine;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class ACommand3 : Command
    {
        [Inject] public ICoroutineProvider _coroutineProvider { get; set; }
        
        public override void Execute()
        {
            Retain();
            Debug.Log("ACommand3 executed");
            _coroutineProvider.StartCoroutine(DelayedComplete());
        }

        private IEnumerator DelayedComplete()
        {
            yield return new WaitForSeconds(3f);
            Debug.Log("ACommand3 completed");
            Release();
        }
    }
}