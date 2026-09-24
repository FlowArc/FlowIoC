#if UNITY_EDITOR
using Modules.WorldPointerModule.Entities;
using Modules.WorldPointerModule.PointerSampleScreenModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.Entities
{
    /// <summary>
    /// A label over a cube: the word it was sent, on a panel of the tint it was sent, and an arrow
    /// of the same tint that shows on the edge. The indicator's own rect is a square the arrow's
    /// whole turn fits in, so a clamped label keeps its arrow on screen.
    /// </summary>
    public class SampleLabelIndicator : WorldPointerIndicator<WorldPointerSampleVO>
    {
        [SerializeField] private Text _label;
        [SerializeField] private Image _panel;
        [SerializeField] private Image _arrow;

        public override void SetContent(WorldPointerSampleVO content)
        {
            if (_label != null) _label.text = content != null ? content.Text : string.Empty;
            if (content == null) return;

            if (_panel != null) _panel.color = content.Colour;
            if (_arrow != null) _arrow.color = content.Colour;
        }
    }
}
#endif