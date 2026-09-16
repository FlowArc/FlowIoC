using System;
using System.Collections.Generic;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using UnityEngine.UIElements;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>
    /// The Signals page: a foldout per holder, a row per Incoming signal with a Fire button and,
    /// for one parseable payload, a field beside it. The framework's holders come last and start
    /// folded, so the game's own surface is what the page opens on.
    /// </summary>
    public class SignalsTabPainter
    {
        private readonly ScrollView _page;

        public event Action<DebugSignalVO, string> OnFired;

        public SignalsTabPainter(ScrollView page)
        {
            _page = page;
        }

        public void Paint(IReadOnlyList<DebugSignalVO> rows)
        {
            _page.Clear();

            if (rows == null || rows.Count == 0)
            {
                var note = new Label("No signal holder is bound across contexts in this scene.");
                note.AddToClassList("dd-note");
                _page.Add(note);
                return;
            }

            Foldout group = null;

            foreach (DebugSignalVO row in rows)
            {
                if (group == null || group.text != row.HolderName)
                {
                    group = new Foldout {text = row.HolderName, value = !row.IsFramework};
                    group.AddToClassList("dd-group");
                    if (row.IsFramework) group.AddToClassList("dd-group-framework");
                    _page.Add(group);
                }

                group.Add(Row(row));
            }
        }

        private VisualElement Row(DebugSignalVO signal)
        {
            var row = new VisualElement();
            row.AddToClassList("dd-row");

            var label = new Label(signal.SignalName);
            label.AddToClassList("dd-row-label");
            row.Add(label);

            if (!signal.CanFire)
            {
                var reason = new Label(signal.Reason);
                reason.AddToClassList("dd-row-reason");
                row.Add(reason);
                row.SetEnabled(false);
                return row;
            }

            TextField field = null;

            if (signal.PayloadTypes.Length == 1)
            {
                field = new TextField();
                field.AddToClassList("dd-row-field");
                field.textEdition.placeholder = signal.PayloadTypes[0].Name;
                row.Add(field);
            }

            var fire = new Button(() => OnFired?.Invoke(signal, field != null ? field.value : "")) {text = "Fire"};
            fire.AddToClassList("dd-row-button");
            row.Add(fire);

            return row;
        }
    }
}
