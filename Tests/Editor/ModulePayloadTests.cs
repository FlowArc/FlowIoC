using System.IO;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModulePayloadTests
    {
        /// <summary>
        /// Every package ships its modules under the same folder FlowIoC does, so a page written
        /// in another package resolves to that package's Modules~ with nothing declared.
        /// </summary>
        [Test]
        public void The_payload_sits_in_Modules_tilde_inside_the_package()
        {
            var payload = new ModulePayload(Path.Combine("X", "Packages", "Addons"));

            Assert.AreEqual(
                Path.Combine("X", "Packages", "Addons", ModulesSource.ModulesFolder),
                payload.Source().Root);
        }

        [Test]
        public void A_payload_with_a_package_behind_it_is_resolved()
        {
            Assert.IsTrue(new ModulePayload("anywhere").IsResolved);
        }

        /// <summary>
        /// A page compiled outside any package - loose in Assets, say - has no payload to point
        /// at. That is answered rather than guessed, so the page can say so instead of the
        /// installer failing on a path nobody chose.
        /// </summary>
        [Test]
        public void A_page_that_belongs_to_no_package_resolves_to_nothing()
        {
            var payload = new ModulePayload((string) null);

            Assert.IsFalse(payload.IsResolved);
            Assert.IsNull(payload.Source());
        }

        /// <summary>
        /// The assembly this test lives in is compiled from Packages/FlowIoC, so asking it for
        /// its package answers the package rather than nothing. This is the path the adapter
        /// actually takes.
        /// </summary>
        [Test]
        public void An_assembly_compiled_from_a_package_finds_that_package()
        {
            var payload = new ModulePayload(typeof(ModulePayloadTests).Assembly);

            Assert.IsTrue(payload.IsResolved);
            Assert.IsTrue(Directory.Exists(payload.PackageRoot), payload.PackageRoot);
        }
    }
}