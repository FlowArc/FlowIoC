using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.Enums;
using Modules.AbTestFlowModule.Services;
using UnityEngine;

namespace Modules.AbTestFlowModule.Data.UnityObjects
{
    /// <summary>
    /// Every experiment the game has ever defined. Only this module reads it, which is why it stays
    /// in the Runtime assembly rather than in Shared.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_AbTests", menuName = "FlowIoC/AbTestFlowModule/Data/CD_AbTests")]
    public class CD_AbTests : ScriptableObject
    {
        [SerializeField] private List<AbTestCVO> _tests = new();

        public List<AbTestCVO> Tests => _tests;

        public List<AbTestCVO> ActiveTests()
        {
            var active = new List<AbTestCVO>();

            foreach (AbTestCVO test in _tests)
            {
                if (test.IsActive)
                    active.Add(test);
            }

            return active;
        }

#if UNITY_EDITOR
        /// <summary>
        /// A designer hears about a broken experiment while editing it. A build has no OnValidate,
        /// which is why the model checks the types again at boot.
        /// </summary>
        private void OnValidate()
        {
            foreach (AbTestValidationVO message in new AbTestConfigValidator().Validate(_tests))
            {
                switch (message.Severity)
                {
                    case AbTestValidationSeverity.Error:
                        FlowLogger.LogError(FlowLogType.AbTestFlowModule, message.Message, this);
                        break;
                    case AbTestValidationSeverity.Warning:
                        FlowLogger.LogWarning(FlowLogType.AbTestFlowModule, message.Message);
                        break;
                    case AbTestValidationSeverity.Information:
                        FlowLogger.Log(FlowLogType.AbTestFlowModule, message.Message);
                        break;
                }
            }
        }
#endif
    }
}