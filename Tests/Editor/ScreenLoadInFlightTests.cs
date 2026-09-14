using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.BaseModule.ViewsMediators.Utils;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.Layer;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Two callers can reach one screen entry before its instance exists: an Open, and a preload
    /// that arrives while the Open's load is still out - or the other way round. The load is out
    /// for frames, and whichever caller's continuation lands first used to decide the instance's
    /// fate for both: the preload parked a screen the Open had just put on stage, the Open showed
    /// a screen the preload had just parked and the pool kept it. This is the whole service over
    /// the real models, with the asset service held open until the test says so.
    /// </summary>
    public class ScreenLoadInFlightTests
    {
        private class ProbeScreen : ScreenBody
        {
            // Registering looks for a Root above the screen, and these screens hang under a bare
            // layer object. Marking the screen registered where the service activates it keeps the
            // show path out of the hierarchy walk, which is a scene's business and not this test's.
            public override void BeforeScreenActivation()
            {
                IsRegistered = true;
                base.BeforeScreenActivation();
            }
        }

        private const string KEY = "Probe";

        private readonly List<GameObject> _created = new();

        private FakeAssetService _assets;
        private ScreenRegistryModel _registry;
        private ScreenRuntimeModel _runtime;
        private LoadSubService _load;
        private HideSubService _hide;
        private IScreenBuilderSubService _builder;
        private ScreenLayer _layer;

        [SetUp]
        public void SetUp()
        {
            GameObject prefab = new GameObject("ProbePrefab", typeof(RectTransform));
            prefab.AddComponent<ProbeScreen>();
            prefab.AddComponent<ViewInjector>();
            _created.Add(prefab);

            _assets = new FakeAssetService {Hold = true};
            _assets.Prefabs[KEY] = prefab;

            InjectionBinderCrossContext crossContext = new InjectionBinderCrossContext();
            crossContext.BindInstance<IAssetService>(_assets);

            AddressableLoadSubService addressable = new AddressableLoadSubService();
            AssetTestKit.Inject(addressable, "_crossContext", crossContext);

            _registry = new ScreenRegistryModel();
            _runtime = new ScreenRuntimeModel();
            _load = new LoadSubService();
            _hide = new HideSubService();
            ShowSubService show = new ShowSubService();
            SetupSubService setup = new SetupSubService();
            UnloadSubService unload = new UnloadSubService();
            DisposeSubService dispose = new DisposeSubService();
            ScreenBuilderSubService builder = new ScreenBuilderSubService();

            StandInContext context = new StandInContext();
            context.InjectionBinder.BindInstance<IScreenRegistryModel>(_registry);
            context.InjectionBinder.BindInstance<IScreenRuntimeModel>(_runtime);
            context.InjectionBinder.BindInstance(addressable);
            context.InjectionBinder.BindInstance(new ResourceLoadSubService());
            context.InjectionBinder.BindInstance(_load);
            context.InjectionBinder.BindInstance(_hide);
            context.InjectionBinder.BindInstance(show);
            context.InjectionBinder.BindInstance(setup);
            context.InjectionBinder.BindInstance(unload);
            context.InjectionBinder.BindInstance(dispose);

            context.TryToInjectObject(_load);
            context.TryToInjectObject(_hide);
            context.TryToInjectObject(show);
            context.TryToInjectObject(setup);
            context.TryToInjectObject(unload);
            context.TryToInjectObject(dispose);
            context.TryToInjectObject(builder);
            _builder = builder;

            GameObject layer = new GameObject("Layer_0", typeof(RectTransform));
            _created.Add(layer);
            _layer = layer.AddComponent<ScreenLayer>();

            _registry.RegisterScreenManager(new ScreenManagerVO {ManagerID = 0, ScreenLayerList = {_layer}});
            _registry.RegisterScreen(new ScreenEntry
            {
                ViewType = typeof(ProbeScreen),
                Screen = new ScreenCVO {ManagerId = 0, Layer = 0, Load = ScreenLoadCVO.Addressable(KEY)},
                Owner = context
            });
        }

        [TearDown]
        public void TearDown()
        {
            foreach (IScreenBody loaded in _registry.GetAllLoadedScreens())
            {
                if (loaded.IsAlive())
                    Object.DestroyImmediate(loaded.gameObject);
            }

            foreach (GameObject created in _created)
            {
                if (created != null)
                    Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator An_open_whose_load_is_out_when_the_preload_reaches_its_entry_stays_on_stage()
        {
            Task<ProbeScreen> shown = _builder.Open<ProbeScreen>().Show<ProbeScreen>();

            bool preloaded = false;
            _load.All(completeCallback: () => preloaded = true);

            Assert.AreEqual(1, _assets.Claims.Count, "the two callers share one load");

            _assets.Complete(KEY);
            yield return AssetTestKit.Until(shown);
            yield return Until(() => preloaded);

            ProbeScreen screen = shown.Result;
            Assert.IsNotNull(screen);
            Assert.AreEqual(1, _registry.GetAllLoadedScreens().Count, "one instance for the entry");
            Assert.IsTrue(screen.Data.HasState(ScreenState.InUse));
            Assert.IsFalse(screen.Data.HasState(ScreenState.InPool));
            Assert.AreSame(_layer.transform, screen.transform.parent, "the screen stays on its layer");
            Assert.IsFalse(_runtime.GetScreen<ProbeScreen>(0, out _), "a screen on stage is not in the pool");
        }

        /// <summary>
        /// The hide after that race is where the old bug surfaced: the preload had parked the
        /// screen while it was on stage, and the hide parked it again - a warning, and the same
        /// instance twice in the list. Parked once means handed out once.
        /// </summary>
        [UnityTest]
        public IEnumerator After_that_race_a_hide_parks_the_screen_once()
        {
            Task<ProbeScreen> shown = _builder.Open<ProbeScreen>().Show<ProbeScreen>();
            bool preloaded = false;
            _load.All(completeCallback: () => preloaded = true);
            _assets.Complete(KEY);
            yield return AssetTestKit.Until(shown);
            yield return Until(() => preloaded);

            _hide.Screen(shown.Result);

            Assert.IsTrue(_runtime.GetScreen<ProbeScreen>(0, out ProbeScreen first));
            Assert.AreSame(shown.Result, first);
            Assert.IsFalse(_runtime.GetScreen<ProbeScreen>(0, out _), "parked once, so handed out once");
        }

        [UnityTest]
        public IEnumerator An_open_that_joins_a_preload_already_out_takes_the_instance_out_of_the_pool()
        {
            bool preloaded = false;
            _load.All(completeCallback: () => preloaded = true);

            Task<ProbeScreen> shown = _builder.Open<ProbeScreen>().Show<ProbeScreen>();

            Assert.AreEqual(1, _assets.Claims.Count, "the two callers share one load");

            _assets.Complete(KEY);
            yield return Until(() => preloaded);
            yield return AssetTestKit.Until(shown);

            ProbeScreen screen = shown.Result;
            Assert.IsNotNull(screen);
            Assert.AreEqual(1, _registry.GetAllLoadedScreens().Count, "one instance for the entry");
            Assert.IsTrue(screen.Data.HasState(ScreenState.InUse));
            Assert.IsFalse(screen.Data.HasState(ScreenState.InPool));
            Assert.AreSame(_layer.transform, screen.transform.parent, "the screen is on its layer");
            Assert.IsFalse(_runtime.GetScreen<ProbeScreen>(0, out _), "the pool no longer holds a screen that is on stage");
        }

        [UnityTest]
        public IEnumerator A_preload_alone_parks_the_screen()
        {
            bool preloaded = false;
            _load.All(completeCallback: () => preloaded = true);

            _assets.Complete(KEY);
            yield return Until(() => preloaded);

            Assert.IsTrue(_runtime.GetScreen<ProbeScreen>(0, out ProbeScreen pooled));
            Assert.AreSame(_registry.GetAllLoadedScreens()[0], pooled);
        }

        private static IEnumerator Until(System.Func<bool> condition, int frames = 50)
        {
            for (int i = 0; i < frames && !condition(); i++)
                yield return null;

            Assert.IsTrue(condition(), "the condition did not hold within the frame budget");
        }
    }
}
