using FlowIoC.BaseModule.Controller;
using UnityEngine;

namespace FlowIoC.Samples.CommandExecutionTestModule.Controllers
{
    public class CCommand0 : Command
    {
        public override void Execute()
        {
            Debug.Log("CCommand0 executed");
        }
    }
}