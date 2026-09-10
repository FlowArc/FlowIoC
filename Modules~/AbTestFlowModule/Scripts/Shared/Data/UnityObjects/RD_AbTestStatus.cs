using System.Collections.Generic;
using Modules.AbTestFlowModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.AbTestFlowModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The module's status: where this player stands in every A/B test this session decided,
    /// readable by whoever needs it whenever they are ready. Nothing is announced - the decision is
    /// made at boot, before any other module is listening - so an analytics module reads this once
    /// its own SDK is up.
    /// </summary>
    [CreateAssetMenu(fileName = "RD_AbTestStatus", menuName = "FlowIoC/AbTestFlowModule/Data/RD_AbTestStatus")]
    public class RD_AbTestStatus : ScriptableObject
    {
        [SerializeField] private List<AbTestStatusRVO> _tests = new();

        public IReadOnlyList<AbTestStatusRVO> Tests => _tests;

        /// <summary>Files where the player stands in one test, replacing what that test had before.</summary>
        public void Set(AbTestStatusRVO status)
        {
            if (status == null || string.IsNullOrEmpty(status.AbTestId))
                return;

            for (var i = 0; i < _tests.Count; i++)
            {
                if (_tests[i].AbTestId != status.AbTestId)
                    continue;

                _tests[i] = status;
                return;
            }

            _tests.Add(status);
        }

        public AbTestStatusRVO Get(string abTestId)
        {
            foreach (AbTestStatusRVO status in _tests)
            {
                if (status.AbTestId == abTestId)
                    return status;
            }

            return null;
        }

        public void Clear() => _tests.Clear();
    }
}