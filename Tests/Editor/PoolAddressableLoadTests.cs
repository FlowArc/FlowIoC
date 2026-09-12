using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.PoolModule.Components;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Services.Sub.Load;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The pool's addressable loader keeps no cache and no statics: it claims the prefab from the
    /// asset service under its group's owner id, instantiates or only holds, and releases.
    /// </summary>
    public class PoolAddressableLoadTests
    {
        private const string GUID = "0123456789abcdef0123456789abcdef";

        private InjectionBinderCrossContext _crossContext;
        private FakeAssetService _assets;
        private AddressableLoadSubService _loader;
        private AssetReferenceSpawnableObject _reference;
        private GameObject _prefab;
        private GameObject _instance;

        [SetUp]
        public void SetUp()
        {
            _crossContext = new InjectionBinderCrossContext();
            _assets = new FakeAssetService();
            _loader = new AddressableLoadSubService();
            AssetTestKit.Inject(_loader, "_crossContext", _crossContext);

            _reference = new AssetReferenceSpawnableObject(GUID);
            _prefab = new GameObject("ProbePrefab");
            _prefab.AddComponent<PoolableItem>();
            _assets.Prefabs[GUID] = _prefab;
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
            Object.DestroyImmediate(_prefab);
        }

        private void BindAssets() => _crossContext.BindInstance<IAssetService>(_assets);

        [UnityTest]
        public IEnumerator An_item_is_claimed_under_its_group_and_instantiated()
        {
            BindAssets();

            Task<IPoolableItem> task = _loader.LoadItem(_reference, "g");
            yield return AssetTestKit.Until(task);
            _instance = task.Result?.transform.gameObject;

            Assert.AreEqual((GUID, "Pool/g"), _assets.Claims[0]);
            Assert.IsNotNull(task.Result);
            Assert.AreNotSame(_prefab, _instance);
        }

        [UnityTest]
        public IEnumerator Preload_claims_without_instantiating()
        {
            BindAssets();

            Task task = _loader.PreloadItemAsync(_reference, "g");
            yield return AssetTestKit.Until(task);

            Assert.AreEqual((GUID, "Pool/g"), _assets.Claims[0]);
            Assert.AreEqual(1, Object.FindObjectsByType<PoolableItem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
        }

        [Test]
        public void Unload_releases_under_the_group()
        {
            BindAssets();
            _loader.PreloadItemAsync(_reference, "g");

            _loader.UnloadItem(_reference, "g");

            Assert.AreEqual((GUID, "Pool/g"), _assets.Releases[0]);
        }

        [UnityTest]
        public IEnumerator With_no_asset_service_in_the_scene_the_load_names_the_root()
        {
            LogAssert.Expect(LogType.Error, new Regex(GUID + ".*AssetServiceRoot"));

            Task<IPoolableItem> task = _loader.LoadItem(_reference, "g");
            yield return AssetTestKit.Until(task);

            Assert.IsNull(task.Result);
            Assert.AreEqual(0, _assets.Claims.Count);
        }

        [Test]
        public void An_invalid_reference_is_an_error_and_claims_nothing()
        {
            BindAssets();
            LogAssert.Expect(LogType.Error, new Regex("Invalid AssetReference in PreloadItemAsync"));

            Task task = _loader.PreloadItemAsync(new AssetReferenceSpawnableObject(""), "g");

            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(0, _assets.Claims.Count);
        }
    }
}
