using System;

namespace Modules.AbTestFlowModule.Shared.Data.ValueObjects
{
    /// <summary>
    /// Where this player stands in one A/B test. Produced during play and never persisted as an
    /// asset - the durable copy is the PlayerPrefs entry the module writes.
    /// </summary>
    [Serializable]
    public class AbTestStatusRVO
    {
        public string AbTestId;
        public int    Version;

        /// <summary>Empty when the rollout left the player outside the test.</summary>
        public string Group;

        public bool IsInTest;
    }
}
