using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleLogTypePlanTests
    {
        private LogTypeChanges Plan(string[] registered, string[] modules)
        {
            return new ModuleLogTypePlan().Plan(registered, modules);
        }

        [Test]
        public void A_module_with_no_log_type_is_added()
        {
            LogTypeChanges changes = Plan(new string[0], new[] { "CameraModule" });

            CollectionAssert.AreEqual(new[] { "CameraModule" }, changes.ToAdd);
            Assert.IsEmpty(changes.ToRemove);
        }

        [Test]
        public void A_module_that_already_has_a_log_type_is_left_alone()
        {
            LogTypeChanges changes = Plan(new[] { "CameraModule" }, new[] { "CameraModule" });

            Assert.IsEmpty(changes.ToAdd);
            Assert.IsEmpty(changes.ToRemove);
        }

        /// <summary>
        /// ModuleAutoDetector only ever added. A module deleted from the project kept its log
        /// type registered and its constant in the generated FlowLogType.cs indefinitely —
        /// SamplesModule survived its own folder being deleted this way.
        /// </summary>
        [Test]
        public void A_log_type_whose_module_is_gone_is_removed()
        {
            LogTypeChanges changes = Plan(new[] { "SamplesModule" }, new[] { "CameraModule" });

            CollectionAssert.AreEqual(new[] { "CameraModule" }, changes.ToAdd);
            CollectionAssert.AreEqual(new[] { "SamplesModule" }, changes.ToRemove);
        }

        [Test]
        public void The_comparison_ignores_case()
        {
            LogTypeChanges changes = Plan(new[] { "cameramodule" }, new[] { "CameraModule" });

            Assert.IsEmpty(changes.ToAdd);
            Assert.IsEmpty(changes.ToRemove);
        }

        /// <summary>
        /// A scan that found no modules is far more likely to be a failed scan than a project that
        /// genuinely has none, and acting on it would delete every auto-registered channel. The guard
        /// lives here rather than at the call site so it cannot be forgotten — the same reasoning
        /// LogTypeSettingsGuard already applies to the console settings.
        /// </summary>
        [Test]
        public void An_empty_module_list_removes_nothing_because_no_modules_means_a_failed_scan()
        {
            LogTypeChanges changes = Plan(new[] { "CameraModule", "HudModule" }, new string[0]);

            Assert.IsEmpty(changes.ToRemove);
            Assert.IsEmpty(changes.ToAdd);
        }

        [Test]
        public void A_null_input_is_treated_as_empty()
        {
            LogTypeChanges changes = new ModuleLogTypePlan().Plan(null, null);

            Assert.IsEmpty(changes.ToAdd);
            Assert.IsEmpty(changes.ToRemove);
        }
    }
}
