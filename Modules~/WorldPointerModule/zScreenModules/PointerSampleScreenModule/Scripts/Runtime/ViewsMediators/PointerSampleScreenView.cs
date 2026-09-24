#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.WorldPointerModule.PointerSampleScreenModule.Entities;
using UnityEngine;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.ViewsMediators
{
    /// <summary>
    /// The sample's display: three layers, one per off-screen mode, each with its own preset. It
    /// holds scene references and nothing else - which layer draws which id is the opening
    /// Command's to say. No animations, so the screen reports shown and hidden at once.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class PointerSampleScreenView : ScreenView
    {
        [SerializeField] private SampleLabelLayer _hideLayer;
        [SerializeField] private SampleLabelLayer _clampLayer;
        [SerializeField] private SampleLabelLayer _ignoreLayer;

        public SampleLabelLayer HideLayer => _hideLayer;
        public SampleLabelLayer ClampLayer => _clampLayer;
        public SampleLabelLayer IgnoreLayer => _ignoreLayer;

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
#endif