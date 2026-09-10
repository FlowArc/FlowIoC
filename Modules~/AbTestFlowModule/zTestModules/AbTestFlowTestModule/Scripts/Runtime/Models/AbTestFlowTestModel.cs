#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Data.UnityObjects;
using Modules.AbTestFlowModule.AbTestFlowTestModule.RootsContexts;
using Modules.AbTestFlowModule.Data.UnityObjects;
using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.Models;
using UnityEngine;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Models
{
    /// <summary>
    /// Holds the two assets the scene reads and writes: the probe the experiment overrides, and the
    /// module's own CD_AbTests, whose version the Raise button bumps. Both are filed on this Root's
    /// adapter, under their type names.
    /// </summary>
    public class AbTestFlowTestModel : IAbTestFlowTestModel, IConstructable
    {
        private const string EXPERIMENT_ID = "Probe";

        [Inject(nameof(AbTestFlowTestContext))] private GameObject _root { get; set; }

        private readonly EditorAssetSnapshots _snapshots = new();

        private CD_AbTests _config;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public string AbTestId => EXPERIMENT_ID;

        public int Version => Experiment()?.Version ?? 0;

        public CD_AbTestProbe Probe { get; private set; }

        /// <summary>
        /// Runs after the service Root's own PostConstruct - its order is lower - so by now the
        /// probe already reads whatever the assigned group wrote over it.
        /// </summary>
        public void PostConstruct()
        {
            var adapter = _root.GetComponent<RootAdapter>();

            Probe = adapter.GetScriptable<CD_AbTestProbe>();
            _config = adapter.GetScriptable<CD_AbTests>();

            _snapshots.Capture(new List<ScriptableObject> {_config});
        }

        public void RaiseVersion()
        {
            AbTestCVO experiment = Experiment();

            if (experiment != null)
                experiment.Version++;
        }

        public void RestoreEditorAssets() => _snapshots.Restore();

        private AbTestCVO Experiment()
        {
            if (_config == null)
                return null;

            foreach (AbTestCVO test in _config.Tests)
            {
                if (test.Id == EXPERIMENT_ID)
                    return test;
            }

            return null;
        }
    }
}

#endif
