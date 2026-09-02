using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The segments a file's folders add to its module namespace. It is the reading half of the
    /// same convention DotSettingsPlan writes: a folder the layout does not treat as a namespace
    /// provider is structure, so it names nothing.
    ///
    /// The picker is handed the skip list rather than reading a settings file, which is what lets
    /// these tests run with no project on disk - and what stopped the generator from writing
    /// Modules.PlayerModule.Scripts.Shared.Signals while every module already on disk said
    /// Modules.PlayerModule.Shared.Signals.
    /// </summary>
    public class FolderNamespaceSegmentsTests
    {
        private const string MODULE = "C:/proj/Assets/Modules/PlayerModule";

        private static List<string> Segments(string fileDirectory, params string[] skip) =>
            new FolderNamespaceSegments().Between(MODULE, fileDirectory, skip).ToList();

        /// <summary>
        /// Scripts is structure, not namespace, so the public signal holder lands in
        /// Modules.PlayerModule.Shared.Signals rather than one segment deeper.
        /// </summary>
        [Test]
        public void A_folder_the_layout_skips_names_nothing()
        {
            CollectionAssert.AreEqual(
                new[] {"Shared", "Signals"},
                Segments(MODULE + "/Scripts/Shared/Signals", MODULE + "/Scripts"));
        }

        [Test]
        public void Every_skipped_folder_on_the_way_down_is_dropped()
        {
            CollectionAssert.AreEqual(
                new[] {"Signals"},
                Segments(MODULE + "/Scripts/Runtime/Signals",
                    MODULE + "/Scripts",
                    MODULE + "/Scripts/Runtime"));
        }

        [Test]
        public void A_folder_that_is_not_skipped_becomes_a_segment()
        {
            CollectionAssert.AreEqual(
                new[] {"Scripts", "Runtime", "Models"},
                Segments(MODULE + "/Scripts/Runtime/Models"));
        }

        /// <summary>
        /// A file sitting in the module folder itself is in the module's own namespace, with
        /// nothing added.
        /// </summary>
        [Test]
        public void The_module_folder_itself_adds_no_segment()
        {
            CollectionAssert.IsEmpty(Segments(MODULE));
        }

        /// <summary>
        /// The skip list is built from paths Unity and the layout assembled separately, so the
        /// two sides meet with different separators and different casing on Windows. A namespace
        /// that changed with the shape of a path would be a bug nobody could see.
        /// </summary>
        [Test]
        public void Separators_and_casing_do_not_change_the_answer()
        {
            CollectionAssert.AreEqual(
                new[] {"Shared", "Signals"},
                new FolderNamespaceSegments()
                    .Between(
                        @"C:\proj\Assets\Modules\PlayerModule",
                        @"C:\proj\Assets\Modules\PlayerModule\Scripts\Shared\Signals",
                        new[] {"c:/PROJ/assets/modules/playermodule/scripts"})
                    .ToList());
        }

        /// <summary>
        /// A directory that is not under the module cannot describe the module's namespace, so
        /// the caller is told nothing rather than handed a walk up out of the module.
        /// </summary>
        [Test]
        public void A_directory_outside_the_module_adds_no_segment()
        {
            CollectionAssert.IsEmpty(Segments("C:/proj/Assets/Modules/OtherModule/Scripts"));
        }
    }
}
