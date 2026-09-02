using FlowIoC.Editor.Migration;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What to do with one leftover CD_Screen asset. Generating a file into a project is the
    /// migrator's most consequential act, so when it does and does not happen is pinned down here.
    /// </summary>
    public class ScreenConfigMigrationPlanTests
    {
        private const string ViewScript =
            "Assets/Modules/MainModule/zScreenModules/MainScreenModule/Scripts/Runtime/ViewsMediators/MainScreenView.cs";

        private ScreenConfigMigrationPlan _plan;
        private LegacyScreenConfig _config;

        [SetUp]
        public void SetUp()
        {
            _plan = new ScreenConfigMigrationPlan();
            _config = new LegacyScreenConfig
            {
                AssetPath = "Assets/Modules/MainModule/zScreenModules/MainScreenModule/Scriptables/ScreenConfigs/CD_MainScreen.asset",
                ViewTypeName = "MainScreenView",
                MediatorTypeName = "MainScreenMediator",
                Layer = 1,
                Tag = ScreenTag.GroupB,
                LoadType = ScreenLoadType.Addressable,
                AddressableKey = "MainScreen",
                HasShowAnimation = true
            };
        }

        [Test]
        public void A_screen_without_a_context_gets_one_generated_beside_its_view()
        {
            ScreenConfigMigrationStep step = _plan.For(_config, ViewScript, contextExists: false);

            Assert.AreEqual(ScreenConfigMigrationAction.GenerateContext, step.Action);
            Assert.AreEqual(
                "Assets/Modules/MainModule/zScreenModules/MainScreenModule/Scripts/Runtime/RootsContexts/MainScreenContext.cs",
                step.ContextPath);
            Assert.AreEqual("MainScreenContext", step.ContextName);
            Assert.AreEqual("MainScreenView", step.ViewName);
            Assert.AreEqual("MainScreenMediator", step.MediatorName);
        }

        [Test]
        public void The_asset_values_become_the_context_settings()
        {
            ScreenConfigMigrationStep step = _plan.For(_config, ViewScript, contextExists: false);

            Assert.AreEqual(1, step.Settings.Layer);
            Assert.AreEqual(ScreenTag.GroupB, step.Settings.Tag);
            Assert.AreEqual(ScreenLoadType.Addressable, step.Settings.LoadType);
            Assert.AreEqual("MainScreen", step.Settings.AddressableKey);
            Assert.IsTrue(step.Settings.HasShowAnimation);
            Assert.IsFalse(step.Settings.HasHideAnimation);
        }

        [Test]
        public void A_resource_screen_keeps_its_path()
        {
            _config.LoadType = ScreenLoadType.Resource;
            _config.ResourcePath = "Screens/Main";

            ScreenConfigMigrationStep step = _plan.For(_config, ViewScript, contextExists: false);

            Assert.AreEqual(ScreenLoadType.Resource, step.Settings.LoadType);
            Assert.AreEqual("Screens/Main", step.Settings.ResourcePath);
        }

        [Test]
        public void A_screen_that_already_has_a_context_is_reported_not_overwritten()
        {
            ScreenConfigMigrationStep step = _plan.For(_config, ViewScript, contextExists: true);

            Assert.AreEqual(ScreenConfigMigrationAction.ReportBlock, step.Action);
            StringAssert.Contains("ScreenSubContext<MainScreenView, MainScreenMediator>", step.Reason);
        }

        [Test]
        public void A_direct_prefab_screen_is_reported_because_code_cannot_hold_the_prefab()
        {
            _config.WasDirectPrefab = true;

            ScreenConfigMigrationStep step = _plan.For(_config, ViewScript, contextExists: false);

            Assert.AreEqual(ScreenConfigMigrationAction.ReportBlock, step.Action);
            StringAssert.Contains("DirectPrefab", step.Reason);
        }

        [Test]
        public void A_screen_whose_view_script_cannot_be_found_is_skipped()
        {
            ScreenConfigMigrationStep step = _plan.For(_config, viewScriptPath: null, contextExists: false);

            Assert.AreEqual(ScreenConfigMigrationAction.Skip, step.Action);
            StringAssert.Contains("MainScreenView", step.Reason);
        }

        [Test]
        public void A_missing_mediator_name_is_derived_from_the_view_name()
        {
            _config.MediatorTypeName = null;

            Assert.AreEqual("MainScreenMediator", _plan.For(_config, ViewScript, false).MediatorName);
        }

        [Test]
        public void A_view_outside_a_ViewsMediators_folder_gets_its_context_beside_it()
        {
            _config.ViewTypeName = "HudView";
            _config.MediatorTypeName = "HudMediator";

            ScreenConfigMigrationStep step = _plan.For(_config, "Assets/Game/HudView.cs", contextExists: false);

            Assert.AreEqual("Assets/Game/HudContext.cs", step.ContextPath);
            Assert.AreEqual("HudContext", step.ContextName);
        }
    }
}
