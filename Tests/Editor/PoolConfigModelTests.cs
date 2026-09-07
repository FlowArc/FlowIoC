using FlowIoC.PoolModule.Models.Config;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The config model answers for items it was never told about. Every pool service asks it for
    /// an item's group before doing anything with the item, and a key nobody registered is the
    /// ordinary way a typo reaches the pool - so the answer is null and the caller's report, not a
    /// thrown dictionary key.
    /// </summary>
    public class PoolConfigModelTests
    {
        [Test]
        public void An_item_registered_nowhere_has_no_group()
        {
            IPoolConfigModel model = new PoolConfigModel();

            Assert.That(model.GetGroupConfigOfItem("nobody-registered-this"), Is.Null);
        }
    }
}
