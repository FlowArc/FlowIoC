using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace Modules.AssetDeliveryModule.Services
{
    public partial interface IAssetDeliveryService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Holds a flow until one pack is on the device, the pack named where the step is
            /// bound: <c>.ToSequence&lt;IAssetDeliveryService.Commands.Ensure&gt;("Levels")</c>.
            /// Reports no loading step - only the flow knows which set it is in; a flow that wants
            /// the bar calls EnsureAsync with a progress from a Command of its own.
            /// </summary>
            public class Ensure : Command<string>
            {
                [Inject] private IAssetDeliveryService _delivery { get; set; }

                public override async void Execute(string pack)
                {
                    Retain();

                    try
                    {
                        bool landed = await _delivery.EnsureAsync(new[] { pack });

                        if (!landed)
                        {
                            FlowLogger.Log($"IAssetDeliveryService.Commands.Ensure - '{pack}' did not land, so the sequence stops here.");
                            Stop();
                            return;
                        }

                        Release();
                    }
                    catch (Exception exception)
                    {
                        FlowLogger.LogError($"IAssetDeliveryService.Commands.Ensure threw while fetching '{pack}': {exception}");
                        Stop();
                    }
                }
            }
        }
    }
}
