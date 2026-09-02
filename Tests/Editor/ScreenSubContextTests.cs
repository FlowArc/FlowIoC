using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Bind.Bindings.Mediator;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.RootsContexts;
using FlowIoC.ScreenModule.Service;
using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.Signals;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A screen context declares the screen: it binds the mediation itself and hands the service
    /// its ScreenCVO in Setup, then takes it back when the context is destroyed. What it hands
    /// over is the declaration unless the Root listing it overrode the five values a scene may
    /// decide - Load never being one of them.
    /// </summary>
    public class ScreenSubContextTests
    {
        private class ProbeScreenView : ScreenView
        {
        }

        private class ProbeScreenMediator : IMediator
        {
            public void OnRegister()
            {
            }

            public void OnRemove()
            {
            }
        }

        private class ProbeScreenContext : ScreenSubContext<ProbeScreenView, ProbeScreenMediator>
        {
            protected override ScreenCVO Screen => new() {Layer = 3, Load = ScreenLoadCVO.Addressable("Probe")};
        }

        private class FakeScreenService : IScreenService
        {
            public LoadSubService Load { get; set; }
            public CheckSubService Check { get; set; }
            public TryGetSubService TryGet { get; set; }
            public HideSubService Hide { get; set; }
            public UnloadSubService Unload { get; set; }
            public IScreenBuilderSubService Open<T>(int managerId = 0) where T : IScreenBody => null;
        }

        private InjectionBinderCrossContext _crossContext;
        private ScreenServiceInternalSignals _signals;
        private GameObject _serviceHost;
        private Context _serviceContext;
        private GameObject _host;
        private ProbeScreenContext _context;

        [SetUp]
        public void SetUp()
        {
            _crossContext = ((RootsManager) RootsManagerFactory.GetRootsManager()).InjectionBinderCrossContext;

            // The cross-context binder binds through whichever context last claimed it, so a
            // context has to have started before anything can be bound - the way ScreenServiceRoot
            // starts before a screen's Root in a scene.
            _serviceHost = new GameObject("FakeScreenServiceHost");
            _serviceContext = new Context();
            _serviceContext.Initialize(_serviceHost, -99, _crossContext, new List<IContext>());
            _serviceContext.Start();

            _signals = _crossContext.Bind<ScreenServiceInternalSignals>();
            _crossContext.BindInstance<IScreenService>(new FakeScreenService());

            _host = new GameObject("ProbeScreenContextHost");
            _context = new ProbeScreenContext();
            _context.Initialize(_host, 0, _crossContext, new List<IContext>());
            _context.Start();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_host);
            UnityEngine.Object.DestroyImmediate(_serviceHost);

            _crossContext.UnBind<ScreenServiceInternalSignals>();
            _crossContext.UnBind<IScreenService>();
            _crossContext.UnBind<IUpdateProvider>();
            _crossContext.UnBind<ICoroutineProvider>();
            foreach (UpdateProvider provider in UnityEngine.Object.FindObjectsByType<UpdateProvider>(FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(provider.gameObject);

            foreach (CoroutineProvider provider in UnityEngine.Object.FindObjectsByType<CoroutineProvider>(FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(provider.gameObject);
        }

        [Test]
        public void The_view_is_bound_to_the_mediator_by_the_context_itself()
        {
            _context.MediationBindings();

            MediatorBinding binding = _context.MediationBinder.GetBinding(typeof(ProbeScreenView));

            Assert.IsNotNull(binding);
            Assert.AreEqual(typeof(ProbeScreenMediator), binding.Value);
        }

        [Test]
        public void Setup_registers_the_screen_with_its_declaration_and_its_owner()
        {
            ScreenEntry received = null;
            _signals.RegisterScreen.AddListener(entry => received = entry);

            _context.Setup();

            Assert.IsNotNull(received);
            Assert.AreEqual(typeof(ProbeScreenView), received.ViewType);
            Assert.AreSame(_context, received.Owner);
            Assert.AreEqual(3, received.Screen.Layer);
            Assert.AreEqual("Probe", received.Screen.Load.Key);
            Assert.IsNull(received.Loaded);
        }

        [Test]
        public void Destroying_the_context_unregisters_the_screen_at_its_own_manager()
        {
            int managerId = -1;
            Type unregistered = null;
            _signals.UnRegisterScreen.AddListener((id, viewType) =>
            {
                managerId = id;
                unregistered = viewType;
            });

            _context.DestroyContext();

            Assert.AreEqual(0, managerId);
            Assert.AreEqual(typeof(ProbeScreenView), unregistered);
        }

        [Test]
        public void The_resolved_declaration_is_read_once_and_kept()
        {
            ScreenCVO first = _context.Resolved;
            ScreenCVO second = _context.Resolved;

            Assert.AreSame(first, second);
            Assert.AreNotSame(first, _context.Declaration);
            Assert.AreEqual(3, first.Layer);
        }

        [Test]
        public void An_override_replaces_the_declaration_but_never_its_load()
        {
            ((ISubContextOverridable) _context).ApplyOverride(new SubContextData
            {
                OverrideScreen = true,
                ScreenManagerId = 1,
                ScreenLayer = 7,
                ScreenTag = ScreenTag.GroupB,
                ScreenHasShowAnimation = true,
                ScreenHasHideAnimation = true
            });

            ScreenCVO resolved = _context.Resolved;

            Assert.AreEqual(1, resolved.ManagerId);
            Assert.AreEqual(7, resolved.Layer);
            Assert.AreEqual(ScreenTag.GroupB, resolved.Tag);
            Assert.IsTrue(resolved.HasShowAnimation);
            Assert.IsTrue(resolved.HasHideAnimation);
            Assert.AreEqual("Probe", resolved.Load.Key);
        }

        [Test]
        public void An_entry_that_does_not_override_leaves_the_declaration_alone()
        {
            ((ISubContextOverridable) _context).ApplyOverride(new SubContextData
            {
                OverrideScreen = false,
                ScreenLayer = 7
            });

            Assert.AreEqual(3, _context.Resolved.Layer);
        }

        [Test]
        public void Setup_registers_the_overridden_declaration()
        {
            ScreenEntry received = null;
            _signals.RegisterScreen.AddListener(entry => received = entry);
            ((ISubContextOverridable) _context).ApplyOverride(new SubContextData
            {
                OverrideScreen = true,
                ScreenManagerId = 1,
                ScreenLayer = 7
            });

            _context.Setup();

            Assert.AreEqual(1, received.Screen.ManagerId);
            Assert.AreEqual(7, received.Screen.Layer);
            Assert.AreEqual("Probe", received.Screen.Load.Key);
        }

        [Test]
        public void A_second_override_does_not_compound_onto_the_first()
        {
            ISubContextOverridable overridable = _context;
            overridable.ApplyOverride(new SubContextData {OverrideScreen = true, ScreenLayer = 7});
            overridable.ApplyOverride(new SubContextData {OverrideScreen = true, ScreenLayer = 2});

            Assert.AreEqual(2, _context.Resolved.Layer);
            Assert.AreEqual("Probe", _context.Resolved.Load.Key);
        }
    }
}