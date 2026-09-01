using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Adapters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.Tests
{
    public class RootAdapterTests
    {
        private GameObject _host;
        private RootAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("RootAdapterTestHost");
            _adapter = _host.AddComponent<RootAdapter>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_host);

        /// <summary>
        /// The map is serialized and private, so a module that wants to walk every asset the
        /// adapter carries - rather than ask for one by name - had no way in at all.
        /// </summary>
        [Test]
        public void Scriptables_exposes_every_entry_in_the_map()
        {
            ScriptableObject first = ScriptableObject.CreateInstance<ScriptableObject>();
            ScriptableObject second = ScriptableObject.CreateInstance<ScriptableObject>();
            SetMap(new SerializedDictionary<string, ScriptableObject>
            {
                { "PD_First", first },
                { "PD_Second", second }
            });

            IReadOnlyDictionary<string, ScriptableObject> map = _adapter.Scriptables;

            Assert.AreEqual(2, map.Count);
            Assert.AreSame(first, map["PD_First"]);
            Assert.AreSame(second, map["PD_Second"]);

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void Scriptables_is_empty_when_nothing_was_assigned()
        {
            SetMap(new SerializedDictionary<string, ScriptableObject>());

            Assert.AreEqual(0, _adapter.Scriptables.Count);
        }

        private void SetMap(SerializedDictionary<string, ScriptableObject> map)
        {
            FieldInfo field = typeof(RootAdapter)
                .GetField("_scriptableMap", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(_adapter, map);
        }
    }
}
