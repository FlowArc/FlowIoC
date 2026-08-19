using FlowIoC.BaseModule.Controller;
using UnityEngine;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class GroupCommandA : Command
    {
        public override void Execute()
        {
            Retain();
            Debug.Log("[GroupTest] Group A Command executed");
            Release();
        }
    }
}