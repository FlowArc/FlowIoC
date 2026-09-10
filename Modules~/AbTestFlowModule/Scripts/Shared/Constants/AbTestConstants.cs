namespace Modules.AbTestFlowModule.Shared.Constants
{
    /// <summary>
    /// What another module needs to know about this one without referencing its Runtime assembly.
    /// The SRDebugger module that will let a tester pick a group writes the same keys.
    /// </summary>
    public class AbTestConstants
    {
        /// <summary>A stored assignment reads "&lt;version&gt;|&lt;group&gt;".</summary>
        public const string PrefsPrefix = "flowioc.abtest.";

        /// <summary>The group of a player the rollout left outside the test.</summary>
        public const string OutOfTestMarker = "-";

        /// <summary>
        /// Low enough that every override lands before a game module reads its config, and above
        /// LocalSave at -100, which this module does not depend on.
        /// </summary>
        public const int InitializeOrder = -90;
    }
}
