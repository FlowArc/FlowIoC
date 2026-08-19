using FlowIoC.BaseModule.Controller;
using UnityEngine;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class GroupCommandB : Command
    {
        public override void Execute()
        {
            Retain();
            Debug.Log("[GroupTest] Group B Command executed");
            Release();
        }
    }
}