namespace Modules.HapticModule.Models
{
    /// <summary>
    /// The module's one piece of state: whether haptics are on. Reading and writing PlayerPrefs is
    /// the commands' job, the way the A/B module keeps its assignments.
    /// </summary>
    public interface IHapticModel
    {
        bool IsEnabled { get; }

        void SetEnabled(bool on);
    }
}
