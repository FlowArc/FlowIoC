using System.IO;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class PendingInstallPayloadTests
    {
        [Test]
        public void A_payload_that_names_a_package_is_complete()
        {
            Assert.IsTrue(new PendingInstallPayload("root").IsComplete);
        }

        [Test]
        public void A_payload_that_names_none_is_not()
        {
            Assert.IsFalse(new PendingInstallPayload(null).IsComplete);
            Assert.IsFalse(new PendingInstallPayload(string.Empty).IsComplete);
        }

        /// <summary>
        /// Every package ships its modules under Modules~, so the package root is the whole of
        /// what has to survive the domain reload.
        /// </summary>
        [Test]
        public void A_complete_payload_reads_the_package_it_was_written_from()
        {
            var payload = new PendingInstallPayload(Path.Combine("X", "Addons"));

            Assert.AreEqual(
                Path.Combine("X", "Addons", ModulesSource.ModulesFolder), payload.Source().Root);
        }

        /// <summary>
        /// Everything written before this existed named no payload, and every module FlowIoC
        /// itself ships is in Modules~. An incomplete payload therefore resumes where it always
        /// did rather than refusing.
        /// </summary>
        [Test]
        public void An_incomplete_payload_falls_back_to_the_modules_FlowIoC_ships()
        {
            ModulesSource source = new PendingInstallPayload(null).Source();

            StringAssert.EndsWith(ModulesSource.ModulesFolder, source.Root);
        }
    }
}