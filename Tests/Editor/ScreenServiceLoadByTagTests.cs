using System.Collections;
using System.Collections.Generic;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.BaseModule.ViewsMediators.Utils;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.Service;
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
    /// The step a game binds to put a tag's screens in the pool before a later step opens one of
    /// them. What it promises the sequence is the retain: held until the last screen of the tag is
    /// in, and not a screen of any other tag.
    /// </summary>
    public class ScreenServiceLoadByTagTests
    {
        private class TaggedScreen : ScreenBody
        {
        }

        private class OtherScreen : ScreenBody
        {
        }

        /// <summary>Counts the release instead of handing it to a group this test does not run.</summary>
        private class RecordingCommand : IScreenService.Commands.LoadByTag
        {
            public int Released;

            public override void Release(params object[] commandGroupData) => Released++;
        }

        /// <summary>The service as the command sees it: the load sub service and nothing else.</summary>
        private class LoadOnlyScreenService : IScreenService
        {
            public LoadSubService Load { get; set; }
            public CheckSubService Check => null;
            public TryGetSubService TryGet => null;
            public HideSubService Hide => null;
            public UnloadSubService Unload => null;
            public IScreenBuilder Open<T>(int managerId = 0) where T : IScreenBody => null;
        }

        private readonly List<GameObject> _created = new();

        private ScreenRegistryModel _registry;
        private ScreenRuntimeModel _runtime;
        private RecordingCommand _command;

        [SetUp]
        public void SetUp()
        {
            FakeAssetService assets = new FakeAssetService();
            assets.Prefabs["Tagged"] = Prefab<TaggedScreen>("Tagged");
            assets.Prefabs["Other"] = Prefab<OtherScreen>("Other");

            InjectionBinderCrossContext crossContext = new InjectionBinderCrossContext();
            crossContext.BindInstance<IAssetService>(assets);

            AddressableLoadSubService addressable = new AddressableLoadSubService();
            AssetTestKit.Inject(addressable, "_crossContext", crossContext);

            _registry = new ScreenRegistryModel();
            _runtime = new ScreenRuntimeModel();
            LoadSubService load = new LoadSubService();

            StandInContext context = new StandInContext();
            context.InjectionBinder.BindInstance<IScreenRegistryModel>(_registry);
            context.InjectionBinder.BindInstance<IScreenRuntimeModel>(_runtime);
            context.InjectionBinder.BindInstance(addressable);
            context.InjectionBinder.BindInstance(new ResourceLoadSubService());
            context.TryToInjectObject(load);

            _registry.RegisterScreen(Entry<TaggedScreen>("Tagged", ScreenTag.GroupA, context));
            _registry.RegisterScreen(Entry<OtherScreen>("Other", ScreenTag.Default, context));

            // Through the binder rather than by reflection on the command: the property is declared
            // on the base, and a lookup on the recording subclass does not see a private member of it.
            context.InjectionBinder.BindInstance<IScreenService>(new LoadOnlyScreenService {Load = load});
            _command = new RecordingCommand();
            context.TryToInjectObject(_command);
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

        private GameObject Prefab<T>(string name) where T : ScreenBody
        {
            GameObject prefab = new GameObject(name, typeof(RectTransform));
            prefab.AddComponent<T>();
            prefab.AddComponent<ViewInjector>();
            _created.Add(prefab);
            return prefab;
        }

        private static ScreenEntry Entry<T>(string key, ScreenTag tag, StandInContext owner) => new()
        {
            ViewType = typeof(T),
            Screen = new ScreenCVO {ManagerId = 0, Layer = 0, Tag = tag, Load = ScreenLoadCVO.Addressable(key)},
            Owner = owner
        };

        [UnityTest]
        public IEnumerator The_step_holds_the_sequence_until_the_tag_s_screens_are_in_the_pool()
        {
            _command.Execute(ScreenTag.GroupA);

            Assert.IsTrue(_command.HasRetain, "the step retains before the load is out");

            yield return Until(() => _command.Released == 1);

            Assert.IsTrue(_runtime.GetScreen<TaggedScreen>(0, out _), "the tag's screen is in the pool");
            Assert.IsFalse(_runtime.GetScreen<OtherScreen>(0, out _), "a screen of another tag is not loaded");
            Assert.AreEqual(1, _registry.GetAllLoadedScreens().Count);
        }

        [UnityTest]
        public IEnumerator A_tag_with_no_screens_releases_at_once()
        {
            _command.Execute(ScreenTag.GroupH);

            yield return Until(() => _command.Released == 1);

            Assert.AreEqual(0, _registry.GetAllLoadedScreens().Count);
        }

        private static IEnumerator Until(System.Func<bool> condition, int frames = 50)
        {
            for (int i = 0; i < frames && !condition(); i++)
                yield return null;

            Assert.IsTrue(condition(), "the condition did not hold within the frame budget");
        }
    }
}
