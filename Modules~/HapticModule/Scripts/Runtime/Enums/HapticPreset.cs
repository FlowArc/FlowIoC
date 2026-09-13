namespace Modules.HapticModule.Enums
{
    /// <summary>
    /// The nine presets iOS names, and the same nine on Android. The numbers are HitNPoP's and
    /// Nice Vibrations' and are what FlowHaptics.mm switches on, so they never move. None lets a
    /// data-driven caller say "no haptic" without a branch.
    /// </summary>
    public enum HapticPreset
    {
        None = -1,
        Selection = 0,
        Success = 1,
        Warning = 2,
        Failure = 3,
        LightImpact = 4,
        MediumImpact = 5,
        HeavyImpact = 6,
        RigidImpact = 7,
        SoftImpact = 8
    }
}
