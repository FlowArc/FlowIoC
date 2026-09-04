using FlowIoC.Editor.CodeGenerator.Menus.Module;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What Create Module calls the Root and the Context it writes. The module name itself is
    /// never touched - a Service module is named for what it does - so these two names are the
    /// only place the role is spelled out, and the Root inspector reads its colour off the Root's
    /// name alone.
    /// </summary>
    public class ModuleRoleNamingTests
    {
        private ModuleRoleNaming _naming;

        [SetUp]
        public void SetUp() => _naming = new ModuleRoleNaming();

        [Test]
        public void A_system_module_names_its_root_and_context_for_the_role()
        {
            Assert.AreEqual("PlayerSystemRoot", _naming.RootName("Player", ModuleRole.System));
            Assert.AreEqual("PlayerSystemContext", _naming.ContextName("Player", ModuleRole.System));
        }

        [Test]
        public void A_service_module_names_its_root_and_context_for_the_role()
        {
            Assert.AreEqual("CounterServiceRoot", _naming.RootName("Counter", ModuleRole.Service));
            Assert.AreEqual("CounterServiceContext", _naming.ContextName("Counter", ModuleRole.Service));
        }

        /// <summary>
        /// Core is the plain Root the generator has always written, so it adds nothing at all.
        /// </summary>
        [Test]
        public void A_core_module_keeps_the_names_it_had_before_the_role_existed()
        {
            Assert.AreEqual("PlayerRoot", _naming.RootName("Player", ModuleRole.Core));
            Assert.AreEqual("PlayerContext", _naming.ContextName("Player", ModuleRole.Core));
            Assert.AreEqual(string.Empty, _naming.Suffix(ModuleRole.Core));
        }

        /// <summary>
        /// A module named for the role already - the shape the rules discourage, but nothing stops
        /// someone typing it - is left as it is rather than gaining the word twice.
        /// </summary>
        [Test]
        public void A_name_that_already_ends_in_the_role_does_not_gain_it_twice()
        {
            Assert.AreEqual("CounterServiceRoot", _naming.RootName("CounterService", ModuleRole.Service));
            Assert.AreEqual("MapSystemContext", _naming.ContextName("MapSystem", ModuleRole.System));
        }

        /// <summary>
        /// The ending is read as the word it is, so a name that merely looks similar in lowercase
        /// is a different name and still gains the role.
        /// </summary>
        [Test]
        public void A_name_ending_in_the_role_in_another_casing_still_gains_it()
        {
            Assert.AreEqual("CounterserviceService", _naming.Apply("Counterservice", ModuleRole.Service));
        }

        [Test]
        public void An_empty_module_name_is_handed_back_untouched()
        {
            Assert.AreEqual(string.Empty, _naming.Apply(string.Empty, ModuleRole.System));
        }
    }
}
