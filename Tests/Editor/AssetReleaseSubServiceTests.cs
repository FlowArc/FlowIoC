using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Ownership decides when an asset really goes: a claim per owner, and the handle released
    /// when the last claim does.
    /// </summary>
    public class AssetReleaseSubServiceTests
    {
        private AssetTestKit _kit;

        [SetUp]
        public void SetUp() => _kit = new AssetTestKit();

        private IEnumerator Loaded(string key, params string[] owners)
        {
            Task<string> last = null;

            foreach (string owner in owners)
                last = _kit.Load.LoadAssetAsync<string>(key, owner);

            _kit.Gateway.Complete(key, key + "-asset");
            yield return AssetTestKit.Until(last);
        }

        [UnityTest]
        public IEnumerator Release_by_one_owner_keeps_the_asset_while_another_holds_it()
        {
            yield return Loaded("a", "Pool/x", "Screen/0");

            _kit.Release.Release("a", "Pool/x");

            Assert.AreEqual(0, _kit.Gateway.Handles["a"].Releases);
            Assert.IsTrue(_kit.Registry.Entries.ContainsKey("a"));
            CollectionAssert.DoesNotContain(_kit.Registry.Groups["Pool/x"].Keys, "a");

            _kit.Release.Release("a", "Screen/0");

            Assert.AreEqual(1, _kit.Gateway.Handles["a"].Releases);
            Assert.IsFalse(_kit.Registry.Entries.ContainsKey("a"));
        }

        [UnityTest]
        public IEnumerator ReleaseGroup_drops_every_claim_and_releases_the_orphans()
        {
            string released = null;
            _kit.Signals.Outgoing.GroupReleased.AddListener(group => released = group);

            yield return Loaded("a", "g");
            yield return Loaded("b", "g", "h");

            _kit.Release.ReleaseGroup("g");

            Assert.AreEqual(1, _kit.Gateway.Handles["a"].Releases);
            Assert.AreEqual(0, _kit.Gateway.Handles["b"].Releases);
            Assert.IsFalse(_kit.Registry.Groups.ContainsKey("g"));
            CollectionAssert.AreEquivalent(new[] {"h"}, _kit.Registry.Entries["b"].Owners);
            Assert.AreEqual("g", released);
        }

        [UnityTest]
        public IEnumerator An_unscoped_claim_counts()
        {
            Task<string> first = _kit.Load.LoadAssetAsync<string>("a");
            Task<string> second = _kit.Load.LoadAssetAsync<string>("a");
            _kit.Gateway.Complete("a", "asset");
            yield return AssetTestKit.Until(first);
            yield return AssetTestKit.Until(second);

            _kit.Release.Release("a");
            Assert.AreEqual(0, _kit.Gateway.Handles["a"].Releases);

            _kit.Release.Release("a");
            Assert.AreEqual(1, _kit.Gateway.Handles["a"].Releases);
        }

        [Test]
        public void Releasing_what_was_never_loaded_is_quiet()
        {
            _kit.Release.Release("nothing", "Pool/x");
            _kit.Release.ReleaseGroup("nothing");

            Assert.AreEqual(0, _kit.Registry.Entries.Count);
        }
    }
}
