using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class ModuleAssetPathResolverTests
    {
        /// <summary>
        /// ModuleRegistry.PathOf returns empty for a descriptor whose FolderGuid no longer
        /// resolves. A plain "Assets".Length substring throws ArgumentOutOfRangeException on
        /// that input; the resolver has to treat it as "nothing to resolve" instead.
        /// </summary>
        [Test]
        public void An_empty_asset_path_resolves_to_empty()
        {
            Assert.AreEqual(string.Empty, new ModuleAssetPathResolver().ToAbsolutePath(string.Empty));
        }

        [Test]
        public void A_null_asset_path_resolves_to_empty()
        {
            Assert.AreEqual(string.Empty, new ModuleAssetPathResolver().ToAbsolutePath(null));
        }

        [Test]
        public void An_Assets_path_resolves_under_Application_dataPath()
        {
            string result = new ModuleAssetPathResolver().ToAbsolutePath("Assets/Modules/CameraModule");

            string expected = Path.GetFullPath(Path.Combine(Application.dataPath, "Modules", "CameraModule"));
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// ModuleIndexRebuilder also scans Packages/*/Modules, so PathOf can legitimately hand
        /// back a path rooted at "Packages/..." for an embedded package module. The old
        /// substring-strip approach assumed every path started with "Assets" and produced
        /// garbage for this case; resolving against the project root instead handles both roots
        /// the same way.
        /// </summary>
        [Test]
        public void A_Packages_path_resolves_under_the_project_root_not_under_Assets()
        {
            string result = new ModuleAssetPathResolver().ToAbsolutePath("Packages/FlowIoC/Editor/Modules");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string expected = Path.GetFullPath(Path.Combine(projectRoot, "Packages/FlowIoC/Editor/Modules"));
            Assert.AreEqual(expected, result);
        }
    }
}
