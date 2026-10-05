using UnityEngine;

namespace Modules.LoadingModule.Entities
{
    /// <summary>
    /// Turns the icon of the LoadingSpinner prefab, the indicator a screen puts inside its own
    /// prefab while it waits for its data. The screen switches the GameObject on and off; nothing
    /// here decides when. It blocks no input, so a close button beside it keeps working, and the
    /// prefab carries a Canvas of its own so the turning rebuilds that canvas and not the screen's.
    /// A game makes a variant of the prefab to change the icon, its size or its speed.
    /// </summary>
    public class LoadingSpinner : MonoBehaviour
    {
        [SerializeField] private RectTransform _icon;
        [SerializeField] private float _degreesPerSecond = 270f;

        private void OnEnable() => _icon.localRotation = Quaternion.identity;

        private void Update() => _icon.Rotate(0f, 0f, -_degreesPerSecond * Time.unscaledDeltaTime);
    }
}
