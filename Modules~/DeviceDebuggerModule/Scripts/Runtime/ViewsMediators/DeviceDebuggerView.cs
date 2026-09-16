using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Data.UnityObjects;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>
    /// The panel's UIDocument and raw input: the corner trigger, the tab bar, the five pages
    /// through their painters, and the safe area. It raises what the tester did and paints what
    /// it is handed; which tab opens, what a tap means, what a value becomes - none of that is
    /// decided here. It sits on a child of the Root, authored inactive, and comes on at Setup.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    [RequireComponent(typeof(UIDocument))]
    public class DeviceDebuggerView : MonoBehaviour, IView
    {
        private const string CORNER_CLASS_PREFIX = "dd-corner-";
        private const string TAB_ON_CLASS = "dd-tab-on";
        private const string TRIGGER_TAP_CLASS = "dd-trigger-tap";
        private const string BADGE_ON_CLASS = "dd-badge-on";

        [SerializeField] private UIDocument _document;

        public bool IsRegistered { get; set; }

        public event Action OnTriggerTapped;
        public event Action OnCloseTapped;
        public event Action<DebugTab> OnTabSelected;
        public event Action<DebugOptionVO, string> OnOptionActivated;
        public event Action<DebugSignalVO, string> OnSignalFired;
        public event Action OnFilterChanged;
        public event Action OnClearTapped;
        public event Action OnCopyLogsTapped;
        public event Action<ConsoleLog> OnCopyDetailTapped;
        public event Action OnCopyInfoTapped;
        public event Action<ConsoleLog> OnLogSelected;
        public event Action<float> OnTick;

        private VisualElement _root;
        private VisualElement _trigger;
        private Label _triggerLabel;
        private Label _badge;
        private VisualElement _panel;
        private readonly Dictionary<DebugTab, Button> _tabs = new();
        private readonly Dictionary<DebugTab, VisualElement> _pages = new();

        private ConsoleTabPainter _console;
        private OptionsTabPainter _options;
        private SignalsTabPainter _signals;
        private StatsTabPainter _stats;
        private InfoTabPainter _info;
        private PressFeedback _pressFeedback;

        private const float TRIGGER_MARGIN = 8f;

        private DebugTrigger _triggerKind = DebugTrigger.Button;
        private DebugCorner _corner = DebugCorner.BottomRight;
        private float _bottomInset = DeviceDebuggerConstants.DEFAULT_BOTTOM_INSET;
        private bool _badgeEnabled = true;
        private bool _badgeShowing;
        private int _taps;
        private Rect _appliedSafeArea = new(-1f, -1f, 0f, 0f);
        private float _firstTapAt;
        private bool _built;

        /// <summary>What the Console shows; the painter owns it, the mediator reads it.</summary>
        public LogFilterVO Filter => _console?.Filter;

        /// <summary>
        /// The tree is queried on enable, but a UIDocument that has not attached yet answers with no
        /// root; then the first Update builds it, and the mediator paints once this turns true.
        /// </summary>
        public bool IsBuilt => _built;

        private void OnEnable()
        {
            if (_document == null) _document = GetComponent<UIDocument>();

            Build();
        }

        private void OnDisable()
        {
            if (!_built) return;

            _trigger.UnregisterCallback<PointerUpEvent>(TriggerPressed);
            _root.UnregisterCallback<GeometryChangedEvent>(GeometryChanged);
            _pressFeedback.Detach();
            _built = false;
        }

        private void Update()
        {
            if (!_built) Build();

            if (_built && _appliedSafeArea != Screen.safeArea) ApplySafeArea();

            OnTick?.Invoke(Time.unscaledDeltaTime);
        }

        // ======================== Painting ========================

        public void ApplyConfig(CD_DeviceDebugger config)
        {
            if (!_built || config == null) return;

            _triggerKind = config.Trigger;
            _badgeEnabled = config.ShowErrorBadge;
            _corner = config.Corner;
            _bottomInset = config.BottomInset;

            foreach (DebugCorner corner in Enum.GetValues(typeof(DebugCorner)))
                _trigger.EnableInClassList(CORNER_CLASS_PREFIX + corner.ToString().ToLowerInvariant(), corner == config.Corner);

            _appliedSafeArea = new Rect(-1f, -1f, 0f, 0f);
            ApplySafeArea();

            _trigger.EnableInClassList(TRIGGER_TAP_CLASS, config.Trigger == DebugTrigger.TripleTap);
            _triggerLabel.text = DeviceDebuggerConstants.TRIGGER_LABEL;
            _triggerLabel.style.display = config.Trigger == DebugTrigger.Button ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshTriggerVisibility();
        }

        public void SetErrorBadge(int unread)
        {
            if (!_built) return;

            bool on = _badgeEnabled && unread > 0;
            _badgeShowing = on;
            _badge.text = unread > 99 ? "99+" : unread.ToString();
            _badge.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            _trigger.EnableInClassList(BADGE_ON_CLASS, on);
            RefreshTriggerVisibility();
        }

        public void SetTriggerFps(float fps, bool show)
        {
            if (!_built) return;

            if (_triggerKind != DebugTrigger.Button) return;

            _triggerLabel.text = show ? fps.ToString("0") + " fps" : DeviceDebuggerConstants.TRIGGER_LABEL;
        }

        public void ShowPanel(bool open)
        {
            if (!_built) return;

            _panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            _trigger.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
            RefreshTriggerVisibility();
        }

        public void ShowTab(DebugTab tab)
        {
            if (!_built) return;

            foreach (KeyValuePair<DebugTab, VisualElement> page in _pages)
                page.Value.style.display = page.Key == tab ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (KeyValuePair<DebugTab, Button> button in _tabs)
                button.Value.EnableInClassList(TAB_ON_CLASS, button.Key == tab);
        }

        public void PaintLogs(IReadOnlyList<ConsoleLog> rows, bool keepScrolledToEnd) => _console?.PaintRows(rows, keepScrolledToEnd);

        public void PaintCounts(int logs, int warnings, int errors) => _console?.PaintCounts(logs, warnings, errors);

        public void PaintChannels(IReadOnlyList<FlowLogChannel> channels) => _console?.PaintChannels(channels);

        public void PaintDetail(ConsoleLog row) => _console?.PaintDetail(row);

        public void ClearLogSelection() => _console?.ClearSelection();

        public void PaintOptions(IReadOnlyList<DebugOptionVO> options) => _options?.Paint(options);

        public void RefreshOptionValue(DebugOptionVO option) => _options?.RefreshValue(option);

        public void PaintSignals(IReadOnlyList<DebugSignalVO> rows) => _signals?.Paint(rows);

        public void PaintStats(StatsSampleVO sample) => _stats?.Paint(sample);

        public void PaintInfo(IReadOnlyList<InfoRowVO> rows) => _info?.Paint(rows);

        // ======================== The tree ========================

        private void Build()
        {
            if (_built || _document == null) return;

            VisualElement root = _document.rootVisualElement;

            if (root == null) return;

            _root = root.Q<VisualElement>("root") ?? root;
            _trigger = root.Q<VisualElement>("trigger");
            _triggerLabel = root.Q<Label>("trigger-label");
            _badge = root.Q<Label>("error-badge");
            _panel = root.Q<VisualElement>("panel");

            if (_trigger == null || _panel == null)
            {
                FlowLogger.LogError("DeviceDebuggerPanel.uxml has no trigger or panel element, so the panel cannot be built.");
                return;
            }

            _tabs.Clear();
            _pages.Clear();
            Tab(root, DebugTab.Console, "tab-console", "page-console");
            Tab(root, DebugTab.Options, "tab-options", "page-options");
            Tab(root, DebugTab.Signals, "tab-signals", "page-signals");
            Tab(root, DebugTab.Stats, "tab-stats", "page-stats");
            Tab(root, DebugTab.Info, "tab-info", "page-info");

            root.Q<Button>("close").clicked += () => OnCloseTapped?.Invoke();
            _trigger.RegisterCallback<PointerUpEvent>(TriggerPressed);
            _root.RegisterCallback<GeometryChangedEvent>(GeometryChanged);

            _console = new ConsoleTabPainter(_pages[DebugTab.Console]);
            _console.OnFilterChanged += () => OnFilterChanged?.Invoke();
            _console.OnClearTapped += () => OnClearTapped?.Invoke();
            _console.OnCopyTapped += () => OnCopyLogsTapped?.Invoke();
            _console.OnCopyDetailTapped += row => OnCopyDetailTapped?.Invoke(row);
            _console.OnRowSelected += row => OnLogSelected?.Invoke(row);

            _options = new OptionsTabPainter((ScrollView) _pages[DebugTab.Options]);
            _options.OnActivated += (option, text) => OnOptionActivated?.Invoke(option, text);

            _signals = new SignalsTabPainter((ScrollView) _pages[DebugTab.Signals]);
            _signals.OnFired += (row, text) => OnSignalFired?.Invoke(row, text);

            _stats = new StatsTabPainter(_pages[DebugTab.Stats]);

            _info = new InfoTabPainter(_pages[DebugTab.Info]);
            _info.OnCopyTapped += () => OnCopyInfoTapped?.Invoke();

            _pressFeedback = new PressFeedback(_root);

            _panel.style.display = DisplayStyle.None;
            _badge.style.display = DisplayStyle.None;
            _built = true;
        }

        private void Tab(VisualElement root, DebugTab tab, string buttonName, string pageName)
        {
            Button button = root.Q<Button>(buttonName);
            VisualElement page = root.Q<VisualElement>(pageName);

            if (button == null || page == null)
            {
                FlowLogger.LogError("DeviceDebuggerPanel.uxml has no " + buttonName + " or " + pageName + " element.");
                return;
            }

            button.clicked += () => OnTabSelected?.Invoke(tab);
            _tabs[tab] = button;
            _pages[tab] = page;
        }

        // ======================== Raw input ========================

        private void TriggerPressed(PointerUpEvent pointer)
        {
            pointer.StopPropagation();

            if (_triggerKind == DebugTrigger.None || _triggerKind == DebugTrigger.Button || _badgeShowing)
            {
                // A visible pill, or the badge on any trigger, opens on one tap. Read on the release
                // rather than the press, so the panel that appears under the finger does not take
                // the same touch as a tap of its own.
                OnTriggerTapped?.Invoke();
                return;
            }

            float now = Time.unscaledTime;

            if (_taps == 0 || now - _firstTapAt > DeviceDebuggerConstants.TRIPLE_TAP_WINDOW_SECONDS)
            {
                _taps = 0;
                _firstTapAt = now;
            }

            _taps++;

            if (_taps < 3) return;

            _taps = 0;
            OnTriggerTapped?.Invoke();
        }

        /// <summary>
        /// A None trigger is invisible and inert unless the badge is showing; a TripleTap zone is
        /// invisible but catches taps; a Button is always drawn.
        /// </summary>
        private void RefreshTriggerVisibility()
        {
            bool visible = _triggerKind == DebugTrigger.Button || _badgeShowing;
            bool interactive = _triggerKind != DebugTrigger.None || _badgeShowing;

            _trigger.style.opacity = visible ? 1f : 0f;
            _trigger.pickingMode = interactive ? PickingMode.Position : PickingMode.Ignore;
        }

        private void GeometryChanged(GeometryChangedEvent _) => ApplySafeArea();

        /// <summary>
        /// UI Toolkit knows nothing of the notch; the root is inset by the safe area, converted from
        /// screen pixels to panel units so the scale mode is honoured. Inset by its offsets rather
        /// than padded: the trigger and the panel are positioned absolutely inside it, and padding
        /// does not move an absolute child. Re-applied whenever the safe area itself changes, since
        /// a rotation is not a geometry change of the root.
        /// </summary>
        private void ApplySafeArea()
        {
            IPanel panel = _root?.panel;

            if (panel == null) return;

            Rect safe = Screen.safeArea;
            Vector2 topLeft = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMin, Screen.height - safe.yMax));
            Vector2 bottomRight = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMax, Screen.height - safe.yMin));
            Vector2 size = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height));

            float left = Mathf.Max(0f, topLeft.x);
            float top = Mathf.Max(0f, topLeft.y);
            float right = Mathf.Max(0f, size.x - bottomRight.x);
            float bottom = Mathf.Max(0f, size.y - bottomRight.y) + _bottomInset;

            // The panel keeps the whole screen and pads its content, so the strips a notch or a
            // rounded corner takes are covered rather than left showing the game through them.
            _panel.style.paddingLeft = left;
            _panel.style.paddingTop = top;
            _panel.style.paddingRight = right;
            _panel.style.paddingBottom = bottom;

            // The trigger sits in its corner, a finger's width in from the same insets.
            bool atTop = _corner == DebugCorner.TopLeft || _corner == DebugCorner.TopRight;
            bool atLeft = _corner == DebugCorner.TopLeft || _corner == DebugCorner.BottomLeft;
            var auto = new StyleLength(StyleKeyword.Auto);
            _trigger.style.top = atTop ? new StyleLength(top + TRIGGER_MARGIN) : auto;
            _trigger.style.bottom = atTop ? auto : new StyleLength(bottom + TRIGGER_MARGIN);
            _trigger.style.left = atLeft ? new StyleLength(left + TRIGGER_MARGIN) : auto;
            _trigger.style.right = atLeft ? auto : new StyleLength(right + TRIGGER_MARGIN);

            _appliedSafeArea = Screen.safeArea;
        }
    }
}