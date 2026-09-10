using System.Reflection;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Root;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What a Root hands the RootsManager at registration: the Shared Scriptables of the adapter
    /// on its own GameObject, and nothing when it has no adapter.
    /// </summary>
    public class RootSharedScriptablesTests
    {
        private GameObject _host;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("RootSharedScriptablesTestHost");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void A_Root_without_an_adapter_shares_nothing()
        {
            RootBase root = _host.AddComponent<RootBase>();

            Assert.IsNull(root.SharedScriptables);
        }

        [Test]
        public void A_Root_shares_what_its_adapter_files_as_shared()
        {
            var probe = ScriptableObject.CreateInstance<ScriptableObject>();
            var map = new SerializedDictionary<string, ScriptableObject> {{"RD_Probe", probe}};

            RootAdapter adapter = _host.AddComponent<RootAdapter>();
            typeof(RootAdapter)
                .GetField("_sharedScriptableMap", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(adapter, map);
            RootBase root = _host.AddComponent<RootBase>();

            Assert.AreSame(map, root.SharedScriptables);

            Object.DestroyImmediate(probe);
        }
    }
}
