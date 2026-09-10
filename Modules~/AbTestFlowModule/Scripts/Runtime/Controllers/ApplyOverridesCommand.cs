using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.Models;
using Modules.AbTestFlowModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.AbTestFlowModule.Controllers
{
    /// <summary>
    /// Writes the assigned group's variants over the originals, for every experiment this player
    /// is inside. The control group carries no overrides, so a player who landed there changes
    /// nothing - the original configuration is the control configuration.
    ///
    /// The copy is JsonUtility rather than the Newtonsoft stack the save module uses, and
    /// deliberately. That one serialises for a file, so it drops every field that points at
    /// another Object - a pointer means nothing to the session that reads the file back. Here the
    /// JSON lives for the length of one call, so a variant that swaps a prefab, a sprite or a clip
    /// carries those references across as instance ids. It also deep-copies nested lists, so the
    /// original never ends up sharing the variant asset's own collections.
    /// </summary>
    public class ApplyOverridesCommand : Command
    {
        [Inject] private IAbTestFlowModel _model { get; set; }

        public override void Execute()
        {
            FlowLogger.Log(FlowLogType.AbTestFlowModule, "Execute - ApplyOverridesCommand");

            foreach (AbTestStatusRVO status in _model.Statuses)
            {
                if (!status.IsInTest)
                    continue;

                AbTestGroupCVO group = FindGroup(status.AbTestId, status.Group);
                if (group == null)
                    continue;

                foreach (AbTestOverrideCVO pair in group.Overrides)
                {
                    if (Apply(pair.Original, pair.Variant))
                        continue;

                    FlowLogger.LogError(FlowLogType.AbTestFlowModule,
                        $"Execute - ApplyOverridesCommand - '{status.AbTestId}' group '{group.Name}' "
                        + "could not write its variant over the original. The two are a different type, or "
                        + "a slot is empty.", pair.Original);
                }
            }
        }

        /// <summary>Copies one config asset over another. Answers whether it did.</summary>
        private bool Apply(ScriptableObject original, ScriptableObject variant)
        {
            if (original == null || variant == null)
                return false;

            if (original.GetType() != variant.GetType())
                return false;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(variant), original);
            return true;
        }

        private AbTestGroupCVO FindGroup(string abTestId, string groupName)
        {
            foreach (AbTestCVO test in _model.ActiveTests)
            {
                if (test.Id != abTestId)
                    continue;

                foreach (AbTestGroupCVO group in test.Groups)
                {
                    if (group.Name == groupName)
                        return group;
                }
            }

            return null;
        }
    }
}