using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ConsoleModule;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Modules.InputModule.ViewsMediators
{
    /// <summary>
    /// The one place the module touches the Input System. It reads the actions out of the asset
    /// the module ships - assigned in the inspector, so a game can point it at an asset of its own
    /// without touching this file - and hands what happens to the mediator as plain callbacks.
    ///
    /// Nothing here decides anything. Whether a drag means something is the game's business.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class InputView : MonoBehaviour, IView
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _pointerMapName = "Pointer";
        [SerializeField] private string _pressActionName = "Press";
        [SerializeField] private string _positionActionName = "Position";

        public bool IsRegistered { get; set; }

        public Action<Vector2> OnPointerPressed;
        public Action<Vector2> OnPointerDragged;
        public Action<Vector2> OnPointerReleased;

        private InputAction _press;
        private InputAction _position;
        private bool _isPressed;

        public InputActionAsset Actions => _actions;

        private void Awake()
        {
            if (_actions == null)
            {
                FlowLogger.LogError(FlowLogType.InputModule,
                    $"{nameof(InputView)} has no action asset assigned.");

                return;
            }

            InputActionMap pointerMap = _actions.FindActionMap(_pointerMapName);

            if (pointerMap == null)
            {
                FlowLogger.LogError(FlowLogType.InputModule,
                    $"Action map '{_pointerMapName}' is not in {_actions.name}.");

                return;
            }

            _press = pointerMap.FindAction(_pressActionName);
            _position = pointerMap.FindAction(_positionActionName);

            if (_press == null || _position == null)
            {
                FlowLogger.LogError(FlowLogType.InputModule,
                    $"'{_pressActionName}' or '{_positionActionName}' is not in the '{_pointerMapName}' map.");

                return;
            }

            _press.started += HandlePressStarted;
            _press.canceled += HandlePressCanceled;
            _position.performed += HandlePositionPerformed;

            pointerMap.Enable();
        }

        private void OnDestroy()
        {
            if (_press != null)
            {
                _press.started -= HandlePressStarted;
                _press.canceled -= HandlePressCanceled;
            }

            if (_position != null)
                _position.performed -= HandlePositionPerformed;
        }

        // A press that is still held when the object goes away never gets its release, so the
        // next enable would start mid-drag.
        private void OnDisable() => _isPressed = false;

        private void HandlePressStarted(InputAction.CallbackContext context)
        {
            _isPressed = true;
            OnPointerPressed?.Invoke(ReadPosition());
        }

        private void HandlePressCanceled(InputAction.CallbackContext context)
        {
            _isPressed = false;
            OnPointerReleased?.Invoke(ReadPosition());
        }

        // Position fires whenever the pointer moves, pressed or not. Only the pressed half is
        // announced: a signal per mouse move would be a dispatch per frame for something almost
        // no game wants.
        private void HandlePositionPerformed(InputAction.CallbackContext context)
        {
            if (!_isPressed) return;

            OnPointerDragged?.Invoke(context.ReadValue<Vector2>());
        }

        private Vector2 ReadPosition() =>
            _position != null ? _position.ReadValue<Vector2>() : Vector2.zero;
    }
}
