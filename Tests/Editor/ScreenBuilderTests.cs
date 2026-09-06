using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// One Open, one builder. The sub service is a single object for the whole run, and it used to
    /// keep the screen being opened in its own fields - so two commands opening a screen in the
    /// same frame wrote over each other and the second Show opened the first one's screen.
    ///
    /// Each test stops the open at a check rather than letting it reach the show service, which is
    /// where a scene would be needed. What the checks were asked about is what says which screen
    /// the builder was carrying.
    /// </summary>
    public class ScreenBuilderTests
    {
        private const int FirstManager = 1;
        private const int SecondManager = 2;

        private RecordingRuntimeModel _runtimeModel;
        private IScreenBuilderSubService _builderSubService;

        [SetUp]
        public void SetUp()
        {
            _runtimeModel = new RecordingRuntimeModel();

            ScreenRegistryModel registry = new ScreenRegistryModel();
            registry.RegisterScreen(EntryFor<FirstScreen>(FirstManager, layer: 0));
            registry.RegisterScreen(EntryFor<SecondScreen>(SecondManager, layer: 0));

            StandInContext context = new StandInContext();
            context.InjectionBinder.BindInstance<IScreenRegistryModel>(registry);
            context.InjectionBinder.BindInstance<IScreenRuntimeModel>(_runtimeModel);
            context.InjectionBinder.BindInstance(new ShowSubService());
            context.InjectionBinder.BindInstance(new HideSubService());

            ScreenBuilderSubService builderSubService = new ScreenBuilderSubService();
            context.TryToInjectObject(builderSubService);

            _builderSubService = builderSubService;
        }

        [Test]
        public void Open_hands_back_a_different_builder_every_time()
        {
            IScreenBuilder first = _builderSubService.Open<FirstScreen>(FirstManager);
            IScreenBuilder second = _builderSubService.Open<SecondScreen>(SecondManager);

            Assert.That(first, Is.Not.SameAs(second));
        }

        /// <summary>
        /// The regression this whole shape exists for: two opens in flight at once, and what one of
        /// them is told does not reach the other.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task What_one_open_is_told_does_not_reach_another()
        {
            IScreenBuilder first = _builderSubService.Open<FirstScreen>(FirstManager);
            IScreenBuilder second = _builderSubService.Open<SecondScreen>(SecondManager);

            first.OpenInLayer(5);

            _runtimeModel.LayerIsFull = true;
            LogAssert.Expect(LogType.Error, new Regex("Layer 0 is not empty"));

            await second.Show();

            Assert.That(_runtimeModel.LastLayerAsked, Is.Zero, "the second open kept its own layer");
            Assert.That(_runtimeModel.LastManagerAsked, Is.EqualTo(SecondManager), "and its own manager");
            Assert.That(_runtimeModel.LastTypeAsked, Is.EqualTo(typeof(SecondScreen)), "and its own screen");
        }

        [Test]
        public async System.Threading.Tasks.Task An_open_carries_the_layer_it_was_given()
        {
            IScreenBuilder builder = _builderSubService.Open<FirstScreen>(FirstManager).OpenInLayer(5);

            _runtimeModel.LayerIsFull = true;
            LogAssert.Expect(LogType.Error, new Regex("Layer 5 is not empty"));

            await builder.Show();

            Assert.That(_runtimeModel.LastLayerAsked, Is.EqualTo(5));
        }

        [Test]
        public async System.Threading.Tasks.Task A_screen_registered_nowhere_is_reported_and_shows_nothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("not registered at manager"));

            IScreenBuilder builder = _builderSubService.Open<UnregisteredScreen>();

            IScreenBody shown = await builder.Show();

            Assert.That(shown, Is.Null);
            Assert.That(_runtimeModel.LastTypeAsked, Is.Null, "it never got as far as a check");
        }

        [Test]
        public async System.Threading.Tasks.Task A_builder_shows_once()
        {
            IScreenBuilder builder = _builderSubService.Open<FirstScreen>(FirstManager);

            _runtimeModel.LayerIsFull = true;
            LogAssert.Expect(LogType.Error, new Regex("Layer 0 is not empty"));

            await builder.Show();

            LogAssert.Expect(LogType.Error, new Regex("has already been shown"));

            IScreenBody second = await builder.Show();

            Assert.That(second, Is.Null);
        }

        private static ScreenEntry EntryFor<T>(int managerId, int layer) where T : IScreenBody
        {
            return new ScreenEntry
            {
                ViewType = typeof(T),
                Screen = new ScreenCVO
                {
                    ManagerId = managerId,
                    Layer = layer,
                    Load = ScreenLoadCVO.Resource(typeof(T).Name)
                }
            };
        }

        #region Stand-ins

        private class FirstScreen : ScreenBody
        {
        }

        private class SecondScreen : ScreenBody
        {
        }

        private class UnregisteredScreen : ScreenBody
        {
        }

        /// <summary>
        /// Answers the two checks an open runs and writes down what it was asked, which is how a
        /// test sees which screen the builder was carrying without the show service being involved.
        /// </summary>
        private class RecordingRuntimeModel : IScreenRuntimeModel
        {
            public bool LayerIsFull;
            public int LastLayerAsked = -1;
            public int LastManagerAsked = -1;
            public Type LastTypeAsked;

            public bool IsScreenActive(Type screenType, int managerId, out IScreenBody screenBody)
            {
                LastTypeAsked = screenType;
                LastManagerAsked = managerId;
                screenBody = null;
                return false;
            }

            public bool IsLayerFull(int layerIndex, int managerId, out IScreenBody screenBody)
            {
                LastLayerAsked = layerIndex;
                LastManagerAsked = managerId;
                screenBody = null;
                return LayerIsFull;
            }

            public bool GetScreen<T>(int managerId, out T screen) where T : IScreenBody
            {
                screen = default;
                return false;
            }

            public void AddToPassivePool(IScreenBody screenBody) { }
            public void AddToActivePools(IScreenBody screenBody) { }
            public void RemoveFromActivePools(IScreenBody screenBody) { }
            public void RemoveFromPassivePool(IScreenBody screenBody) { }
            public List<IScreenBody> GetAllActiveScreens() => new();

            public bool GetActiveManagerScreens(int managerId, out List<IScreenBody> list)
            {
                list = null;
                return false;
            }

            public bool GetActiveTagScreens(ScreenTag tag, int managerId, out List<IScreenBody> list)
            {
                list = null;
                return false;
            }
        }

        #endregion
    }
}
