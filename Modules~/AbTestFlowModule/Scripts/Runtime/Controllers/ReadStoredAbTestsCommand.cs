using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.Models;
using Modules.AbTestFlowModule.Shared.Constants;
using Modules.AbTestFlowModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.AbTestFlowModule.Controllers
{
    /// <summary>
    /// Files the assignments that still stand: what PlayerPrefs holds for an experiment, when it
    /// was written under the experiment's current version. Anything else - nothing stored, an
    /// older version, a group the config no longer has - is left for the roll that follows.
    /// </summary>
    public class ReadStoredAbTestsCommand : Command
    {
        [Inject] private IAbTestFlowModel _model { get; set; }

        public override void Execute()
        {
            FlowLogger.Log(FlowLogType.AbTestFlowModule, "Execute - ReadStoredAbTestsCommand");

            _model.ClearStatuses();

            foreach (AbTestCVO test in _model.ActiveTests)
            {
                if (!TryReadStored(test, out string group))
                    continue;

                AbTestStatusRVO status = Status(test, group);
                _model.SetStatus(status);

                FlowLogger.Log(FlowLogType.AbTestFlowModule,
                    $"Execute - ReadStoredAbTestsCommand - '{test.Id}' v{test.Version}: the player "
                    + (status.IsInTest ? $"is in group '{status.Group}'." : "is outside the test."));
            }
        }

        /// <summary>
        /// A stored group the config no longer has - one renamed without the version being raised -
        /// is treated as not stored, so it is rolled again: kept, it would answer a name no override
        /// matches.
        /// </summary>
        private bool TryReadStored(AbTestCVO test, out string group)
        {
            group = null;

            string stored = PlayerPrefs.GetString(AbTestConstants.PrefsPrefix + test.Id, string.Empty);
            if (string.IsNullOrEmpty(stored))
                return false;

            string[] parts = stored.Split('|');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int storedVersion))
                return false;

            if (storedVersion != test.Version)
                return false;

            if (parts[1] != AbTestConstants.OutOfTestMarker && !HasGroup(test, parts[1]))
            {
                FlowLogger.LogWarning(FlowLogType.AbTestFlowModule,
                    $"TryReadStored - ReadStoredAbTestsCommand - '{test.Id}' v{test.Version} stored group "
                    + $"'{parts[1]}', which the config no longer has. It will be rolled again.");
                return false;
            }

            group = parts[1];
            return true;
        }

        private bool HasGroup(AbTestCVO test, string groupName)
        {
            foreach (AbTestGroupCVO group in test.Groups)
            {
                if (group.Name == groupName)
                    return true;
            }

            return false;
        }

        private AbTestStatusRVO Status(AbTestCVO test, string group)
        {
            bool isInTest = group != AbTestConstants.OutOfTestMarker;

            return new AbTestStatusRVO
            {
                AbTestId = test.Id,
                Version = test.Version,
                Group = isInTest ? group : string.Empty,
                IsInTest = isInTest
            };
        }
    }
}