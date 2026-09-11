using System.Reflection;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Adapters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    public class RootAdapterTests
    {
        private GameObject _host;
        private RootAdapter _adapter;
        private RootAdapterProbe _probe;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("RootAdapterTestHost");
            _adapter = _host.AddComponent<RootAdapter>();
            _probe = ScriptableObject.CreateInstance<RootAdapterProbe>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_probe);
        }

        [Test]
        public void GetScriptable_answers_the_asset_filed_under_the_name()
        {
            SetMap(new SerializedDictionary<string, ScriptableObject> {{"CD_Probe", _probe}});

            Assert.AreSame(_probe, _adapter.GetScriptable<RootAdapterProbe>("CD_Probe"));
        }

        /// <summary>
        /// The parameterless overload files by type name, which is what a module's model asks for
        /// when the asset is named after its type - the usual case.
        /// </summary>
        [Test]
        public void GetScriptable_without_a_name_files_under_the_type_name()
        {
            SetMap(new SerializedDictionary<string, ScriptableObject> {{nameof(RootAdapterProbe), _probe}});

            Assert.AreSame(_probe, _adapter.GetScriptable<RootAdapterProbe>());
        }

        /// <summary>
        /// A miss used to be a KeyNotFoundException from inside the framework. It is an authoring
        /// mistake - the asset was never dragged onto the Root - so it is reported as one, naming
        /// the asset and the Root, and the caller gets null rather than a throw.
        /// </summary>
        [Test]
        public void GetScriptable_of_an_asset_that_is_not_filed_logs_an_error_and_answers_null()
        {
            SetMap(new SerializedDictionary<string, ScriptableObject>());

            LogAssert.Expect(LogType.Error, new Regex("CD_Missing[\\s\\S]*RootAdapterTestHost"));

            Assert.IsNull(_adapter.GetScriptable<RootAdapterProbe>("CD_Missing"));
        }

        [Test]
        public void GetScriptable_of_an_asset_filed_as_another_type_logs_an_error_and_answers_null()
        {
            var other = ScriptableObject.CreateInstance<ScriptableObject>();
            SetMap(new SerializedDictionary<string, ScriptableObject> {{"CD_Probe", other}});

            LogAssert.Expect(LogType.Error, new Regex("CD_Probe"));

            Assert.IsNull(_adapter.GetScriptable<RootAdapterProbe>("CD_Probe"));

            Object.DestroyImmediate(other);
        }

        [Test]
        public void GetScriptable_on_an_adapter_that_was_never_filled_logs_an_error_and_answers_null()
        {
            LogAssert.Expect(LogType.Error, new Regex("CD_Probe"));

            Assert.IsNull(_adapter.GetScriptable<RootAdapterProbe>("CD_Probe"));
        }

        [Test]
        public void GetScriptable_finds_an_asset_filed_as_shared()
        {
            SetMap(new SerializedDictionary<string, ScriptableObject>());
            SetSharedMap(new SerializedDictionary<string, ScriptableObject> {{"RD_Probe", _probe}});

            Assert.AreSame(_probe, _adapter.GetScriptable<RootAdapterProbe>("RD_Probe"));
        }

        /// <summary>
        /// The private slot is the module's own and answers first. Nothing is reported: the same
        /// name in both slots of one adapter is a slip with one adapter to look at.
        /// </summary>
        [Test]
        public void GetScriptable_prefers_the_private_slot_when_both_slots_hold_the_name()
        {
            var shared = ScriptableObject.CreateInstance<RootAdapterProbe>();
            SetMap(new SerializedDictionary<string, ScriptableObject> {{"RD_Probe", _probe}});
            SetSharedMap(new SerializedDictionary<string, ScriptableObject> {{"RD_Probe", shared}});

            Assert.AreSame(_probe, _adapter.GetScriptable<RootAdapterProbe>("RD_Probe"));

            Object.DestroyImmediate(shared);
        }

        [Test]
        public void SharedScriptables_is_the_shared_slot()
        {
            var map = new SerializedDictionary<string, ScriptableObject> {{"RD_Probe", _probe}};
            SetSharedMap(map);

            Assert.AreSame(map, _adapter.SharedScriptables);
        }

        [Test]
        public void SharedScriptables_is_null_until_the_slot_is_filled()
        {
            Assert.IsNull(_adapter.SharedScriptables);
        }

        [Test]
        public void GetMonoBehaviour_answers_the_component_filed_under_the_name()
        {
            RootAdapterMonoProbe probe = _host.AddComponent<RootAdapterMonoProbe>();
            SetMonoMap("_monoMap", new SerializedDictionary<string, MonoBehaviour> {{"Probe", probe}});

            Assert.AreSame(probe, _adapter.GetMonoBehaviour<RootAdapterMonoProbe>("Probe"));
        }

        [Test]
        public void GetMonoBehaviour_without_a_name_files_under_the_type_name()
        {
            RootAdapterMonoProbe probe = _host.AddComponent<RootAdapterMonoProbe>();
            SetMonoMap("_monoMap", new SerializedDictionary<string, MonoBehaviour> {{nameof(RootAdapterMonoProbe), probe}});

            Assert.AreSame(probe, _adapter.GetMonoBehaviour<RootAdapterMonoProbe>());
        }

        [Test]
        public void GetMonoBehaviour_finds_a_component_filed_as_shared()
        {
            RootAdapterMonoProbe probe = _host.AddComponent<RootAdapterMonoProbe>();
            SetMonoMap("_sharedMonoMap", new SerializedDictionary<string, MonoBehaviour> {{"Probe", probe}});

            Assert.AreSame(probe, _adapter.GetMonoBehaviour<RootAdapterMonoProbe>("Probe"));
        }

        /// <summary>
        /// A miss used to be a KeyNotFoundException thrown from inside the framework. It is the
        /// same authoring mistake as a missing asset, so it is reported the same way.
        /// </summary>
        [Test]
        public void GetMonoBehaviour_of_a_component_that_is_not_filed_logs_an_error_and_answers_null()
        {
            LogAssert.Expect(LogType.Error, new Regex("Probe[\\s\\S]*RootAdapterTestHost"));

            Assert.IsNull(_adapter.GetMonoBehaviour<RootAdapterMonoProbe>("Probe"));
        }

        [Test]
        public void SharedMonoBehaviours_is_the_shared_mono_slot()
        {
            RootAdapterMonoProbe probe = _host.AddComponent<RootAdapterMonoProbe>();
            var map = new SerializedDictionary<string, MonoBehaviour> {{"Probe", probe}};
            SetMonoMap("_sharedMonoMap", map);

            Assert.AreSame(map, _adapter.SharedMonoBehaviours);
        }

        private void SetMap(SerializedDictionary<string, ScriptableObject> map)
        {
            FieldInfo field = typeof(RootAdapter)
                .GetField("_scriptableMap", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(_adapter, map);
        }

        private void SetSharedMap(SerializedDictionary<string, ScriptableObject> map)
        {
            FieldInfo field = typeof(RootAdapter)
                .GetField("_sharedScriptableMap", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(_adapter, map);
        }

        private void SetMonoMap(string fieldName, SerializedDictionary<string, MonoBehaviour> map)
        {
            FieldInfo field = typeof(RootAdapter)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(_adapter, map);
        }

        private class RootAdapterProbe : ScriptableObject
        {
        }

        private class RootAdapterMonoProbe : MonoBehaviour
        {
        }
    }
}