using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The registry behind ISharedDataModel, driven directly: what a name answers, what a second
    /// filing of it reports, and what a Root takes back when it goes.
    /// </summary>
    public class SharedDataModelTests
    {
        /// <summary>A Root as the registry sees it: a name and an identity. Nothing else is asked of it.</summary>
        private class FakeRoot : IRoot
        {
            public FakeRoot(string name) => Name = name;

            public string Name { get; }
            public IReadOnlyDictionary<string, ScriptableObject> SharedScriptables => null;

            public void StartContext(bool forceToStart = false)
            {
            }

            public void InitializeSubContexts()
            {
            }

            public IContext GetContext() => null;
            public Stack<IContext> GetSubContexts() => new();
            public Stack<IContext> GetAllContexts() => new();

            public void Setup(bool forceToSetup = false)
            {
            }

            public void Launch(bool forceToLaunch = false)
            {
            }
        }

        private class SharedProbe : ScriptableObject
        {
        }

        private SharedDataModel _model;
        private SharedProbe _probe;
        private FakeRoot _first;
        private FakeRoot _second;

        [SetUp]
        public void SetUp()
        {
            _model = new SharedDataModel();
            _probe = ScriptableObject.CreateInstance<SharedProbe>();
            _probe.name = "RD_Probe";
            _first = new FakeRoot("MatchRoot");
            _second = new FakeRoot("HudRoot");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_probe);
        }

        [Test]
        public void A_filed_asset_answers_by_name()
        {
            _model.Register(_first, Filing("RD_Probe", _probe));

            Assert.AreSame(_probe, _model.GetScriptable<SharedProbe>("RD_Probe"));
        }

        [Test]
        public void A_filed_asset_answers_by_type_name_when_no_name_is_given()
        {
            _model.Register(_first, Filing(nameof(SharedProbe), _probe));

            Assert.AreSame(_probe, _model.GetScriptable<SharedProbe>());
        }

        [Test]
        public void An_asset_nobody_filed_logs_an_error_and_answers_null()
        {
            LogAssert.Expect(LogType.Error, new Regex("not filed on any Root[\\s\\S]*RD_Missing"));

            Assert.IsNull(_model.GetScriptable<SharedProbe>("RD_Missing"));
        }

        [Test]
        public void An_asset_filed_as_another_type_logs_an_error_and_answers_null()
        {
            var other = ScriptableObject.CreateInstance<ScriptableObject>();
            _model.Register(_first, Filing("RD_Probe", other));

            LogAssert.Expect(LogType.Error, new Regex("another type[\\s\\S]*RD_Probe[\\s\\S]*MatchRoot"));

            Assert.IsNull(_model.GetScriptable<SharedProbe>("RD_Probe"));

            Object.DestroyImmediate(other);
        }

        /// <summary>
        /// A null value in the slot is a name with no asset behind it. It is not filed, so the
        /// reader gets the not-filed error, which names the right fix.
        /// </summary>
        [Test]
        public void An_empty_entry_is_not_filed()
        {
            _model.Register(_first, Filing("RD_Probe", null));

            LogAssert.Expect(LogType.Error, new Regex("not filed on any Root[\\s\\S]*RD_Probe"));

            Assert.IsNull(_model.GetScriptable<SharedProbe>("RD_Probe"));
        }

        [Test]
        public void A_null_map_files_nothing_and_throws_nothing()
        {
            Assert.DoesNotThrow(() => _model.Register(_first, null));
        }

        /// <summary>
        /// The same asset dragged onto a second Root is the mistake the slot exists to end. It
        /// is a warning naming both Roots - the answer is unchanged, and the warning is where to
        /// look - and the first filing keeps answering.
        /// </summary>
        [Test]
        public void A_second_filing_of_the_same_asset_logs_a_warning_and_the_first_answers()
        {
            _model.Register(_first, Filing("RD_Probe", _probe));

            ConsoleLog warning = Capture(LogType.Warning, () => _model.Register(_second, Filing("RD_Probe", _probe)));

            Assert.IsNotNull(warning, "no warning was logged for the second filing");
            StringAssert.Contains("filed twice", warning.Message);
            StringAssert.Contains("MatchRoot", warning.Message);
            StringAssert.Contains("HudRoot", warning.Message);
            Assert.AreSame(_probe, _model.GetScriptable<SharedProbe>("RD_Probe"));
        }

        /// <summary>
        /// Two different assets under one name is ambiguity, not redundancy: which one a reader
        /// gets would depend on Awake order. An error, and the first filing answers.
        /// </summary>
        [Test]
        public void A_second_filing_of_a_different_asset_logs_an_error_and_the_first_answers()
        {
            var other = ScriptableObject.CreateInstance<SharedProbe>();
            other.name = "RD_Probe_Test";
            _model.Register(_first, Filing("RD_Probe", _probe));

            LogAssert.Expect(LogType.Error,
                new Regex("under one name[\\s\\S]*RD_Probe[\\s\\S]*MatchRoot[\\s\\S]*HudRoot[\\s\\S]*RD_Probe_Test"));
            _model.Register(_second, Filing("RD_Probe", other));

            Assert.AreSame(_probe, _model.GetScriptable<SharedProbe>("RD_Probe"));

            Object.DestroyImmediate(other);
        }

        /// <summary>
        /// A filing survives the first Root going away. The scene that filed it unloads, and the
        /// other Root that also filed it - reported, but still there - is what answers now.
        /// </summary>
        [Test]
        public void When_the_first_filer_goes_the_next_filing_answers()
        {
            var other = ScriptableObject.CreateInstance<SharedProbe>();
            _model.Register(_first, Filing("RD_Probe", _probe));
            LogAssert.Expect(LogType.Error, new Regex("under one name"));
            _model.Register(_second, Filing("RD_Probe", other));

            _model.UnRegister(_first);

            Assert.AreSame(other, _model.GetScriptable<SharedProbe>("RD_Probe"));

            Object.DestroyImmediate(other);
        }

        [Test]
        public void When_every_filer_has_gone_the_asset_is_not_filed()
        {
            _model.Register(_first, Filing("RD_Probe", _probe));

            _model.UnRegister(_first);

            LogAssert.Expect(LogType.Error, new Regex("not filed on any Root[\\s\\S]*RD_Probe"));
            Assert.IsNull(_model.GetScriptable<SharedProbe>("RD_Probe"));
        }

        [Test]
        public void UnRegister_of_a_Root_that_filed_nothing_throws_nothing()
        {
            Assert.DoesNotThrow(() => _model.UnRegister(_second));
        }

        private Dictionary<string, ScriptableObject> Filing(string name, ScriptableObject asset) =>
            new() {{name, asset}};

        /// <summary>
        /// A framework warning is gated on the console settings before it reaches Unity's log,
        /// so it is caught at the Flow Console's own intake instead - the one place it always goes.
        /// </summary>
        private ConsoleLog Capture(LogType logType, Action act)
        {
            ConsoleLog captured = null;

            void OnAdded(ConsoleLog log)
            {
                if (captured == null && log.LogType == logType)
                    captured = log;
            }

            FlowLogger.OnLogAdded += OnAdded;
            try
            {
                act();
            }
            finally
            {
                FlowLogger.OnLogAdded -= OnAdded;
            }

            return captured;
        }
    }
}
