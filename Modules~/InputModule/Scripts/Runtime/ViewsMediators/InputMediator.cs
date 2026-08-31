using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.InputModule.Shared.Signals;
using UnityEngine;

namespace Modules.InputModule.ViewsMediators
{
    /// <summary>
    /// Turns what the view saw into the module's outgoing signals, and nothing else.
    /// </summary>
    public class InputMediator : IMediator
    {
        [Inject]       private InputView _view    { get; set; }
        [InjectSignal] private InputSignals      _signals { get; set; }

        public void OnRegister()
        {
            _view.OnPointerPressed  += HandlePressed;
            _view.OnPointerDragged  += HandleDragged;
            _view.OnPointerReleased += HandleReleased;
        }

        public void OnRemove()
        {
            _view.OnPointerPressed  -= HandlePressed;
            _view.OnPointerDragged  -= HandleDragged;
            _view.OnPointerReleased -= HandleReleased;
        }

        private void HandlePressed(Vector2 position) =>
            _signals.Outgoing.PointerPressed.Dispatch(position);

        private void HandleDragged(Vector2 position) =>
            _signals.Outgoing.PointerDragged.Dispatch(position);

        private void HandleReleased(Vector2 position) =>
            _signals.Outgoing.PointerReleased.Dispatch(position);
    }
}
