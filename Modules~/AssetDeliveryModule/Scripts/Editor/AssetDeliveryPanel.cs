#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModulePanels;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Android;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets.Android;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>
    /// The build settings a store delivery depends on, read and shown, because each one fails
    /// quietly when it is off: an APK carries no packs, an unsplit binary makes none, a group
    /// without the schema ships inside the app. One button, and it is Unity's own work.
    /// </summary>
    internal class AssetDeliveryPanel : ModulePanel
    {
        private const string MENU_PATH = "Tools/FlowIoC-Modules/Asset Delivery/Panel";
        private const string INIT_PAD_MENU = "Window/Asset Management/Addressables/Init Play Asset Delivery";

        /// <summary>The same priority every module panel uses: it is what seats FlowIoC-Modules second under Tools.</summary>
        private const int MENU_PRIORITY = -1080;

        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        private static void Open() => ModulePanelWindow.Open<AssetDeliveryPanel>();

        public override string Title => "Asset Delivery";

        public override string Module => "AssetDeliveryModule";

        public override string Subtitle => "What the next build sends to the stores";

        public override FlowRole Role => FlowRole.Service;

        public override string HelpPage => "Asset Delivery Module";

        public override void Draw(ModulePanelPainter painter)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            DrawAndroid(painter, settings);
            painter.Space();
            DrawIos(painter, settings);
            painter.Space();
            DrawGroups(painter, settings);
        }

        private void DrawAndroid(ModulePanelPainter painter, AddressableAssetSettings settings)
        {
            painter.Heading("Android");

            bool padReady = new PlayAssetDeliveryState().IsInitialized(settings);
            painter.Field("Play Asset Delivery", padReady ? "initialised" : "not initialised - no group reaches Play");
            painter.Field("Build App Bundle", EditorUserBuildSettings.buildAppBundle ? "on" : "off - an APK carries no asset packs");
            painter.Field("Split Application Binary", PlayerSettings.Android.splitApplicationBinary ? "on" : "off - no asset packs are generated");

            if (!padReady)
                painter.Actions(new ModulePanelAction("Init Play Asset Delivery", () => EditorApplication.ExecuteMenuItem(INIT_PAD_MENU)));
        }

        private void DrawIos(ModulePanelPainter painter, AddressableAssetSettings settings)
        {
            painter.Heading("iOS");
            painter.Field("Deployment target", PlayerSettings.iOS.targetOSVersionString + " (managed asset packs need 26.0 on the device)");
            painter.Field("App Group", "group." + PlayerSettings.applicationIdentifier + ".assets");
            painter.Field("Initialization object",
                settings != null && new AssetDeliverySetup().IsRegistered(settings) ? "listed" : "listed at the next iOS Addressables build");
        }

        private void DrawGroups(ModulePanelPainter painter, AddressableAssetSettings settings)
        {
            painter.Heading("Delivery groups");

            if (settings == null)
            {
                painter.Note("No Addressables settings yet.");
                return;
            }

            var names = new AssetPackNames();
            int shown = 0;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null || !group.HasSchema<PlayAssetDeliverySchema>()) continue;

                DeliveryType type = group.GetSchema<PlayAssetDeliverySchema>().AssetPackDeliveryType;
                if (type == DeliveryType.None) continue;

                string android = names.IsGoogleCompliant(group.Name) ? group.Name : "renamed by Unity at build";
                painter.Field(group.Name, $"{type} - Android: {android}, iOS: {names.Sanitize(group.Name)}");
                shown++;
            }

            if (shown == 0)
                painter.Note("No group carries the Play Asset Delivery schema with a delivery type. Everything ships inside the app.");
        }
    }
}
#endif
