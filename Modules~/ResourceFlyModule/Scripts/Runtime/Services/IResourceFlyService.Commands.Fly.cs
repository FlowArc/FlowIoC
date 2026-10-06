using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.ResourceFlyModule.Data.ValueObjects;

namespace Modules.ResourceFlyModule.Services
{
    public partial interface IResourceFlyService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Flies the amount the signal carries along the route bound with the step, and holds the
            /// sequence until the last icon is down:
            /// <c>.ToSequence&lt;IResourceFlyService.Commands.Fly&gt;(new ResourceFlyRouteVO("WinReward", "Coin", "Banknote"))</c>.
            /// The route may name a look from CD_ResourceFly's Looks. A flight that cannot play ends the step at once.
            /// </summary>
            public class Fly : Command<ResourceFlyRouteVO>
            {
                [SignalParam] private int _amount { get; set; }

                [Inject] private IResourceFlyService _service { get; set; }

                public override void Execute(ResourceFlyRouteVO route)
                {
                    Retain();

                    try
                    {
                        _service.Fly(route, _amount, () => Release());
                    }
                    catch (Exception exception)
                    {
                        // A throw before the flight could end - a FlightStarted listener, say - would
                        // otherwise hold the sequence for ever; the sequence goes on without the coins.
                        FlowLogger.LogError($"IResourceFlyService.Commands.Fly - {route} threw: {exception}");
                        Release();
                    }
                }
            }
        }
    }
}