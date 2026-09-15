using System.Collections.Generic;
using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.Shared.Data.ValueObjects;

namespace Modules.AbTestFlowModule.Models
{
    /// <summary>
    /// The module's data: the one experiment the designer switched on, and what was decided about
    /// this player for it. Deciding is not on this surface - a Command does that and files the
    /// result here.
    /// </summary>
    public interface IAbTestFlowModel
    {
        /// <summary>The test CD_AbTests names as active, or null when it names none.</summary>
        AbTestCVO ActiveTest { get; }

        /// <summary>What this boot evaluated: the active test's status, or nothing.</summary>
        IReadOnlyList<AbTestStatusRVO> Statuses { get; }

        /// <summary>
        /// Files what was decided about one experiment, replacing whatever that experiment had, and
        /// publishes it to RD_AbTestStatus.
        /// </summary>
        void SetStatus(AbTestStatusRVO status);

        /// <summary>Forgets every assignment, here and in the published asset.</summary>
        void ClearStatuses();

        /// <summary>What was decided about one experiment, or null when nothing was yet.</summary>
        AbTestStatusRVO GetStatus(string abTestId);

        /// <summary>
        /// The group this player landed in, or null when they are outside the test, the experiment
        /// is not the active one, or nothing of that id is defined.
        /// </summary>
        string GetGroup(string abTestId);

#if UNITY_EDITOR
        /// <summary>
        /// Puts the config assets back to what they were before a variant was written over them, so
        /// a play session leaves no diff behind. Called on quit.
        /// </summary>
        void RestoreEditorAssets();
#endif
    }
}