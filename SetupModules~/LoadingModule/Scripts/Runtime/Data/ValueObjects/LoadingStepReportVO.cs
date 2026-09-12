using Modules.LoadingModule.Enums;

namespace Modules.LoadingModule.Data.ValueObjects
{
    /// <summary>What ILoadingStep hands the internal signal. Time is stamped by the service.</summary>
    public class LoadingStepReportVO
    {
        public string Step;
        public LoadingReportKind Kind;
        public float Fraction;
        public string Detail;
        public string Reason;
        public float Time;
    }
}
