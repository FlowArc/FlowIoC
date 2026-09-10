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
    /// Processes the tests nothing stood for: two dice, in or out of the rollout and then which
    /// group, with an equal split across the groups whatever their number. Each answer is written
    /// to PlayerPrefs at once and the file is saved once at the end. Raising an experiment's version
    /// is the only way to land here again, and it rolls for everybody - including the players the
    /// rollout left outside, who are otherwise outside for good.
    /// </summary>
    public class ProcessAbTestCommand : Command
    {
        [Inject] private IAbTestFlowModel _model { get; set; }

        public override void Execute()
        {
            FlowLogger.Log(FlowLogType.AbTestFlowModule, "Execute - ProcessAbTestCommand");

            var processed = false;

            foreach (AbTestCVO test in _model.ActiveTests)
            {
                if (_model.GetStatus(test.Id) != null)
                    continue;

                string group = Roll(test);

                PlayerPrefs.SetString(AbTestConstants.PrefsPrefix + test.Id, $"{test.Version}|{group}");
                processed = true;

                AbTestStatusRVO status = Status(test, group);
                _model.SetStatus(status);

                FlowLogger.Log(FlowLogType.AbTestFlowModule,
                    $"Execute - ProcessAbTestCommand - '{test.Id}' v{test.Version} processed: the player "
                    + (status.IsInTest ? $"is in group '{status.Group}'." : "is outside the test."));
            }

            if (processed)
                PlayerPrefs.Save();
        }

        private string Roll(AbTestCVO test)
        {
            if (test.Groups.Count == 0 || Die() * 100f >= test.RolloutPercent)
                return AbTestConstants.OutOfTestMarker;

            var index = (int) (Die() * test.Groups.Count);
            if (index >= test.Groups.Count)
                index = test.Groups.Count - 1;

            return test.Groups[index].Name;
        }

        /// <summary>Random.value is inclusive of 1, and the two dice want [0, 1).</summary>
        private float Die()
        {
            float value = Random.value;

            return value < 1f ? value : 0f;
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