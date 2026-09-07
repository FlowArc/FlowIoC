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

        /// <summary>
        /// The crash this exists for. A path read out of a stack trace is whatever text was
        /// there - a generated frame carries angle brackets, which System.IO.Path refuses with an
        /// ArgumentException. Thrown inside OnGUI it unbalanced GUILayout and took the whole
        /// console window's drawing down, once per repaint.
        /// </summary>
        [Test]
        public void A_path_System_IO_Path_would_refuse_is_still_taken_apart()
        {
            Assert.AreEqual("Thing<T>.cs", _filter.FileNameOf("<generated>/Thing<T>.cs"));
            Assert.AreEqual("Thing<T>", _filter.FileNameWithoutExtensionOf("<generated>/Thing<T>.cs"));
        }

        [Test]
        public void Both_separators_are_understood()
        {
            Assert.AreEqual("A.cs", _filter.FileNameOf("Assets/Modules/A.cs"));
            Assert.AreEqual("A.cs", _filter.FileNameOf(@"Assets\Modules\A.cs"));
        }

        [Test]
        public void A_bare_name_is_its_own_file_name()
        {
            Assert.AreEqual("A.cs", _filter.FileNameOf("A.cs"));
            Assert.AreEqual("A", _filter.FileNameWithoutExtensionOf("A.cs"));
        }

        /// <summary>A leading dot is the whole name, not an empty name with an extension.</summary>
        [Test]
        public void A_dotfile_keeps_its_name()
        {
            Assert.AreEqual(".hidden", _filter.FileNameWithoutExtensionOf(".hidden"));
        }

        [Test]
        public void No_path_at_all_is_an_empty_name_rather_than_a_throw()
        {
            Assert.AreEqual(string.Empty, _filter.FileNameOf(null));
            Assert.AreEqual(string.Empty, _filter.FileNameOf(""));
            Assert.AreEqual(string.Empty, _filter.FileNameWithoutExtensionOf(null));
        }

        [Test]
        public void A_frame_gives_up_its_file_and_line()
        {
            Assert.IsTrue(_filter.TryParseFrame(
                "Modules.A.B:C () (at Assets/Modules/A.cs:22)", out string path, out int line));

            Assert.AreEqual("Assets/Modules/A.cs", path);
            Assert.AreEqual(22, line);
        }

        [Test]
        public void A_frame_with_no_location_gives_up_nothing()
        {
            Assert.IsFalse(_filter.TryParseFrame("UnityEngine.Debug:Log (object)", out _, out _));
            Assert.IsFalse(_filter.TryParseFrame(null, out _, out _));
        }

        [Test]
        public void A_frames_class_name_is_read_off_its_front()
        {
            Assert.AreEqual("Modules.A.B", _filter.ParseClassName("Modules.A.B:C () (at A.cs:1)"));
            Assert.AreEqual("Modules.A.B", _filter.ParseClassName("Modules.A.B/Nested:C ()"));
            Assert.IsNull(_filter.ParseClassName("no colon here"));
        }

        [Test]
        public void No_trace_at_all_reports_no_game_frame()
        {
            Assert.AreEqual(-1, _filter.FindFirstGameFrame(null));
            Assert.AreEqual(-1, _filter.FindFirstGameFrame(new string[0]));
        }
    }
}