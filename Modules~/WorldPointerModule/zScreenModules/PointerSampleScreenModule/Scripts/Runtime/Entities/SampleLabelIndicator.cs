#if UNITY_EDITOR
using Modules.WorldPointerModule.Entities;
using Modules.WorldPointerModule.PointerSampleScreenModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.Entities
{
    /// <summary>A label over a cube: the word it was sent, on a panel of the tint it was sent.</summary>
    public class SampleLabelIndicator : WorldPointerIndicator<WorldPointerSampleVO>
    {
        [SerializeField] private Text _label;
        [SerializeField] private Image _panel;

        public override void SetContent(WorldPointerSampleVO content)
        {
            if (_label != null) _label.text = content != null ? content.Text : string.Empty;
            if (_panel != null && content != null) _panel.color = content.Colour;
        }
    }
}
#endif
