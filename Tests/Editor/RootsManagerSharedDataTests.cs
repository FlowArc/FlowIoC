using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.SharedData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The wiring, through real Roots: the manager binds the model for everyone, a Root's shared
    /// slot is filed when the Root registers at Awake, and taken back when its context goes.
    /// Edit mode runs no Unity messages, so the probe Root says when Awake happens.
    /// </summary>
    public class RootsManagerSharedDataTests
    {
        internal class SharedContext : Context
        {
        }

        private class SharedRoot : Root<SharedContext>
        {
            /// <summary>Edit mode sends no Unity messages, so Awake - the Root's own - is called by name.</summary>
            public void Build() =>
                typeof(Root<SharedContext>)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(this, null);

            /// <summary>
            /// Edit mode runs no OnDestroy either. Safe to ask for twice - the host being
            /// destroyed asks for it as well - because unregistering and unbinding what is not
            /// there both do nothing.
            /// </summary>
            public void TearDownNow() => DestroyContext();
        }

        private RootsManager _manager;
        private ScriptableObject _probe;
        private readonly List<GameObject> _hosts = new();

        [SetUp]
        public void SetUp()
        {
            _manager = (RootsManager) RootsManagerFactory.GetRootsManager();
            _probe = ScriptableObject.CreateInstance<ScriptableObject>();
            _probe.name = "RD_Probe";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject host in _hosts)
            {
                if (host == null)
                    continue;

                foreach (SharedRoot root in host.GetComponents<SharedRoot>())
                    root.TearDownNow();

                Object.DestroyImmediate(host);
            }

            _hosts.Clear();
            Object.DestroyImmediate(_probe);

            foreach (UpdateProvider provider in Object.FindObjectsByType<UpdateProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);

            foreach (CoroutineProvider provider in Object.FindObjectsByType<CoroutineProvider>(FindObjectsSortMode.None))
                Object.DestroyImmediate(provider.gameObject);
        }

        [Test]
        public void The_manager_binds_ISharedDataModel_for_every_context()
        {
            Assert.IsNotNull(_manager.SharedDataModel);
            Assert.AreSame(_manager.SharedDataModel, _manager.InjectionBinderCrossContext.GetInstance<ISharedDataModel>());
        }

        [Test]
        public void A_registering_Root_files_what_its_adapter_shares()
        {
            BuildRoot("MatchRoot", _probe);

            Assert.AreSame(_probe, _manager.SharedDataModel.GetScriptable<ScriptableObject>("RD_Probe"));
        }

        [Test]
        public void A_Root_without_an_adapter_registers_without_filing()
        {
            Assert.DoesNotThrow(() => BuildRoot("BareRoot", null));
        }

        [Test]
        public void A_Root_going_away_takes_its_filing_with_it()
        {
            SharedRoot root = BuildRoot("MatchRoot", _probe);

            root.TearDownNow();

            LogAssert.Expect(LogType.Error, new Regex("not filed on any Root[\\s\\S]*RD_Probe"));
            Assert.IsNull(_manager.SharedDataModel.GetScriptable<ScriptableObject>("RD_Probe"));
        }

        /// <summary>
        /// The mono slot goes the same way as the scriptable one. The adapter itself is the
        /// component filed, which spares the test a probe type of its own.
        /// </summary>
        [Test]
        public void A_registering_Root_files_the_components_its_adapter_shares()
        {
            GameObject host = new GameObject("MatchRoot");
            _hosts.Add(host);

            RootAdapter adapter = host.AddComponent<RootAdapter>();
            typeof(RootAdapter)
                .GetField("_sharedMonoMap", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(adapter, new SerializedDictionary<string, MonoBehaviour> {{"Adapter", adapter}});

            host.AddComponent<SharedRoot>().Build();

            Assert.AreSame(adapter, _manager.SharedDataModel.GetMonoBehaviour<RootAdapter>("Adapter"));
        }

        /// <summary>A Root in a scene, built: Awake has run, so it is registered and its slot is filed.</summary>
        private SharedRoot BuildRoot(string name, ScriptableObject shared)
        {
            GameObject host = new GameObject(name);
            _hosts.Add(host);

            if (shared != null)
            {
                RootAdapter adapter = host.AddComponent<RootAdapter>();
                typeof(RootAdapter)
                    .GetField("_sharedScriptableMap", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(adapter, new SerializedDictionary<string, ScriptableObject> {{shared.name, shared}});
            }

            SharedRoot root = host.AddComponent<SharedRoot>();
            root.Build();
            return root;
        }
    }
}