using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AbTestFlowModule.Data.UnityObjects;
using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.RootsContexts;
using Modules.AbTestFlowModule.Shared.Data.UnityObjects;
using Modules.AbTestFlowModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.AbTestFlowModule.Models
{
    /// <summary>
    /// Holds the config and the assignments. PostConstruct only takes the two assets off the Root's
    /// adapter and readies them; it decides nothing. The deciding is a Command's, started by the
    /// service, and the result comes back through SetAssignment.
    /// </summary>
    public class AbTestFlowModel : IAbTestFlowModel, IConstructable
    {
        [Inject(nameof(AbTestFlowServiceContext))]
        private GameObject _root { get; set; }

        private readonly List<AbTestCVO> _activeTests = new();
        private readonly List<AbTestStatusRVO> _statuses = new();

        private RD_AbTestStatus _published;

#if UNITY_EDITOR
        private readonly EditorAssetSnapshots _snapshots = new();
#endif

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public IReadOnlyList<AbTestCVO> ActiveTests => _activeTests;

        public IReadOnlyList<AbTestStatusRVO> Statuses => _statuses;

        public void PostConstruct()
        {
            _activeTests.Clear();
            _statuses.Clear();

            if (!TryReadAdapter(out CD_AbTests config, out _published))
                return;

            _activeTests.AddRange(config.ActiveTests());

#if UNITY_EDITOR
            // Before anything is written: every original of every group, because which group this
            // player lands in is not known yet, and the published asset, which is filled at boot.
            _snapshots.Capture(AssetsAboutToChange(_activeTests, _published));
#endif

            _published.Clear();
        }

        public void Deconstruct()
        {
            _activeTests.Clear();
            _statuses.Clear();
        }

        public void SetStatus(AbTestStatusRVO status)
        {
            if (status == null || string.IsNullOrEmpty(status.AbTestId))
                return;

            for (var i = 0; i < _statuses.Count; i++)
            {
                if (_statuses[i].AbTestId != status.AbTestId)
                    continue;

                _statuses[i] = status;
                Publish(status);
                return;
            }

            _statuses.Add(status);
            Publish(status);
        }

        public void ClearStatuses()
        {
            _statuses.Clear();

            if (_published != null)
                _published.Clear();
        }

        public AbTestStatusRVO GetStatus(string abTestId)
        {
            foreach (AbTestStatusRVO status in _statuses)
            {
                if (status.AbTestId == abTestId)
                    return status;
            }

            return null;
        }

        public string GetGroup(string abTestId)
        {
            AbTestStatusRVO status = GetStatus(abTestId);

            return status != null && status.IsInTest ? status.Group : null;
        }

#if UNITY_EDITOR
        public void RestoreEditorAssets() => _snapshots.Restore();
#endif

        private void Publish(AbTestStatusRVO status)
        {
            if (_published != null)
                _published.Set(status);
        }

        /// <summary>
        /// The adapter on the Root is where the two assets are filed, under their type names. One
        /// that is missing is reported by the adapter itself, naming the asset and the Root, so
        /// this only has to notice the null.
        /// </summary>
        private bool TryReadAdapter(out CD_AbTests config, out RD_AbTestStatus published)
        {
            config = null;
            published = null;

            var adapter = _root.GetComponent<RootAdapter>();
            if (adapter == null)
            {
                FlowLogger.LogError(FlowLogType.AbTestFlowModule,
                    "AbTestFlowServiceRoot has no RootAdapter, so no experiment can run.", _root);
                return false;
            }

            config = adapter.GetScriptable<CD_AbTests>();
            published = adapter.GetScriptable<RD_AbTestStatus>();

            return config != null && published != null;
        }

#if UNITY_EDITOR
        private List<ScriptableObject> AssetsAboutToChange(List<AbTestCVO> tests, RD_AbTestStatus published)
        {
            var assets = new List<ScriptableObject> {published};

            foreach (AbTestCVO test in tests)
            {
                foreach (AbTestGroupCVO group in test.Groups)
                {
                    foreach (AbTestOverrideCVO pair in group.Overrides)
                    {
                        if (pair.Original != null)
                            assets.Add(pair.Original);
                    }
                }
            }

            return assets;
        }
#endif
    }
}