using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The load-once promise, proved without Addressables: the fake gateway hands out a handle per
    /// key and completes it when the test says.
    /// </summary>
    public class AssetLoadSubServiceTests
    {
        private AssetTestKit _kit;

        [SetUp]
        public void SetUp() => _kit = new AssetTestKit();

        [UnityTest]
        public IEnumerator A_key_asked_twice_is_loaded_once_and_both_get_the_result()
        {
            Task<string> first = _kit.Load.LoadAssetAsync<string>("a", "Pool/x");
            Task<string> second = _kit.Load.LoadAssetAsync<string>("a", "Screen/0");

            Assert.AreEqual(1, _kit.Gateway.Loads);

            _kit.Gateway.Complete("a", "asset");
            yield return AssetTestKit.Until(first);
            yield return AssetTestKit.Until(second);

            Assert.AreEqual("asset", first.Result);
            Assert.AreEqual("asset", second.Result);
            CollectionAssert.AreEquivalent(new[] {"Pool/x", "Screen/0"}, _kit.Registry.Entries["a"].Owners);
        }

        [UnityTest]
        public IEnumerator A_second_owner_on_a_loaded_key_claims_without_a_load()
        {
            Task<string> first = _kit.Load.LoadAssetAsync<string>("a", "Pool/x");
            _kit.Gateway.Complete("a", "asset");
            yield return AssetTestKit.Until(first);

            Task<string> second = _kit.Load.LoadAssetAsync<string>("a", "Screen/0");
            yield return AssetTestKit.Until(second);

            Assert.AreEqual(1, _kit.Gateway.Loads);
            Assert.AreEqual("asset", second.Result);
            CollectionAssert.Contains(_kit.Registry.Entries["a"].Owners, "Screen/0");
            CollectionAssert.Contains(_kit.Registry.Groups["Screen/0"].Keys, "a");
        }

        [UnityTest]
        public IEnumerator A_failed_load_hard_releases_and_dispatches()
        {
            string failed = null;
            _kit.Signals.Outgoing.AssetLoadFailed.AddListener(key => failed = key);
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("Load failed: a"));

            Task<string> task = _kit.Load.LoadAssetAsync<string>("a", "Pool/x");
            _kit.Gateway.Fail("a");
            yield return AssetTestKit.Until(task);

            Assert.IsNull(task.Result);
            Assert.AreEqual("a", failed);
            Assert.IsFalse(_kit.Registry.Entries.ContainsKey("a"));
            Assert.AreEqual(1, _kit.Gateway.Handles["a"].Releases);
        }

        [UnityTest]
        public IEnumerator The_sync_load_answers_from_the_cache()
        {
            Task<string> task = _kit.Load.LoadAssetAsync<string>("a");
            _kit.Gateway.Complete("a", "asset");
            yield return AssetTestKit.Until(task);

            string cached = _kit.Load.LoadAsset<string>("a");

            Assert.AreEqual("asset", cached);
            Assert.AreEqual(1, _kit.Gateway.Loads);
            Assert.AreEqual(2, _kit.Registry.Entries["a"].UnscopedClaims);
        }

        [Test]
        public void TryGetAsset_answers_only_what_is_loaded()
        {
            _kit.Load.LoadAssetAsync<string>("a");

            Assert.IsFalse(_kit.Load.TryGetAsset("a", out string _));

            _kit.Gateway.Complete("a", "asset");

            Assert.IsTrue(_kit.Load.TryGetAsset("a", out string asset));
            Assert.AreEqual("asset", asset);
        }

        [Test]
        public void An_invalid_key_is_an_error_and_loads_nothing()
        {
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("invalid key"));

            Task<string> task = _kit.Load.LoadAssetAsync<string>("");

            Assert.IsTrue(task.IsCompleted);
            Assert.IsNull(task.Result);
            Assert.AreEqual(0, _kit.Gateway.Loads);
        }
    }
}
