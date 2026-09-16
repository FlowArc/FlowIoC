namespace Modules.AnalyticsModule.Enums
{
    /// <summary>What an <see cref="Data.ValueObjects.AnalyticsParameterVO"/> carries. Each SDK maps the four its own way.</summary>
    public enum AnalyticsParameterKind
    {
        String = 0,
        Long = 1,
        Double = 2,
        Bool = 3
    }
}
