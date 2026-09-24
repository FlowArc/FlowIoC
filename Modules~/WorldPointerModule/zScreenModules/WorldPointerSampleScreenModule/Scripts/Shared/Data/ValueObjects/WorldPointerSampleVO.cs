#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Modules.WorldPointerModule.WorldPointerSampleScreenModule.Shared.Data.ValueObjects
{
    /// <summary>What the sample's world side sends a label: a word and a tint.</summary>
    [Serializable]
    public class WorldPointerSampleVO
    {
        public string Text;
        public Color Colour = Color.white;

        public override string ToString() => Text;
    }
}
#endif
