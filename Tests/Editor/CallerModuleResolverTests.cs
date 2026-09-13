using FlowIoC.ConsoleModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class CallerModuleResolverTests
    {
        private readonly CallerModuleResolver _resolver = new CallerModuleResolver();

        [Test]
        public void A_file_in_a_module_logs_on_that_module()
        {
            Assert.AreEqual("PlayerModule",
                _resolver.ChannelOf(@"D:\Game\Assets\Modules\PlayerModule\Scripts\Runtime\Controllers\AddCurrencyCommand.cs"));
        }

        /// <summary>
        /// A screen or sub module is the innermost module the file sits in, not the one that holds
        /// it: its rows are its own, on its own channel.
        /// </summary>
        [Test]
        public void A_nested_module_wins_over_the_one_it_lives_in()
        {
            Assert.AreEqual("MainScreenModule",
                _resolver.ChannelOf("/Game/Assets/Modules/MainModule/zScreenModules/MainScreenModule/Scripts/Runtime/Controllers/OpenMainScreenCommand.cs"));
        }

        /// <summary>
        /// A test module has no channel of its own, so its files log on the module they test -
        /// the next module folder out.
        /// </summary>
        [Test]
        public void A_test_module_logs_on_the_module_it_tests()
        {
            Assert.AreEqual("CounterModule",
                _resolver.ChannelOf("Assets/Modules/CounterModule/zTestModules/CounterTestModule/Scripts/Runtime/CounterTestContext.cs"));
        }

        [Test]
        public void A_file_outside_any_module_logs_on_Default()
        {
            Assert.AreEqual(FlowModule.Default, _resolver.ChannelOf(@"D:\Game\Assets\DeviceTests\ConsoleProbe\EmitProbeRowsCommand.cs"));
            Assert.AreEqual(FlowModule.Default, _resolver.ChannelOf(""));
            Assert.AreEqual(FlowModule.Default, _resolver.ChannelOf(null));
        }

        /// <summary>
        /// Only a folder names the module. A file called PlayerModule.cs in a folder that is no
        /// module says nothing, and a folder named exactly Module is not one either.
        /// </summary>
        [Test]
        public void The_file_name_and_a_bare_Module_folder_do_not_count()
        {
            Assert.AreEqual(FlowModule.Default, _resolver.ChannelOf("Assets/Scripts/PlayerModule.cs"));
            Assert.AreEqual(FlowModule.Default, _resolver.ChannelOf("Assets/Module/Thing.cs"));
        }

        /// <summary>
        /// The path is a compile-time constant, so a call site asks with the same string every
        /// time and the answer is kept rather than walked again.
        /// </summary>
        [Test]
        public void The_same_path_is_answered_from_memory()
        {
            const string path = "Assets/Modules/HapticModule/Scripts/Runtime/Services/SilentHapticPlayer.cs";

            Assert.AreSame(_resolver.ChannelOf(path), _resolver.ChannelOf(path));
        }
    }
}
