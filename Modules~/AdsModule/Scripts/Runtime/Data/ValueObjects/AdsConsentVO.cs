namespace Modules.AdsModule.Data.ValueObjects
{
    /// <summary>
    /// The three flags every ad SDK asks for - GDPR consent, CCPA do-not-sell, COPPA age
    /// restriction. The module hands them on; who resolves them is a consent module's job.
    /// </summary>
    public readonly struct AdsConsentVO
    {
        public readonly bool HasUserConsent;
        public readonly bool DoNotSell;
        public readonly bool IsAgeRestricted;

        public AdsConsentVO(bool hasUserConsent, bool doNotSell, bool isAgeRestricted)
        {
            HasUserConsent = hasUserConsent;
            DoNotSell = doNotSell;
            IsAgeRestricted = isAgeRestricted;
        }

        /// <summary>Consented: consent given and selling allowed. Not consented: the reverse. Never age-restricted.</summary>
        public AdsConsentVO(bool consented) : this(consented, !consented, false)
        {
        }

        public override string ToString() =>
            "has_user_consent=" + Word(HasUserConsent)
            + " do_not_sell=" + Word(DoNotSell)
            + " age_restricted=" + Word(IsAgeRestricted);

        private string Word(bool value) => value ? "yes" : "no";
    }
}
