#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.WorldPointerModule.Data.UnityObjects;
using UnityEngine;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.ViewsMediators
{
    /// <summary>
    /// The sample's display: three panels the labels are parented under, one per off-screen mode,
    /// each with its own preset. It holds scene references and nothing else - which panel draws
    /// which channel, and where the labels come from, is the opening Command's to say. No
    /// animations, so the screen reports shown and hidden at once.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class PointerSampleScreenView : ScreenView
    {
        [SerializeField] private RectTransform _hideParent;
        [SerializeField] private RectTransform _clampParent;
        [SerializeField] private RectTransform _ignoreParent;

        [SerializeField] private CD_WorldPointerOptions _hideOptions;
        [SerializeField] private CD_WorldPointerOptions _clampOptions;
        [SerializeField] private CD_WorldPointerOptions _ignoreOptions;

        public RectTransform HideParent => _hideParent;
        public RectTransform ClampParent => _clampParent;
        public RectTransform IgnoreParent => _ignoreParent;

        public CD_WorldPointerOptions HideOptions => _hideOptions;
        public CD_WorldPointerOptions ClampOptions => _clampOptions;
        public CD_WorldPointerOptions IgnoreOptions => _ignoreOptions;

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
#endif