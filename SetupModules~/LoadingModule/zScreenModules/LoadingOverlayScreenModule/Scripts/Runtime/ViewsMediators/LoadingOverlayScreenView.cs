using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.LoadingModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.ViewsMediators
{
    /// <summary>A dimmed screen, a turning ring and one line of text; input stops at the dim.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class LoadingOverlayScreenView : ScreenView
    {
        private const float DEGREES_PER_SECOND = 270f;

        [SerializeField] private RectTransform _spinner;
        [SerializeField] private Text _message;

        /// <summary>The set this overlay is drawing, or null before the first Apply.</summary>
        public string ShowingSet { get; private set; }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            ShowingSet = null;
            _message.text = string.Empty;
            _spinner.localRotation = Quaternion.identity;
        }

        public void Apply(LoadingSetStatusRVO status)
        {
            ShowingSet = status.Set;
            _message.text = status.Message ?? string.Empty;
        }

        private void Update() => _spinner.Rotate(0f, 0f, -DEGREES_PER_SECOND * Time.unscaledDeltaTime);

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}