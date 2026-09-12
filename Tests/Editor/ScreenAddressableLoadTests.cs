using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The screen's addressable loader keeps no cache: it claims the prefab from the asset service
    /// under its manager's owner id, instantiates, and releases the claim on unload.
    /// </summary>
    public class ScreenAddressableLoadTests
    {
        private class ProbeScreen : ScreenBody
        {
        }

        private InjectionBinderCrossContext _crossContext;
        private FakeAssetService _assets;
        private AddressableLoadSubService _loader;
        private GameObject _prefab;
        private ScreenEntry _entry;

        [SetUp]
        public void SetUp()
        {
            _crossContext = new InjectionBinderCrossContext();
            _assets = new FakeAssetService();
            _loader = new AddressableLoadSubService();
            AssetTestKit.Inject(_loader, "_crossContext", _crossContext);

            _prefab = new GameObject("ProbePrefab");
            _prefab.AddComponent<ProbeScreen>();
            _assets.Prefabs["Probe"] = _prefab;

            _entry = new ScreenEntry
            {
                ViewType = typeof(ProbeScreen),
                Screen = new ScreenCVO {ManagerId = 2, Load = ScreenLoadCVO.Addressable("Probe")}
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_entry.Loaded != null && _entry.Loaded.gameObject != null)
                Object.DestroyImmediate(_entry.Loaded.gameObject);

            Object.DestroyImmediate(_prefab);
        }

        private void BindAssets() => _crossContext.BindInstance<IAssetService>(_assets);

        [UnityTest]
        public IEnumerator The_screen_is_claimed_under_its_manager_and_instantiated()
        {
            BindAssets();

            Task<IScreenBody> task = _loader.LoadScreen(_entry);
            yield return AssetTestKit.Until(task);

            Assert.AreEqual(("Probe", "Screen/2"), _assets.Claims[0]);
            Assert.IsNotNull(task.Result);
            Assert.AreSame(task.Result, _entry.Loaded);
            Assert.IsFalse(task.Result.gameObject.activeSelf);
            Assert.AreNotSame(_prefab, task.Result.gameObject);
        }

        [UnityTest]
        public IEnumerator Unload_releases_the_claim_and_destroys()
        {
            BindAssets();
            Task<IScreenBody> task = _loader.LoadScreen(_entry);
            yield return AssetTestKit.Until(task);
            GameObject instance = task.Result.gameObject;

            // The loader destroys with the runtime Destroy, which edit mode refuses and reports; the
            // instance is cleaned up by hand below.
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            _loader.UnloadScreen(_entry, task.Result);

            Assert.AreEqual(("Probe", "Screen/2"), _assets.Releases[0]);
            Assert.IsNull(_entry.Loaded);
            Object.DestroyImmediate(instance);
        }

        [UnityTest]
        public IEnumerator A_second_load_of_a_loaded_entry_is_the_same_screen()
        {
            BindAssets();
            Task<IScreenBody> first = _loader.LoadScreen(_entry);
            yield return AssetTestKit.Until(first);

            Task<IScreenBody> second = _loader.LoadScreen(_entry);
            yield return AssetTestKit.Until(second);

            Assert.AreSame(first.Result, second.Result);
        }

        [UnityTest]
        public IEnumerator With_no_asset_service_in_the_scene_the_load_names_the_root()
        {
            LogAssert.Expect(LogType.Error, new Regex("ProbeScreen.*AssetServiceRoot"));

            Task<IScreenBody> task = _loader.LoadScreen(_entry);
            yield return AssetTestKit.Until(task);

            Assert.IsNull(task.Result);
            Assert.AreEqual(0, _assets.Claims.Count);
        }
    }
}
