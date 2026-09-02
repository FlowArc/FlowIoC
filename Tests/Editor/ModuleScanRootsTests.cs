using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.ModuleScan;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleScanRootsTests
    {
        /// <summary>
        /// The index rebuild and the scan have to agree on what a module is. This class is the
        /// one place that answers it, so it is asked about directories rather than about the
        /// disk - which is also what lets a test describe a project shape that is not this one.
        /// </summary>
        [Test]
        public void Assets_Modules_is_always_the_first_root()
        {
            var roots = new ModuleScanRoots(
                folderExists: path => true,
                directoriesIn: path => new string[0],
                directoriesNamed: (path, name) => new string[0]);

            List<string> result = roots.All("C:/proj").ToList();

            Assert.AreEqual("C:/proj/Assets/Modules", result[0].Replace('\\', '/'));
        }

        /// <summary>
        /// The private addons package ships modules of its own. A scan that missed them would
        /// report the index as drifted against modules it never looked at.
        /// </summary>
        [Test]
        public void A_Modules_folder_inside_an_embedded_package_is_a_root_too()
        {
            var roots = new ModuleScanRoots(
                folderExists: path => true,
                directoriesIn: path => new[] {"C:/proj/Packages/FlowIoC-addons"},
                directoriesNamed: (path, name) => new[] {"C:/proj/Packages/FlowIoC-addons/PrivateModules~/Modules"});

            List<string> result = roots.All("C:/proj").Select(path => path.Replace('\\', '/')).ToList();

            CollectionAssert.Contains(result, "C:/proj/Packages/FlowIoC-addons/PrivateModules~/Modules");
        }

        [Test]
        public void A_project_with_no_Packages_folder_yields_only_Assets_Modules()
        {
            var roots = new ModuleScanRoots(
                folderExists: path => path.Replace('\\', '/').EndsWith("Assets/Modules"),
                directoriesIn: path => new string[0],
                directoriesNamed: (path, name) => new string[0]);

            Assert.AreEqual(1, roots.All("C:/proj").Count());
        }

        /// <summary>
        /// A project whose Assets/Modules has not been created yet is not an error - a fresh
        /// install has none - and it must not stop the embedded packages from being scanned.
        /// </summary>
        [Test]
        public void A_project_with_no_Assets_Modules_still_scans_its_packages()
        {
            var roots = new ModuleScanRoots(
                folderExists: path => !path.Replace('\\', '/').EndsWith("Assets/Modules"),
                directoriesIn: path => new[] {"C:/proj/Packages/FlowIoC-addons"},
                directoriesNamed: (path, name) => new[] {"C:/proj/Packages/FlowIoC-addons/Modules"});

            List<string> result = roots.All("C:/proj").Select(path => path.Replace('\\', '/')).ToList();

            CollectionAssert.AreEqual(new[] {"C:/proj/Packages/FlowIoC-addons/Modules"}, result);
        }
    }
}
