using Modules.AbTestFlowModule.Enums;

namespace Modules.AbTestFlowModule.Data.ValueObjects
{
    /// <summary>
    /// One thing wrong, or worth knowing, about a config, and where in the test's matrix it is -
    /// the scope, and for a group or a cell the group's column and the asset's row - so the editor
    /// can say it beside the part it is about rather than in a list over the test.
    /// </summary>
    public class AbTestValidationVO
    {
        public AbTestValidationSeverity Severity;
        public string Message;
        public AbTestValidationScope Scope;
        public int Group = -1;
        public int Row = -1;
    }
}
