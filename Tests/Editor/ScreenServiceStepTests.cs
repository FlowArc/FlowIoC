using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.Service;
using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The hide and unload steps a game binds. Each forwards to a sub service and returns, so what
    /// is tested is that the screens the runtime model answers with are the ones that end up
    /// parked - through the real HideSubService and UnloadSubService, on a runtime model that only
    /// records. LoadAll and LoadByTag, which hold the sequence, have a fixture of their own in
    /// ScreenServiceLoadByTagTests.
    /// </summary>
    public class ScreenServiceStepTests
    {
        private class TestScreen : ScreenBody
        {
            public int ScreenHiddenCalls;

            public override void ScreenHidden()
            {
                ScreenHiddenCalls++;
                base.ScreenHidden();
            }
        }

        private class FakeScreenRuntimeModel : IScreenRuntimeModel
        {
            public readonly List<IScreenBody> AddedToPassive = new();
            public readonly List<IScreenBody> RemovedFromActive = new();
            public List<IScreenBody> ActiveScreens = new();
            public ScreenTag TagAsked = ScreenTag.Default;

            public void AddToPassivePool(IScreenBody screenBody) => AddedToPassive.Add(screenBody);
            public void AddToActivePools(IScreenBody screenBody) { }
            public void RemoveFromActivePools(IScreenBody screenBody) => RemovedFromActive.Add(screenBody);
            public void RemoveFromPassivePool(IScreenBody screenBody) { }

            public bool GetScreen<T>(int managerId, out T screen) where T : IScreenBody
            {
                screen = default;
                return false;
            }

            public bool GetScreen(int managerId, Type screenType, out IScreenBody screen)
            {
                screen = null;
                return false;
            }

            public bool IsLayerFull(int layerIndex, int managerId, out IScreenBody screenBody)
            {
                screenBody = null;
                return false;
            }

            public bool IsScreenActive(Type screenType, int managerId, out IScreenBody screenBody)
            {
                screenBody = null;
                return false;
            }

            public List<IScreenBody> GetAllActiveScreens() => ActiveScreens;

            public bool GetActiveManagerScreens(int managerId, out List<IScreenBody> list)
            {
                list = ActiveScreens;
                return ActiveScreens.Count > 0;
            }

            public bool GetActiveTagScreens(ScreenTag tag, int managerId, out List<IScreenBody> list)
            {
                TagAsked = tag;
                list = ActiveScreens;
                return ActiveScreens.Count > 0;
            }
        }

        /// <summary>Answers the loaded screens the test gave it, by tag or all at once.</summary>
        private class FakeScreenRegistryModel : IScreenRegistryModel
        {
            public readonly List<ScreenEntry> Entries = new();
            public ScreenTag TagAsked = ScreenTag.Default;

            public void RegisterScreenManager(ScreenManagerVO manager) { }
            public ScreenManagerVO GetScreenManager(int managerId) => null;
            public bool RegisterScreen(ScreenEntry entry) => false;

            public bool TryGetEntry(int managerId, Type viewType, out ScreenEntry entry)
            {
                entry = null;
                return false;
            }

            public ScreenEntry GetEntry(int managerId, Type viewType) => null;
            public void RemoveEntry(ScreenEntry entry) { }
            public List<ScreenEntry> GetAllEntries() => Entries;
            public List<ScreenEntry> GetManagerEntries(int managerId) => Entries;

            public List<ScreenEntry> GetTagEntries(ScreenTag tag)
            {
                TagAsked = tag;
                return Entries;
            }

            public List<IScreenBody> GetAllLoadedScreens()
            {
                var loaded = new List<IScreenBody>();
                foreach (ScreenEntry entry in Entries)
                    if (entry.Loaded != null)
                        loaded.Add(entry.Loaded);
                return loaded;
            }

            public List<IScreenBody> GetAllScreensAtManager(int managerId) => GetAllLoadedScreens();
            public void CopyDataFromConfig(ScreenVO screenData) { }
            public void CopyDataFromConfig(ScreenVO screenData, ScreenCVO screen) { }
        }

        /// <summary>The service as the steps see it: the two sub services under test and nothing else.</summary>
        private class HideUnloadScreenService : IScreenService
        {
            public LoadSubService Load => null;
            public CheckSubService Check => null;
            public TryGetSubService TryGet => null;
            public HideSubService Hide { get; set; }
            public UnloadSubService Unload { get; set; }
            public IScreenBuilder Open<T>(int managerId = 0) where T : IScreenBody => null;
        }

        private StandInContext _context;
        private FakeScreenRuntimeModel _runtime;
        private FakeScreenRegistryModel _registry;
        private HideSubService _hide;
        private readonly List<GameObject> _hosts = new();

        [SetUp]
        public void SetUp()
        {
            _context = new StandInContext();
            _runtime = new FakeScreenRuntimeModel();
            _registry = new FakeScreenRegistryModel();
            _hide = new HideSubService();
            var unload = new UnloadSubService();
            var dispose = new DisposeSubService();
            var setup = new SetupSubService();

            _context.InjectionBinder.BindInstance<IScreenRuntimeModel>(_runtime);
            _context.InjectionBinder.BindInstance<IScreenRegistryModel>(_registry);
            _context.InjectionBinder.BindInstance<SetupSubService>(setup);
            _context.InjectionBinder.BindInstance<LoadSubService>(new LoadSubService());
            _context.InjectionBinder.BindInstance<UnloadSubService>(unload);
            _context.InjectionBinder.BindInstance<DisposeSubService>(dispose);
            _context.InjectionBinder.BindInstance<AddressableLoadSubService>(new AddressableLoadSubService());
            _context.InjectionBinder.BindInstance<ResourceLoadSubService>(new ResourceLoadSubService());
            _context.InjectionBinder.BindInstance<HideSubService>(_hide);
            _context.InjectionBinder.BindInstance<ShowSubService>(new ShowSubService());
            _context.TryToInjectObject(_hide);
            _context.TryToInjectObject(setup);
            _context.TryToInjectObject(unload);
            _context.TryToInjectObject(dispose);

            _context.InjectionBinder.BindInstance<IScreenService>(new HideUnloadScreenService {Hide = _hide, Unload = unload});
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _hosts.Count; i++)
            {
                if (_hosts[i] != null)
                    Object.DestroyImmediate(_hosts[i]);
            }

            _hosts.Clear();
        }

        private TestScreen NewScreen()
        {
            var host = new GameObject("TestScreen");
            _hosts.Add(host);
            TestScreen screen = host.AddComponent<TestScreen>();
            screen.Data.ScreenType = typeof(TestScreen);
            screen.IsRegistered = true;
            screen.Data.AddState(ScreenState.InUse);
            _hide.Setup(screen);
            return screen;
        }

        private T Step<T>() where T : new()
        {
            var step = new T();
            _context.TryToInjectObject(step);
            return step;
        }

        [Test]
        public void HideAll_parks_every_screen_on_stage()
        {
            TestScreen first = NewScreen();
            TestScreen second = NewScreen();
            _runtime.ActiveScreens = new List<IScreenBody> {first, second};

            Step<IScreenService.Commands.HideAll>().Execute();

            Assert.That(_runtime.AddedToPassive, Is.EqualTo(new IScreenBody[] {first, second}));
            Assert.That(first.ScreenHiddenCalls + second.ScreenHiddenCalls, Is.EqualTo(2));
        }

        [Test]
        public void HideByTag_parks_the_tag_s_screens_and_asks_for_the_tag_it_was_bound_with()
        {
            TestScreen screen = NewScreen();
            _runtime.ActiveScreens = new List<IScreenBody> {screen};

            Step<IScreenService.Commands.HideByTag>().Execute(ScreenTag.GroupC);

            Assert.That(_runtime.TagAsked, Is.EqualTo(ScreenTag.GroupC));
            Assert.That(_runtime.AddedToPassive, Is.EqualTo(new[] {screen}));
        }

        [Test]
        public void UnloadAll_hides_every_loaded_screen_on_stage_and_lets_it_go()
        {
            TestScreen screen = NewScreen();
            _registry.Entries.Add(new ScreenEntry {ViewType = typeof(TestScreen), Loaded = screen});

            Step<IScreenService.Commands.UnloadAll>().Execute();

            Assert.That(screen.ScreenHiddenCalls, Is.EqualTo(1));
            Assert.IsFalse(screen.Data.HasState(ScreenState.Unloading), "the unload finished after the hide");
        }

        [Test]
        public void UnloadByTag_asks_the_registry_for_the_tag_it_was_bound_with()
        {
            TestScreen screen = NewScreen();
            _registry.Entries.Add(new ScreenEntry {ViewType = typeof(TestScreen), Loaded = screen});

            Step<IScreenService.Commands.UnloadByTag>().Execute(ScreenTag.GroupD);

            Assert.That(_registry.TagAsked, Is.EqualTo(ScreenTag.GroupD));
            Assert.That(screen.ScreenHiddenCalls, Is.EqualTo(1));
        }
    }
}
