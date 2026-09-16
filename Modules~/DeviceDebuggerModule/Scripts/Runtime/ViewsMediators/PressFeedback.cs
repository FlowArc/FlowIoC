using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>
    /// A finger's answer on a control inside a scrolling page. UI Toolkit's own :active state is
    /// taken off a button the moment the ScrollView captures the pointer for a possible scroll,
    /// which on a touch screen is at once - so a Run button under a finger looked untouched. One
    /// listener on the panel's root sees every press first, finds the control under it, and puts
    /// a class on that control for as long as the finger is down, and for a moment at least, so a
    /// quick tap is seen too. The controls that answer are named by class, so a row built later
    /// joins in without registering anything.
    /// </summary>
    public class PressFeedback
    {
        private const string PRESSED_CLASS = "dd-pressed";
        private const float LEAST_SECONDS = 0.18f;

        private readonly string[] _controlClasses =
        {
            "dd-row-button", "dd-action", "dd-filter", "dd-channel", "dd-close",
            Toggle.ussClassName, BasePopupField<string, string>.ussClassName,
        };

        private readonly VisualElement _root;
        private VisualElement _pressed;
        private float _pressedAt;

        public PressFeedback(VisualElement root)
        {
            _root = root;
            _root.RegisterCallback<PointerDownEvent>(Pressed, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerUpEvent>(Released, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerCancelEvent>(Cancelled, TrickleDown.TrickleDown);
        }

        public void Detach()
        {
            _root.UnregisterCallback<PointerDownEvent>(Pressed, TrickleDown.TrickleDown);
            _root.UnregisterCallback<PointerUpEvent>(Released, TrickleDown.TrickleDown);
            _root.UnregisterCallback<PointerCancelEvent>(Cancelled, TrickleDown.TrickleDown);
            Release(_pressed);
        }

        private void Pressed(PointerDownEvent pointer)
        {
            Release(_pressed);
            _pressed = ControlUnder(pointer.target as VisualElement);

            if (_pressed == null) return;

            _pressed.AddToClassList(PRESSED_CLASS);
            _pressedAt = Time.unscaledTime;
        }

        private void Released(PointerUpEvent pointer) => ReleaseAfterLeast();

        private void Cancelled(PointerCancelEvent pointer) => ReleaseAfterLeast();

        // The release comes through the root whoever captured the pointer, so a scroll that took the
        // finger still ends the press. A tap shorter than the least moment keeps the look until then.
        private void ReleaseAfterLeast()
        {
            VisualElement control = _pressed;
            _pressed = null;

            if (control == null) return;

            float remaining = LEAST_SECONDS - (Time.unscaledTime - _pressedAt);

            if (remaining <= 0f)
            {
                Release(control);
                return;
            }

            control.schedule.Execute(() => Release(control)).ExecuteLater((long) (remaining * 1000f));
        }

        private void Release(VisualElement control)
        {
            if (control == null) return;

            control.RemoveFromClassList(PRESSED_CLASS);
            if (_pressed == control) _pressed = null;
        }

        // The press lands on a button's label or a toggle's checkmark; the control is an ancestor.
        private VisualElement ControlUnder(VisualElement element)
        {
            while (element != null && element != _root)
            {
                foreach (string controlClass in _controlClasses)
                {
                    if (element.ClassListContains(controlClass)) return element;
                }

                element = element.parent;
            }

            return null;
        }
    }
}
