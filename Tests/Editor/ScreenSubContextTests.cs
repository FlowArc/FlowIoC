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
    /// A screen context is the screen's whole declaration: it binds the mediation itself and hands
    /// the service its ScreenCVO in Setup, then takes it back when the context is destroyed.
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
        public void Destroying_the_context_unregisters_the_screen()
        {
            Type unregistered = null;
            _signals.UnRegisterScreen.AddListener(viewType => unregistered = viewType);

            _context.DestroyContext();

            Assert.AreEqual(typeof(ProbeScreenView), unregistered);
        }
    }
}