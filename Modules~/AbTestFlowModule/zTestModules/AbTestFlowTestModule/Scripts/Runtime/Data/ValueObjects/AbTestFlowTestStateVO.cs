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

        public int Lives;
        public float Speed;
    }
}

#endif
