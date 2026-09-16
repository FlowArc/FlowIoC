using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Enums;
using Modules.AnalyticsModule.Models;

namespace Modules.AnalyticsModule.Controllers
{
    /// <summary>The id is kept as current and goes to every Ready slot now; a slot that becomes ready later reads it at its flush.</summary>
    internal class SetUserIdCommand : Command
    {
        [Inject] private IAnalyticsModel _model { get; set; }

        [SignalParam] private string _userId { get; set; }

        public override void Execute()
        {
            _model.SetUserId(_userId);
            FlowLogger.Log($"SetUserId - {_userId}");

            foreach (ProviderSlotVO slot in _model.Slots)
            {
                if (slot.State != AnalyticsProviderState.Ready)
                    continue;

                try
                {
                    slot.Provider.SetUserId(_userId);
                }
                catch (Exception exception)
                {
                    FlowLogger.LogError($"SetUserId - '{slot.Provider.Name}' threw: {exception.Message}");
                }
            }
        }
    }
}
