using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Enums;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Data.UnityObjects
{
    /// <summary>
    /// How the panel is reached and how much it keeps. Only this module reads it, which is why it
    /// stays in the Runtime assembly rather than in Shared; it is filed in the module's own
    /// Scriptables slot on DeviceDebuggerServiceRoot's adapter, and the Model reads it there.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_DeviceDebugger", menuName = "FlowIoC/DeviceDebuggerModule/Data/CD_DeviceDebugger")]
    public class CD_DeviceDebugger : ScriptableObject
    {
        [Tooltip("Button: a small translucent pill in the corner. TripleTap: an invisible zone, three taps within a second. None: only IDeviceDebuggerService.Show and the error badge open the panel.")]
        [SerializeField] private DebugTrigger _trigger = DebugTrigger.Button;

        [Tooltip("Where the trigger and the error badge sit.")]
        [SerializeField] private DebugCorner _corner = DebugCorner.BottomRight;

        [Tooltip("How many log rows the Console keeps; the oldest go first.")]
        [SerializeField] private int _logCapacity = DeviceDebuggerConstants.DEFAULT_LOG_CAPACITY;

        [Tooltip("While the panel is closed, an error or an exception turns the corner red with the unread count; tapping it opens the Console.")]
        [SerializeField] private bool _showErrorBadge = true;

        [Tooltip("Draw the frame rate on the trigger while the panel is closed.")]
        [SerializeField] private bool _showFpsOnTrigger;

        [Tooltip("Room left at the bottom of the panel, in dp, on top of what the device reports as unsafe: the rounded corners and the gesture bar are not in the safe area, and a scrollbar drawn into them cannot be touched.")]
        [SerializeField] private int _bottomInset = DeviceDebuggerConstants.DEFAULT_BOTTOM_INSET;

        public DebugTrigger Trigger => _trigger;

        public DebugCorner Corner => _corner;

        public int LogCapacity => _logCapacity > 0 ? _logCapacity : DeviceDebuggerConstants.DEFAULT_LOG_CAPACITY;

        public bool ShowErrorBadge => _showErrorBadge;

        public bool ShowFpsOnTrigger => _showFpsOnTrigger;

        public int BottomInset => _bottomInset < 0 ? 0 : _bottomInset;
    }
}
