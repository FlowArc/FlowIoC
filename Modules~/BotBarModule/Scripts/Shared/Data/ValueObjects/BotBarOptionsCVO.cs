using System;
using Modules.BotBarModule.Shared.Enums;
using UnityEngine;

namespace Modules.BotBarModule.Shared.Data.ValueObjects
{
    /// <summary>
    /// Every behaviour the bar has is a switch here. An option that is off does not animate at
    /// all: a tab with ScaleSelected off stays at scale 1 while selected.
    /// </summary>
    [Serializable]
    public class BotBarOptionsCVO
    {
        [Header("Selection")]
        public bool ScaleSelected = true;

        public float SelectedScale = 1.25f;
        public bool RaiseSelected = true;

        [Tooltip("Pixels the selected tab's content rises by.")]
        public float SelectedRaise = 24f;

        public bool WidenSelected = true;

        [Tooltip("The selected tab's flexible width against 1 for the others; the layout group slides the neighbours.")]
        public float SelectedWidth = 1.6f;

        public float Duration = 0.25f;
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public bool MoveHighlight = true;

        [Header("Look")]
        public BotBarTitleMode Titles = BotBarTitleMode.SelectedOnly;

        public bool NewMarkOnUnlock = true;

        [Header("Show and hide")]
        public float HideDuration = 0.3f;

        public AnimationCurve HideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}
