using System;
using System.Collections.Generic;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using UnityEngine.UIElements;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>The Info page: label and value rows, and a Copy button.</summary>
    public class InfoTabPainter
    {
        private readonly ScrollView _rows;

        public event Action OnCopyTapped;

        public InfoTabPainter(VisualElement page)
        {
            _rows = page.Q<ScrollView>("info-rows");
            page.Q<Button>("copy-info").clicked += () => OnCopyTapped?.Invoke();
        }

        public void Paint(IReadOnlyList<InfoRowVO> rows)
        {
            _rows.Clear();

            if (rows == null) return;

            foreach (InfoRowVO row in rows)
            {
                var line = new VisualElement();
                line.AddToClassList("dd-row");

                var label = new Label(row.Label);
                label.AddToClassList("dd-row-label");
                var value = new Label(row.Value);
                value.AddToClassList("dd-row-value");
                value.enableRichText = false;

                line.Add(label);
                line.Add(value);
                _rows.Add(line);
            }
        }
    }
}
