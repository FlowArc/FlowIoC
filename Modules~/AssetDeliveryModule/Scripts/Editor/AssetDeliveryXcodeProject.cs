#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.iOS.Xcode;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>
    /// What Apple's "Background Download" template adds to an Xcode project, done to Unity's
    /// generated one: an ExtensionKit downloader extension with one Swift file that takes the
    /// system's managed implementation, the App Group both targets share, and the three
    /// Background Assets keys on the app. Run twice it changes nothing the second time.
    /// Needs iOS Build Support in the Editor, which is where UnityEditor.iOS.Xcode comes from.
    /// </summary>
    internal class AssetDeliveryXcodeProject
    {
        public const string EXTENSION_NAME = "AssetDeliveryExtension";

        private const string SWIFT_FILE = "AssetDeliveryDownloader.swift";
        private const string EMBED_PHASE = "Embed ExtensionKit Extensions";
        private const string FRAMEWORK = "BackgroundAssets.framework";
        private const string APP_ENTITLEMENTS = "Unity-iPhone.entitlements";

        private const string SWIFT =
            "import BackgroundAssets\n\n"
            + "// The system's managed downloader: the policies come from each pack's Manifest.json, so there is nothing to decide here.\n"
            + "@main\n"
            + "struct AssetDeliveryDownloader: ManagedDownloaderExtension {\n"
            + "}\n";

        public void Apply(string projectRoot, string appBundleId)
        {
            string group = "group." + appBundleId + ".assets";
            string extensionDirectory = Path.Combine(projectRoot, EXTENSION_NAME);
            Directory.CreateDirectory(extensionDirectory);
            File.WriteAllText(Path.Combine(extensionDirectory, SWIFT_FILE), SWIFT);
            File.WriteAllText(Path.Combine(extensionDirectory, "Info.plist"), ExtensionPlist());
            File.WriteAllText(Path.Combine(extensionDirectory, EXTENSION_NAME + ".entitlements"), Entitlements(group));

            string pbxPath = PBXProject.GetPBXProjectPath(projectRoot);
            var project = new PBXProject();
            project.ReadFromFile(pbxPath);

            string mainGuid = project.GetUnityMainTargetGuid();
            string frameworkGuid = project.GetUnityFrameworkTargetGuid();
            string extensionGuid = project.TargetGuidByName(EXTENSION_NAME);

            if (extensionGuid == null)
            {
                extensionGuid = project.AddTarget(EXTENSION_NAME, "appex", "com.apple.product-type.extensionkit-extension");
                string sources = project.AddSourcesBuildPhase(extensionGuid);
                project.AddFrameworksBuildPhase(extensionGuid);
                project.AddResourcesBuildPhase(extensionGuid);

                string swiftGuid = project.AddFile(EXTENSION_NAME + "/" + SWIFT_FILE, EXTENSION_NAME + "/" + SWIFT_FILE, PBXSourceTree.Source);
                project.AddFileToBuildSection(extensionGuid, sources, swiftGuid);
                project.AddFrameworkToProject(extensionGuid, FRAMEWORK, false);

                string embed = project.AddCopyFilesBuildPhase(mainGuid, EMBED_PHASE, "$(EXTENSIONS_FOLDER_PATH)", "16");
                project.AddFileToBuildSection(mainGuid, embed, project.GetTargetProductFileRef(extensionGuid));
                project.AddTargetDependency(mainGuid, extensionGuid);
            }

            project.SetBuildProperty(extensionGuid, "PRODUCT_BUNDLE_IDENTIFIER", appBundleId + ".asset-delivery");
            project.SetBuildProperty(extensionGuid, "INFOPLIST_FILE", EXTENSION_NAME + "/Info.plist");
            project.SetBuildProperty(extensionGuid, "GENERATE_INFOPLIST_FILE", "NO");
            project.SetBuildProperty(extensionGuid, "IPHONEOS_DEPLOYMENT_TARGET", "26.0");
            project.SetBuildProperty(extensionGuid, "SWIFT_VERSION", "5.0");
            project.SetBuildProperty(extensionGuid, "TARGETED_DEVICE_FAMILY", "1,2");
            project.SetBuildProperty(extensionGuid, "SKIP_INSTALL", "YES");
            project.SetBuildProperty(extensionGuid, "CODE_SIGN_ENTITLEMENTS", EXTENSION_NAME + "/" + EXTENSION_NAME + ".entitlements");
            project.SetBuildProperty(extensionGuid, "CURRENT_PROJECT_VERSION", "1");
            project.SetBuildProperty(extensionGuid, "MARKETING_VERSION", "1.0");

            // The plugin lives in UnityFramework; the app may run below iOS 26, so the link is weak.
            if (!project.ContainsFramework(frameworkGuid, FRAMEWORK))
                project.AddFrameworkToProject(frameworkGuid, FRAMEWORK, true);

            project.WriteToFile(pbxPath);
            FixProductType(pbxPath);

            string entitlements = project.GetEntitlementFilePathForTarget(mainGuid) ?? APP_ENTITLEMENTS;
            var capabilities = new ProjectCapabilityManager(pbxPath, entitlements, "Unity-iPhone", mainGuid);
            capabilities.AddAppGroups(new[] {group});
            capabilities.WriteToFile();

            string plistPath = Path.Combine(projectRoot, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("BAAppGroupID", group);
            plist.root.SetBoolean("BAHasManagedAssetPacks", true);
            plist.root.SetBoolean("BAUsesAppleHosting", true);
            plist.WriteToFile(plistPath);
        }

        /// <summary>
        /// Unity types the product by its extension - wrapper.app-extension, a last-known type -
        /// and ExtensionKit wants its own, declared explicitly the way Xcode writes it.
        /// </summary>
        private void FixProductType(string pbxPath)
        {
            string text = File.ReadAllText(pbxPath);
            string fixedText = Regex.Replace(text,
                "(?:lastKnownFileType|explicitFileType) = \"?wrapper[.]app-extension\"?;( ?(?:includeInIndex = 0; )?path = \"?" + EXTENSION_NAME +
                "[.]appex\"?;)",
                "explicitFileType = \"wrapper.extensionkit-extension\"; includeInIndex = 0;$1");

            if (fixedText != text) File.WriteAllText(pbxPath, fixedText);
        }

        private string ExtensionPlist() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
            + "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n"
            + "<plist version=\"1.0\">\n<dict>\n"
            + "\t<key>CFBundleDevelopmentRegion</key>\n\t<string>$(DEVELOPMENT_LANGUAGE)</string>\n"
            + "\t<key>CFBundleDisplayName</key>\n\t<string>" + EXTENSION_NAME + "</string>\n"
            + "\t<key>CFBundleExecutable</key>\n\t<string>$(EXECUTABLE_NAME)</string>\n"
            + "\t<key>CFBundleIdentifier</key>\n\t<string>$(PRODUCT_BUNDLE_IDENTIFIER)</string>\n"
            + "\t<key>CFBundleInfoDictionaryVersion</key>\n\t<string>6.0</string>\n"
            + "\t<key>CFBundleName</key>\n\t<string>$(PRODUCT_NAME)</string>\n"
            + "\t<key>CFBundlePackageType</key>\n\t<string>$(PRODUCT_BUNDLE_PACKAGE_TYPE)</string>\n"
            + "\t<key>CFBundleShortVersionString</key>\n\t<string>$(MARKETING_VERSION)</string>\n"
            + "\t<key>CFBundleVersion</key>\n\t<string>$(CURRENT_PROJECT_VERSION)</string>\n"
            + "\t<key>EXAppExtensionAttributes</key>\n\t<dict>\n"
            + "\t\t<key>EXExtensionPointIdentifier</key>\n\t\t<string>com.apple.background-asset-downloader-extension</string>\n"
            + "\t</dict>\n"
            + "</dict>\n</plist>\n";

        private string Entitlements(string group) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
            + "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n"
            + "<plist version=\"1.0\">\n<dict>\n"
            + "\t<key>com.apple.security.application-groups</key>\n\t<array>\n\t\t<string>" + group + "</string>\n\t</array>\n"
            + "</dict>\n</plist>\n";
    }
}
#endif