using System;
using System.IO;
using FlowIoC.Editor.CodeGenerator;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The generator binds the two holders differently: the public one across contexts, because
    /// a Connector and a test module reach it from outside, and the internal one with the plain
    /// binder, because an internal signal never crosses a boundary.
    /// </summary>
    public class BindSignalsInContextTests
    {
        private string _folder;
        private string _contextPath;

        private const string CONTEXT =
            "using FlowIoC.BaseModule.Contexts;\n"
            + "\n"
            + "namespace Modules.PlayerModule.RootsContexts\n"
            + "{\n"
            + "    public class PlayerContext : Context\n"
            + "    {\n"
            + "        public override void SignalBindings()\n"
            + "        {\n"
            + "            base.SignalBindings();\n"
            + "        }\n"
            + "    }\n"
            + "}\n";

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "FlowIoC-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);
            _contextPath = Path.Combine(_folder, "PlayerContext.cs");
            File.WriteAllText(_contextPath, CONTEXT);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        [Test]
        public void The_public_holder_is_bound_across_contexts()
        {
            CodeGeneratorUtils.BindSignalsInContext(_contextPath, "PlayerSignals", "Modules.PlayerModule.Signals");

            string written = File.ReadAllText(_contextPath);

            StringAssert.Contains("_signals = InjectionBinderCrossContext.Bind<PlayerSignals>();", written);
            StringAssert.Contains("private PlayerSignals _signals;", written);
        }

        [Test]
        public void The_internal_holder_is_bound_with_the_plain_binder()
        {
            CodeGeneratorUtils.BindSignalsInContext(_contextPath, "PlayerInternalSignals", "Modules.PlayerModule.Signals",
                "_internalSignals", crossContext: false);

            string written = File.ReadAllText(_contextPath);

            StringAssert.Contains("_internalSignals = InjectionBinder.Bind<PlayerInternalSignals>();", written);
            StringAssert.DoesNotContain("InjectionBinderCrossContext.Bind<PlayerInternalSignals>", written);
        }

        [Test]
        public void A_holder_already_bound_is_not_bound_twice()
        {
            CodeGeneratorUtils.BindSignalsInContext(_contextPath, "PlayerInternalSignals", "Modules.PlayerModule.Signals",
                "_internalSignals", crossContext: false);
            CodeGeneratorUtils.BindSignalsInContext(_contextPath, "PlayerInternalSignals", "Modules.PlayerModule.Signals",
                "_internalSignals", crossContext: false);

            string written = File.ReadAllText(_contextPath);
            int first = written.IndexOf("InjectionBinder.Bind<PlayerInternalSignals>", StringComparison.Ordinal);
            int second = written.IndexOf("InjectionBinder.Bind<PlayerInternalSignals>", first + 1, StringComparison.Ordinal);

            Assert.GreaterOrEqual(first, 0);
            Assert.AreEqual(-1, second);
        }
    }
}