namespace Modules.AnalyticsModule.Data.ValueObjects
{
    /// <summary>What an SDK accepts, handed to the trimmer by the provider that knows its SDK. Firebase and Facebook are both 40 / 40 / 100 / 25.</summary>
    public readonly struct AnalyticsLimitsVO
    {
        public readonly int NameLength;
        public readonly int ParameterNameLength;
        public readonly int StringValueLength;
        public readonly int ParameterCount;

        public AnalyticsLimitsVO(int nameLength, int parameterNameLength, int stringValueLength, int parameterCount)
        {
            NameLength = nameLength;
            ParameterNameLength = parameterNameLength;
            StringValueLength = stringValueLength;
            ParameterCount = parameterCount;
        }
    }
}
