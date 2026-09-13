namespace Modules.HapticModule.Models
{
    public class HapticModel : IHapticModel
    {
        public bool IsEnabled { get; private set; } = true;

        public void SetEnabled(bool on) => IsEnabled = on;
    }
}
