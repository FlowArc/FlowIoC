using Modules.AbTestFlowModule.Enums;

namespace Modules.AbTestFlowModule.Data.ValueObjects
{
    /// <summary>One thing wrong, or worth knowing, about a config.</summary>
    public class AbTestValidationVO
    {
        public AbTestValidationSeverity Severity;
        public string Message;
    }
}
