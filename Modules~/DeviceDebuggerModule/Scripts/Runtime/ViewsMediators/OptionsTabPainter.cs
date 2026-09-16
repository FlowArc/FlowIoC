using System;
using System.Collections.Generic;
using System.Globalization;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Enums;
using UnityEngine.UIElements;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>
    /// The Options page: a foldout per category, a row per option drawn by its kind. A control
    /// raises the row and its value as text - the panel edits text, a Command parses it - and
    /// a Value row shows what its signal last carried. Rows are found again by key so a value
    /// that arrives later repaints only its own row.
    /// </summary>
    public class OptionsTabPainter
    {
        private readonly ScrollView _page;
        private readonly Dictionary<string, VisualElement> _controls = new();
        private readonly Dictionary<string, Label> _values = new();

        public event Action<DebugOptionVO, string> OnActivated;

        public OptionsTabPainter(ScrollView page)
        {
            _page = page;
        }

        public void Paint(IReadOnlyList<DebugOptionVO> options)
        {
            _page.Clear();
            _controls.Clear();
            _values.Clear();

            if (options == null || options.Count == 0)
            {
                _page.Add(Note("No options. Put [DebugOption] on a public signal field or a shipped step."));
                return;
            }

            // A value row that shares its key with a control feeds that control and is not drawn twice.
            var controlKeys = new HashSet<string>();

            foreach (DebugOptionVO option in options)
            {
                if (option.Kind != DebugOptionKind.Value && option.Kind != DebugOptionKind.Unsupported)
                    controlKeys.Add(option.Key);
            }

            Foldout group = null;

            foreach (DebugOptionVO option in options)
            {
                if (option.Kind == DebugOptionKind.Value && controlKeys.Contains(option.Key)) continue;

                if (group == null || group.text != option.Category)
                {
                    group = new Foldout {text = option.Category, value = true};
                    group.AddToClassList("dd-group");
                    _page.Add(group);
                }

                group.Add(Row(option));
            }
        }

        /// <summary>A Value row's text, and the shown state of the control that shares its key.</summary>
        public void RefreshValue(DebugOptionVO option)
        {
            if (option == null) return;

            if (_values.TryGetValue(option.Key, out Label value))
                value.text = option.LastValue;

            if (!_controls.TryGetValue(option.Key, out VisualElement control)) return;

            switch (control)
            {
                case Toggle toggle when bool.TryParse(option.LastValue, out bool on):
                    toggle.SetValueWithoutNotify(on);
                    break;
                case Slider slider when float.TryParse(option.LastValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float f):
                    slider.SetValueWithoutNotify(f);
                    break;
                case SliderInt sliderInt when int.TryParse(option.LastValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i):
                    sliderInt.SetValueWithoutNotify(i);
                    break;
                case TextField field:
                    field.SetValueWithoutNotify(option.LastValue);
                    break;
                case DropdownField dropdown when dropdown.choices.Contains(option.LastValue):
                    dropdown.SetValueWithoutNotify(option.LastValue);
                    break;
            }
        }

        private VisualElement Row(DebugOptionVO option)
        {
            var row = new VisualElement();
            row.AddToClassList("dd-row");

            var label = new Label(option.Label);
            label.AddToClassList("dd-row-label");
            row.Add(label);

            switch (option.Kind)
            {
                case DebugOptionKind.Button:
                    var button = new Button(() => OnActivated?.Invoke(option, "")) {text = "Run"};
                    button.AddToClassList("dd-row-button");
                    row.Add(button);
                    break;

                case DebugOptionKind.Toggle:
                    var toggle = new Toggle();
                    toggle.AddToClassList("dd-row-toggle");
                    toggle.RegisterValueChangedCallback(changed => OnActivated?.Invoke(option, changed.newValue ? "true" : "false"));
                    row.Add(toggle);
                    Remember(option, toggle);
                    break;

                case DebugOptionKind.Number:
                    row.Add(NumberControl(option));
                    break;

                case DebugOptionKind.Text:
                    var field = new TextField();
                    field.AddToClassList("dd-row-field");
                    var set = new Button(() => OnActivated?.Invoke(option, field.value)) {text = "Set"};
                    set.AddToClassList("dd-row-button");
                    row.Add(field);
                    row.Add(set);
                    Remember(option, field);
                    break;

                case DebugOptionKind.Choice:
                    var dropdown = new DropdownField(new List<string>(option.Choices ?? Array.Empty<string>()), 0);
                    dropdown.AddToClassList("dd-row-dropdown");
                    dropdown.RegisterValueChangedCallback(changed => OnActivated?.Invoke(option, changed.newValue));
                    row.Add(dropdown);
                    Remember(option, dropdown);
                    break;

                case DebugOptionKind.Value:
                    var value = new Label(option.LastValue);
                    value.AddToClassList("dd-row-value");
                    row.Add(value);
                    _values[option.Key] = value;
                    break;

                default:
                    var reason = new Label(option.Reason);
                    reason.AddToClassList("dd-row-reason");
                    row.Add(reason);
                    row.SetEnabled(false);
                    break;
            }

            return row;
        }

        private VisualElement NumberControl(DebugOptionVO option)
        {
            if (option.HasRange && option.PayloadType == typeof(int))
            {
                var sliderInt = new SliderInt((int) option.Min, (int) option.Max) {showInputField = true};
                sliderInt.AddToClassList("dd-row-slider");
                sliderInt.RegisterValueChangedCallback(changed => OnActivated?.Invoke(option, changed.newValue.ToString(CultureInfo.InvariantCulture)));
                Remember(option, sliderInt);
                return sliderInt;
            }

            if (option.HasRange)
            {
                var slider = new Slider((float) option.Min, (float) option.Max) {showInputField = true};
                slider.AddToClassList("dd-row-slider");
                slider.RegisterValueChangedCallback(changed => OnActivated?.Invoke(option, changed.newValue.ToString(CultureInfo.InvariantCulture)));
                Remember(option, slider);
                return slider;
            }

            var holder = new VisualElement();
            holder.AddToClassList("dd-row-inline");
            var field = new TextField();
            field.AddToClassList("dd-row-field");
            var set = new Button(() => OnActivated?.Invoke(option, field.value)) {text = "Set"};
            set.AddToClassList("dd-row-button");
            holder.Add(field);
            holder.Add(set);
            Remember(option, field);
            return holder;
        }

        private void Remember(DebugOptionVO option, VisualElement control) => _controls[option.Key] = control;

        private static Label Note(string text)
        {
            var note = new Label(text);
            note.AddToClassList("dd-note");
            return note;
        }
    }
}
