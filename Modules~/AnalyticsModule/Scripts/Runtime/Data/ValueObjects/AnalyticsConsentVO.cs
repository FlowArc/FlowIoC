namespace Modules.AnalyticsModule.Data.ValueObjects
{
    /// <summary>
    /// Google Consent Mode's four flags, which Firebase takes as they are and every other SDK
    /// derives from. The module hands them on; who resolves them - a consent form, the ATT
    /// prompt - is a consent module's job.
    /// </summary>
    public readonly struct AnalyticsConsentVO
    {
        public readonly bool AnalyticsStorage;
        public readonly bool AdStorage;
        public readonly bool AdUserData;
        public readonly bool AdPersonalization;

        public AnalyticsConsentVO(bool analyticsStorage, bool adStorage, bool adUserData, bool adPersonalization)
        {
            AnalyticsStorage = analyticsStorage;
            AdStorage = adStorage;
            AdUserData = adUserData;
            AdPersonalization = adPersonalization;
        }

        /// <summary>All four granted, or all four denied.</summary>
        public AnalyticsConsentVO(bool all) : this(all, all, all, all)
        {
        }

        public override string ToString() =>
            "analytics_storage=" + Word(AnalyticsStorage)
            + " ad_storage=" + Word(AdStorage)
            + " ad_user_data=" + Word(AdUserData)
            + " ad_personalization=" + Word(AdPersonalization);

        private string Word(bool granted) => granted ? "granted" : "denied";
    }
}
