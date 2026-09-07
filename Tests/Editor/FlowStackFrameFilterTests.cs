using FlowIoC.ConsoleModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowStackFrameFilterTests
    {
        private readonly FlowStackFrameFilter _filter = new FlowStackFrameFilter();

        [Test]
        public void A_frame_in_the_framework_is_the_frameworks()
        {
            Assert.IsTrue(_filter.IsFrameworkFrame(
                "FlowIoC.BaseModule.Controller.CommandGroup.CommandGroupResolver:StopCommand (FlowIoC.ICommandBody)"));
        }

        [Test]
        public void A_frame_in_the_console_is_the_frameworks()
        {
            Assert.IsTrue(_filter.IsFrameworkFrame("FlowIoC.ConsoleModule.FlowLogger:LogError (int,string)"));
        }

        [Test]
        public void A_frame_in_a_game_module_is_not()
        {
            Assert.IsFalse(_filter.IsFrameworkFrame(
                "Modules.Player.Controllers.AddCurrencyCommand:Execute () (at Assets/Modules/PlayerModule/A.cs:22)"));
        }

        [Test]
        public void Unitys_own_plumbing_is_skipped_too()
        {
            Assert.IsTrue(_filter.IsFrameworkFrame("UnityEngine.Debug:Log (object)"));
            Assert.IsTrue(_filter.IsFrameworkFrame("System.Reflection.MethodBase:Invoke (object,object[])"));
        }

        /// <summary>
        /// The failure this exists for. A command calls Release without Retain, the resolver
        /// catches it, and double-clicking the error used to open the resolver - the framework
        /// checking a condition, not the code that broke it.
        /// </summary>
        [Test]
        public void The_first_game_frame_is_found_below_the_frameworks_own()
        {
            string[] trace =
            {
                "FlowIoC.ConsoleModule.FlowLogger:LogError (int,string)",
                "FlowIoC.BaseModule.Controller.CommandGroup.CommandGroupResolver:StopCommand (FlowIoC.ICommandBody)",
                "FlowIoC.BaseModule.Controller.CommandBody:Stop ()",
                "Modules.Player.Controllers.AddCurrencyCommand:Execute () (at Assets/Modules/PlayerModule/A.cs:22)",
                "FlowIoC.BaseModule.Controller.CommandGroup.CommandGroupResolver:ExecuteCommandStep (FlowIoC.CommandStepVO,int,object[])"
            };

            Assert.AreEqual(3, _filter.FindFirstGameFrame(trace));
        }

        [Test]
        public void A_trace_that_is_all_framework_reports_no_game_frame()
        {
            string[] trace =
            {
                "FlowIoC.ConsoleModule.FlowLogger:LogError (int,string)",
                "FlowIoC.BaseModule.Root.RootsManager:StartContexts ()"
            };

            Assert.AreEqual(-1, _filter.FindFirstGameFrame(trace));
        }

        [Test]
        public void Blank_lines_are_not_mistaken_for_a_frame()
        {
            string[] trace = {"", "FlowIoC.ConsoleModule.FlowLogger:LogError (int,string)", "", "Modules.A.B:C ()"};

            Assert.AreEqual(3, _filter.FindFirstGameFrame(trace));
        }

        [Test]
        public void No_trace_at_all_reports_no_game_frame()
        {
            Assert.AreEqual(-1, _filter.FindFirstGameFrame(null));
            Assert.AreEqual(-1, _filter.FindFirstGameFrame(new string[0]));
        }
    }
}
