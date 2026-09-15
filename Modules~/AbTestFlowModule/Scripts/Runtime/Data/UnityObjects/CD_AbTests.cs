using System.Collections.Generic;
using Modules.AbTestFlowModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.AbTestFlowModule.Data.UnityObjects
{
    /// <summary>
    /// Every experiment the game has ever defined, and which one of them runs: one test is active
    /// at a time, named by its id, and an empty name is no test at all. Only this module reads it,
    /// which is why it stays in the Runtime assembly rather than in Shared.
    ///
    /// The asset says nothing about its own shape: the AB Test Editor shows the validator's word
    /// under the test it is about while the author edits, and a slot that cannot be applied is an
    /// error at boot. Logging from OnValidate would repeat the same line on every tick of a slider.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_AbTests", menuName = "FlowIoC/AbTestFlowModule/Data/CD_AbTests")]
    public class CD_AbTests : ScriptableObject
    {
        [SerializeField] private string _activeTestId;
        [SerializeField] private List<AbTestCVO> _tests = new();

        public string ActiveTestId
        {
            get => _activeTestId;
            set => _activeTestId = value;
        }

        public List<AbTestCVO> Tests => _tests;

        /// <summary>The test the active id names, or null when it names none.</summary>
        public AbTestCVO ActiveTest()
        {
            if (string.IsNullOrEmpty(_activeTestId))
                return null;

            foreach (AbTestCVO test in _tests)
            {
                if (test.Id == _activeTestId)
                    return test;
            }

            return null;
        }
    }
}
