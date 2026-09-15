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
    /// Writes the assigned group's assets over the control group's, when this player is inside
    /// the active experiment. The control group's list is the game's own configuration, so a player
    /// who landed there changes nothing; a player in any other group gets that group's asset
    /// written over the control's at the same index, row by row.
    ///
    /// The copy is JsonUtility rather than the Newtonsoft stack the save module uses, and
    /// deliberately. That one serialises for a file, so it drops every field that points at
    /// another Object - a pointer means nothing to the session that reads the file back. Here the
    /// JSON lives for the length of one call, so a variant that swaps a prefab, a sprite or a clip
    /// carries those references across as instance ids. It also deep-copies nested lists, so the
    /// original never ends up sharing the variant asset's own collections.
    /// </summary>
    internal class ApplyOverridesCommand : Command
    {
        [Inject] private IAbTestFlowModel _model { get; set; }

        public override void Execute()
        {
            FlowLogger.Log("Execute - ApplyOverridesCommand");

            AbTestCVO test = _model.ActiveTest;
            if (test == null || test.Groups.Count == 0)
                return;

            AbTestStatusRVO status = _model.GetStatus(test.Id);
            if (status == null || !status.IsInTest)
                return;

            AbTestGroupCVO control = test.Groups[0];
            AbTestGroupCVO group = FindGroup(test, status.Group);

            if (group == null || group == control)
                return;

            for (var row = 0; row < control.Assets.Count; row++)
            {
                ScriptableObject original = control.Assets[row];
                ScriptableObject variant = row < group.Assets.Count ? group.Assets[row] : null;

                if (Apply(original, variant))
                    continue;

                FlowLogger.LogError($"Execute - ApplyOverridesCommand - '{test.Id}' group '{group.Name}' could not "
                                    + $"write its variant over asset {row + 1} of the control group. The two are a "
                                    + "different type, or a slot is empty.", original);
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

        private static AbTestGroupCVO FindGroup(AbTestCVO test, string groupName)
        {
            foreach (AbTestGroupCVO group in test.Groups)
            {
                if (group.Name == groupName)
                    return group;
            }

            return null;
        }
    }
}
