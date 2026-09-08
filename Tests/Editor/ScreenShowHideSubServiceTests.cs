using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The two sub services that move a screen between the pools, driven against a runtime model
    /// that only records what it was asked. What is under test is the bookkeeping - which pool a
    /// screen ends up in, and how many times - because that is what a real model would be left
    /// holding, and a screen parked twice is handed to two opens at once.
    ///
    /// The registry answers no manager, so SetupSubService's RectTransform work is skipped. Where
    /// a screen is parented is the scene's business and is not what these two decide.
    /// </summary>
    public class ScreenShowHideSubServiceTests
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

        /// <summary>Records what it was asked and answers whatever the test set up beforehand.</summary>
        private class FakeScreenRuntimeModel : IScreenRuntimeModel
        {
            public readonly List<IScreenBody> AddedToActive = new();
            public readonly List<IScreenBody> RemovedFromActive = new();
            public readonly List<IScreenBody> AddedToPassive = new();

            public List<IScreenBody> ActiveScreens = new();
            public IScreenBody LayerOccupant;
            public IScreenBody ActiveByType;

            public void AddToPassivePool(IScreenBody screenBody) => AddedToPassive.Add(screenBody);
            public void AddToActivePools(IScreenBody screenBody) => AddedToActive.Add(screenBody);
            public void RemoveFromActivePools(IScreenBody screenBody) => RemovedFromActive.Add(screenBody);

            public void RemoveFromPassivePool(IScreenBody screenBody)
            {
            }

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
                screenBody = LayerOccupant;
                return LayerOccupant != null;
            }

            public bool IsScreenActive(Type screenType, int managerId, out IScreenBody screenBody)
            {
                screenBody = ActiveByType;
                return ActiveByType != null;
            }

            public List<IScreenBody> GetAllActiveScreens() => ActiveScreens;

            public bool GetActiveManagerScreens(int managerId, out List<IScreenBody> list)
            {
                list = ActiveScreens;
                return ActiveScreens.Count > 0;
            }

            public bool GetActiveTagScreens(ScreenTag tag, int managerId, out List<IScreenBody> list)
            {
                list = ActiveScreens;
                return ActiveScreens.Count > 0;
            }
        }

        /// <summary>Answers no manager, which is what makes the setup step a no-op here.</summary>
        private class FakeScreenRegistryModel : IScreenRegistryModel
        {
            public void RegisterScreenManager(ScreenManagerVO manager)
            {
            }

            public ScreenManagerVO GetScreenManager(int managerId) => null;
            public bool RegisterScreen(ScreenEntry entry) => false;

            public bool TryGetEntry(int managerId, Type viewType, out ScreenEntry entry)
            {
                entry = null;
                return false;
            }

            public ScreenEntry GetEntry(int managerId, Type viewType) => null;

            public void RemoveEntry(ScreenEntry entry)
            {
            }

            public List<ScreenEntry> GetAllEntries() => new();
            public List<ScreenEntry> GetManagerEntries(int managerId) => new();
            public List<ScreenEntry> GetTagEntries(ScreenTag tag) => new();
            public List<IScreenBody> GetAllLoadedScreens() => new();
            public List<IScreenBody> GetAllScreensAtManager(int managerId) => new();

            public void CopyDataFromConfig(ScreenVO screenData)
            {
            }

            public void CopyDataFromConfig(ScreenVO screenData, ScreenCVO screen)
            {
            }
        }

        private StandInContext _context;
        private FakeScreenRuntimeModel _runtime;
        private ShowSubService _show;
        private HideSubService _hide;
        private UnloadSubService _unload;
        private readonly List<GameObject> _hosts = new();

        [SetUp]
        public void SetUp()
        {
            _context = new StandInContext();
            _runtime = new FakeScreenRuntimeModel();

            _show = new ShowSubService();
            _hide = new HideSubService();
            SetupSubService setup = new SetupSubService();
            _unload = new UnloadSubService();
            DisposeSubService dispose = new DisposeSubService();

            _context.InjectionBinder.BindInstance<IScreenRuntimeModel>(_runtime);
            _context.InjectionBinder.BindInstance<IScreenRegistryModel>(new FakeScreenRegistryModel());
            _context.InjectionBinder.BindInstance<SetupSubService>(setup);
            _context.InjectionBinder.BindInstance<LoadSubService>(new LoadSubService());
            _context.InjectionBinder.BindInstance<UnloadSubService>(_unload);
            _context.InjectionBinder.BindInstance<DisposeSubService>(dispose);
            _context.InjectionBinder.BindInstance<AddressableLoadSubService>(new AddressableLoadSubService());
            _context.InjectionBinder.BindInstance<ResourceLoadSubService>(new ResourceLoadSubService());
            _context.InjectionBinder.BindInstance<HideSubService>(_hide);
            _context.InjectionBinder.BindInstance<ShowSubService>(_show);

            _context.TryToInjectObject(_show);
            _context.TryToInjectObject(_hide);
            _context.TryToInjectObject(setup);
            _context.TryToInjectObject(_unload);
            _context.TryToInjectObject(dispose);
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

        /// <summary>
        /// Registered by default: registering a view finds the context above it in the hierarchy
        /// and builds its mediator, which is a scene's business and not what these two services
        /// decide. One test below turns it off deliberately to say that Register is still reached.
        /// </summary>
        private TestScreen NewScreen(bool inUse = false, int layer = 0, int managerId = 0, bool registered = true)
        {
            GameObject host = new GameObject("TestScreen");
            _hosts.Add(host);

            TestScreen screen = host.AddComponent<TestScreen>();
            screen.Data.ScreenType = typeof(TestScreen);
            screen.Data.LayerIndex = layer;
            screen.Data.ManagerId = managerId;
            screen.IsRegistered = registered;

            if (inUse)
                screen.Data.AddState(ScreenState.InUse);

            return screen;
        }

        #region Show

        [Test]
        public void Showing_a_pooled_screen_puts_it_in_the_active_pool_and_shows_it()
        {
            TestScreen screen = NewScreen();

            TestScreen shown = _show.ShowPooledScreen<TestScreen>(screen);

            Assert.AreSame(screen, shown);
            Assert.That(_runtime.AddedToActive, Is.EqualTo(new[] {screen}));
        }

        /// <summary>
        /// A screen registers on its first show and never again: registering builds the mediator,
        /// and a pooled screen coming back is the same instance with the same one. There is no
        /// context above these screens, so what says Register was reached is the report it makes
        /// about that - and the second show, which is already registered, makes none.
        /// </summary>
        [Test]
        public void A_screen_registers_the_first_time_it_is_shown_and_not_after()
        {
            TestScreen screen = NewScreen(registered: false);

            LogAssert.Expect(LogType.Error, new Regex("There is no Context"));
            _show.ShowPooledScreen<TestScreen>(screen);

            screen.IsRegistered = true;
            _show.ShowPooledScreen<TestScreen>(screen);

            Assert.That(_runtime.AddedToActive, Has.Count.EqualTo(2), "both shows were counted");
        }

        [Test]
        public void Showing_a_screen_that_is_null_is_reported_rather_than_shown()
        {
            LogAssert.Expect(LogType.Error, "[ScreenService.Show.ShowPooledScreen] Screen body is null");

            Assert.IsNull(_show.ShowPooledScreen<TestScreen>(null));
            Assert.That(_runtime.AddedToActive, Is.Empty);
        }

        #endregion

        #region Hide

        [Test]
        public void Hiding_a_screen_in_use_parks_it_in_the_passive_pool()
        {
            TestScreen screen = NewScreen(inUse: true);
            _hide.Setup(screen);

            _hide.Screen(screen);

            Assert.That(_runtime.RemovedFromActive, Is.EqualTo(new[] {screen}));
            Assert.That(_runtime.AddedToPassive, Is.EqualTo(new[] {screen}));
            Assert.That(screen.ScreenHiddenCalls, Is.EqualTo(1));
        }

        /// <summary>
        /// A screen that is not in use has nothing to hide - it is already parked, or it never
        /// opened - and hiding it again would park the same instance twice.
        /// </summary>
        [Test]
        public void Hiding_a_screen_that_is_not_in_use_changes_nothing()
        {
            TestScreen screen = NewScreen();
            _hide.Setup(screen);

            _hide.Screen(screen);

            Assert.That(_runtime.AddedToPassive, Is.Empty);
            Assert.That(screen.ScreenHiddenCalls, Is.Zero);
        }

        [Test]
        public void Hiding_a_screen_that_is_null_is_reported()
        {
            LogAssert.Expect(LogType.Error, "[ScreenService.Hide.Screen] Screenbody is null or already destroyed");

            _hide.Screen((IScreenBody) null);

            Assert.That(_runtime.AddedToPassive, Is.Empty);
        }

        /// <summary>
        /// Play mode exit destroys the screen before the Root's OnDestroy drives the unregister
        /// path down to here, and an IScreenBody is an interface - so == null compares the managed
        /// reference and answers that the destroyed instance is still there. It used to reach
        /// AddToPassivePool, which reads the screen's transform, and threw a
        /// MissingReferenceException on every exit from play.
        /// </summary>
        [Test]
        public void Hiding_a_screen_Unity_has_destroyed_is_reported_rather_than_parked()
        {
            TestScreen screen = NewScreen(inUse: true);
            _hide.Setup(screen);
            Object.DestroyImmediate(screen.gameObject);

            LogAssert.Expect(LogType.Error, "[ScreenService.Hide.Screen] Screenbody is null or already destroyed");

            _hide.Screen(screen);

            Assert.That(_runtime.AddedToPassive, Is.Empty);
        }

        /// <summary>
        /// A forced hide skips the screen's own Hide, so it can land while the show animation is
        /// still running. The flag has to come off there rather than in the animation's own
        /// completion, which is never going to arrive: the pooled instance would come back holding
        /// it and its Mediator would refuse every tap.
        /// </summary>
        [Test]
        public void A_forced_hide_takes_the_show_animation_flag_off_the_pooled_instance()
        {
            TestScreen screen = NewScreen(inUse: true);
            screen.Data.HasShowAnimation = true;
            screen.Data.AddState(ScreenState.InShowAnimation);

            _hide.Screen(screen, isForce: true);

            Assert.That(screen.Data.HasState(ScreenState.InShowAnimation), Is.False);
            Assert.That(_runtime.AddedToPassive, Is.EqualTo(new[] {screen}));
        }

        /// <summary>
        /// Setup is called on every show, and a screen force-opened over a duplicate is shown again
        /// before it was hidden. Two subscriptions on one HideCompleted park the same instance in
        /// the pool twice, which is what hands one screen to two opens.
        /// </summary>
        [Test]
        public void Setting_a_screen_up_twice_still_parks_it_once()
        {
            TestScreen screen = NewScreen(inUse: true);

            _hide.Setup(screen);
            _hide.Setup(screen);

            _hide.Screen(screen);

            Assert.That(_runtime.AddedToPassive, Has.Count.EqualTo(1));
        }

        [Test]
        public void Hiding_every_active_screen_parks_each_one()
        {
            TestScreen first = NewScreen(inUse: true);
            TestScreen second = NewScreen(inUse: true);
            _hide.Setup(first);
            _hide.Setup(second);
            _runtime.ActiveScreens = new List<IScreenBody> {first, second};

            _hide.AllScreens();

            Assert.That(_runtime.AddedToPassive, Is.EqualTo(new IScreenBody[] {first, second}));
        }

        [Test]
        public void Hiding_the_screens_at_a_manager_that_has_none_changes_nothing()
        {
            _hide.ScreensAtManager(3);

            Assert.That(_runtime.AddedToPassive, Is.Empty);
        }

        [Test]
        public void Hiding_a_layer_parks_the_screen_that_occupies_it()
        {
            TestScreen screen = NewScreen(inUse: true, layer: 2);
            _hide.Setup(screen);
            _runtime.LayerOccupant = screen;

            _hide.ScreenInLayer(2);

            Assert.That(_runtime.AddedToPassive, Is.EqualTo(new[] {screen}));
        }

        [Test]
        public void Hiding_an_empty_layer_changes_nothing()
        {
            _hide.ScreenInLayer(2);

            Assert.That(_runtime.AddedToPassive, Is.Empty);
        }

        [Test]
        public void Hiding_by_tag_parks_the_screens_the_model_answers_with()
        {
            TestScreen screen = NewScreen(inUse: true);
            _hide.Setup(screen);
            _runtime.ActiveScreens = new List<IScreenBody> {screen};

            _hide.ScreensByTag(ScreenTag.Default);

            Assert.That(_runtime.AddedToPassive, Is.EqualTo(new[] {screen}));
        }

        [Test]
        public void Hiding_a_screen_by_type_that_is_not_active_changes_nothing()
        {
            _hide.Screen<TestScreen>();

            Assert.That(_runtime.AddedToPassive, Is.Empty);
        }

        [Test]
        public void Hiding_a_screen_by_type_parks_the_active_one()
        {
            TestScreen screen = NewScreen(inUse: true);
            _hide.Setup(screen);
            _runtime.ActiveByType = screen;

            _hide.Screen<TestScreen>();

            Assert.That(_runtime.AddedToPassive, Is.EqualTo(new[] {screen}));
        }

        #endregion

        #region Unload

        /// <summary>
        /// A screen still in use when its context goes away is hidden with the animation skipped,
        /// which parks it before the loader releases it.
        /// </summary>
        [Test]
        public void Unregistering_a_screen_that_is_in_use_hides_it_first()
        {
            TestScreen screen = NewScreen(inUse: true);
            _hide.Setup(screen);

            _unload.Unregistered(screen);

            Assert.That(_runtime.AddedToPassive, Is.EqualTo(new[] {screen}));
            Assert.That(screen.ScreenHiddenCalls, Is.EqualTo(1));
        }

        /// <summary>
        /// This is the play mode exit path. The Root's OnDestroy dispatches UnRegisterScreen, and
        /// by then Unity has destroyed the screen - so hiding it would park a dead object, and
        /// parking reads its transform. The bookkeeping still runs; the hide does not.
        /// </summary>
        [Test]
        public void Unregistering_a_screen_Unity_has_destroyed_skips_the_hide()
        {
            TestScreen screen = NewScreen(inUse: true);
            _hide.Setup(screen);
            Object.DestroyImmediate(screen.gameObject);

            _unload.Unregistered(screen);

            Assert.That(_runtime.AddedToPassive, Is.Empty);
            Assert.That(_runtime.RemovedFromActive, Is.EqualTo(new[] {screen}));
            Assert.That(screen.ScreenHiddenCalls, Is.Zero);
        }

        #endregion
    }
}