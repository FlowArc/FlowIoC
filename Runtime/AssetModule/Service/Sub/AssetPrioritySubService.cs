using UnityEngine;
using FlowIoC.AssetModule.Gateway;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.AssetModule.Service.Sub
{
    /// <summary>
    /// Counts the background loads in flight. The first lowers the priority after remembering what
    /// it found; the last to finish puts that value back, so two overlapping background loads never
    /// restore a Low they set themselves.
    /// </summary>
    internal sealed class AssetPrioritySubService
    {
        [Inject] private IAddressablesGateway _gateway { get; set; }

        private int _inFlight;
        private ThreadPriority _restore;

        public void Enter()
        {
            if (_inFlight++ != 0)
                return;

            _restore = _gateway.BackgroundLoadingPriority;
            _gateway.BackgroundLoadingPriority = ThreadPriority.Low;
        }

        public void Exit()
        {
            if (_inFlight == 0)
                return;

            if (--_inFlight == 0)
                _gateway.BackgroundLoadingPriority = _restore;
        }
    }
}
