namespace Modules.LoadingModule.Enums
{
    /// <summary>What a Command told the service about a step - the six methods of ILoadingStep.</summary>
    public enum LoadingReportKind
    {
        Start = 0,
        Progress = 1,
        Detail = 2,
        Complete = 3,
        Skip = 4,
        Fail = 5
    }
}
