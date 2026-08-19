using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.Samples.CommandExecutionTestModule.Signals;
using UnityEngine;
using System.Collections;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class InitializeTestCommand : Command
    {
        [Inject] public ICoroutineProvider _coroutineProvider { get; set; }
        [InjectSignal] public CommandTestSignals _testSignals { get; set; }

        public override void Execute()
        {
            Retain();
            Debug.Log("=== Command Execution Tests Initialized ===");
            _coroutineProvider.StartCoroutine(RunAllTests());
        }

        private IEnumerator RunAllTests()
        {
             // yield return new WaitForSeconds(1f);
             //
             // Debug.Log("\n=== Running ToSequence Test ===");
             // _testSignals.InComing.StartSequenceTest.Dispatch();
             //
             // yield return new WaitForSeconds(2f);
             //
             // Debug.Log("\n=== Running ToParallel Test ===");
             // _testSignals.InComing.StartParallelTest.Dispatch();
             //
             // yield return new WaitForSeconds(3f);
             //
             // Debug.Log("\n=== Running ToGroup Test ===");
             // _testSignals.InComing.StartGroupTest.Dispatch();
            
             yield return new WaitForSeconds(1f);
            
             Debug.Log("\n=== Running Complex Group Test ===");
             _testSignals.InComing.StartComplexGroupTest.Dispatch();

            // yield return new WaitForSeconds(15f);
            //
            // Debug.Log("\n=== Running StartJumpSignalTest ===");
            // _testSignals.InComing.StartJumpSignalTest.Dispatch();
            //
            //  yield return new WaitForSeconds(2f);
            //
            // Debug.Log("\n=== All Tests Completed ===");
            // _testSignals.OutGoing.TestCompleted.Dispatch("All tests completed successfully!");

            Release();
        }
    }
}