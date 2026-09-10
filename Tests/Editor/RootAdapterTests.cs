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

        private void SetMap(SerializedDictionary<string, ScriptableObject> map)
        {
            FieldInfo field = typeof(RootAdapter)
                .GetField("_scriptableMap", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(_adapter, map);
        }

        private class RootAdapterProbe : ScriptableObject
        {
        }
    }
}