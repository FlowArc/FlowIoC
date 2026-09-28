using Modules.DeviceDebuggerModule.Enums;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Data.UnityObjects
{
    /// <summary>
    /// How the panel is reached, how much it keeps and how often it repaints. Only this module
    /// reads it, which is why it stays in the Runtime assembly rather than in Shared; it is filed in
    /// the module's own Scriptables slot on DeviceDebuggerServiceRoot's adapter, and the Model reads
    /// it there.
    ///
    /// Every default is written once, as the field's own value: a new asset and the Model's asset-less
    /// config both start from it. A value nobody can mean - a ring of no rows, a window of no time -
    /// is read as the nearest one that works rather than failing on a device.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_DeviceDebugger", menuName = "FlowIoC/DeviceDebuggerModule/Data/CD_DeviceDebugger")]
    public class CD_DeviceDebugger : ScriptableObject
    {
        private const float LEAST_SECONDS = 0.05f;

        [Header("Trigger")]
        [Tooltip(
            "Button: a small translucent pill in the corner. TripleTap: an invisible zone, three taps within the window below. None: only IDeviceDebuggerService.Show and the error badge open the panel.")]
        [SerializeField]
        private DebugTrigger _trigger = DebugTrigger.Button;

        [Tooltip("Where the trigger and the error badge sit.")] [SerializeField]
        private DebugCorner _corner = DebugCorner.BottomRight;

        [Tooltip("What the Button pill reads while it is not showing the frame rate.")] [SerializeField]
        private string _triggerLabel = "FlowIoC";

        [Tooltip("Gap between the trigger and the edges of the safe area, in dp.")] [SerializeField, Min(0f)]
        private float _triggerMargin = 8f;

        [Tooltip("How long three taps may take on a TripleTap zone, in seconds.")] [SerializeField, Min(LEAST_SECONDS)]
        private float _tripleTapWindowSeconds = 1f;

        [Tooltip("While the panel is closed, an error or an exception turns the corner red with the unread count; tapping it opens the Console.")]
        [SerializeField]
        private bool _showErrorBadge = true;

        [Tooltip("Draw the frame rate on the trigger while the panel is closed.")] [SerializeField]
        private bool _showFpsOnTrigger;

        [Tooltip("How often the frame rate on the trigger is redrawn, in seconds.")] [SerializeField, Min(LEAST_SECONDS)]
        private float _triggerFpsRefreshSeconds = 1f;

        [Header("Panel")] [Tooltip("How many log rows the Console keeps; the oldest go first.")] [SerializeField, Min(1)]
        private int _logCapacity = 10000;

        [Tooltip(
            "Room left at the bottom of the panel, in dp, on top of what the device reports as unsafe: the rounded corners and the gesture bar are not in the safe area, and a scrollbar drawn into them cannot be touched.")]
        [SerializeField, Min(0)]
        private int _bottomInset = 24;

        [Header("Stats")]
        [Tooltip("How many frame times the Stats graph keeps; 120 is two seconds at sixty frames a second.")]
        [SerializeField, Min(1)]
        private int _statsWindow = 120;

        [Tooltip("How often the Stats tab repaints while it is open, in seconds.")] [SerializeField, Min(LEAST_SECONDS)]
        private float _statsRefreshSeconds = 0.25f;

        [Tooltip("The frame time at the top of the Stats graph, in milliseconds; a longer frame is drawn at full height.")] [SerializeField, Min(1f)]
        private float _graphCeilingMs = 50f;

        public DebugTrigger Trigger => _trigger;

        public DebugCorner Corner => _corner;

        public string TriggerLabel => _triggerLabel ?? string.Empty;

        public float TriggerMargin => Mathf.Max(0f, _triggerMargin);

        public float TripleTapWindowSeconds => Mathf.Max(LEAST_SECONDS, _tripleTapWindowSeconds);

        public bool ShowErrorBadge => _showErrorBadge;

        public bool ShowFpsOnTrigger => _showFpsOnTrigger;

        public float TriggerFpsRefreshSeconds => Mathf.Max(LEAST_SECONDS, _triggerFpsRefreshSeconds);

        public int LogCapacity => Mathf.Max(1, _logCapacity);

        public int BottomInset => Mathf.Max(0, _bottomInset);

        public int StatsWindow => Mathf.Max(1, _statsWindow);

        public float StatsRefreshSeconds => Mathf.Max(LEAST_SECONDS, _statsRefreshSeconds);

        public float GraphCeilingMs => Mathf.Max(1f, _graphCeilingMs);
    }
}