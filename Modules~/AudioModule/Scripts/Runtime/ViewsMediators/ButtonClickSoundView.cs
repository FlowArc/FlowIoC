using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using Modules.AudioModule.Entities;
using Modules.AudioModule.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Modules.AudioModule.ViewsMediators
{
    /// <summary>
    /// Optional. Put it on a child of AudioServiceRoot and every Button pressed anywhere plays one
    /// key - the game's own, picked below. It watches the pointer, not the buttons, so a screen
    /// opened later needs nothing. A button that is not interactable, or that sits under a
    /// SuppressClickSound, is left alone.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class ButtonClickSoundView : MonoBehaviour, IView
    {
        [SerializeField, AudioKeyId, Tooltip("The key every button press plays.")]
        private string _key;

        private readonly List<RaycastResult> _hits = new();
        private PointerEventData _pointer;

        public bool IsRegistered { get; set; }

        public Action<AudioKey> OnButtonPressed;

        private void Update()
        {
            if (!TryGetPress(out Vector2 position) || EventSystem.current == null || string.IsNullOrEmpty(_key))
                return;

            _pointer ??= new PointerEventData(EventSystem.current);
            _pointer.position = position;
            _hits.Clear();
            EventSystem.current.RaycastAll(_pointer, _hits);

            if (_hits.Count == 0)
                return;

            Button button = _hits[0].gameObject.GetComponentInParent<Button>();

            if (button != null && button.IsInteractable() && button.GetComponentInParent<SuppressClickSound>() == null)
                OnButtonPressed?.Invoke(new AudioKey(_key));
        }

        private static bool TryGetPress(out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM
            Pointer pointer = Pointer.current;

            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                position = pointer.position.ReadValue();
                return true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }
#endif
            position = default;
            return false;
        }
    }
}
