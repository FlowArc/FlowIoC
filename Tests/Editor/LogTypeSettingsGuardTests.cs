using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class LogTypeSettingsGuardTests
    {
        [Test]
        public void An_empty_list_is_not_trustworthy()
        {
            Assert.IsFalse(new LogTypeSettingsGuard()
                .IsTrustworthy(new List<FlowConsoleSettings.FlowConsoleLogType>()));
        }

        [Test]
        public void A_null_list_is_not_trustworthy()
        {
            Assert.IsFalse(new LogTypeSettingsGuard().IsTrustworthy(null));
        }

        /// <summary>
        /// A real settings asset always carries its mandatory channels. A settings object with
        /// none of them did not come from disk, and deleting generated files on its word is how
        /// a transient import failure turns into lost source.
        /// </summary>
        [Test]
        public void A_list_without_any_mandatory_type_is_not_trustworthy()
        {
            var types = new List<FlowConsoleSettings.FlowConsoleLogType>
            {
                new FlowConsoleSettings.FlowConsoleLogType { Name = "Default", IsMandatory = false },
                new FlowConsoleSettings.FlowConsoleLogType { Name = "PlayerModule", IsMandatory = false },
            };

            Assert.IsFalse(new LogTypeSettingsGuard().IsTrustworthy(types));
        }

        [Test]
        public void A_list_carrying_a_mandatory_type_is_trustworthy()
        {
            var types = new List<FlowConsoleSettings.FlowConsoleLogType>
            {
                new FlowConsoleSettings.FlowConsoleLogType { Name = "All", IsMandatory = true },
                new FlowConsoleSettings.FlowConsoleLogType { Name = "Default", IsMandatory = false },
            };

            Assert.IsTrue(new LogTypeSettingsGuard().IsTrustworthy(types));
        }
    }
}
