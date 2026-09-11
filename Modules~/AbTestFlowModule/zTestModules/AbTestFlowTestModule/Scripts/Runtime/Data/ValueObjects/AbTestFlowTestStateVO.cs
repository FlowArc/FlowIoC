#if UNITY_EDITOR

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Data.ValueObjects
{
    /// <summary>What the scene shows: the experiment as it stands, and the probe as it reads now.</summary>
    public class AbTestFlowTestStateVO
    {
        public string AbTestId;
        public int Version;

        /// <summary>Null when the player is outside the test.</summary>
        public string Group;

        /// <summary>
        /// The same group, read the other way a module can: off RD_AbTestStatus through
        /// ISharedDataModel, the asset the service Root files as shared. Null when the player is
        /// outside the test - or when nothing filed the asset, which the console then reports.
        /// </summary>
        public string SharedGroup;

        public int Lives;
        public float Speed;
    }
}

#endif