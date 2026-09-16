using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Enums;
using Modules.AnalyticsModule.Models;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.Controllers
{
    /// <summary>
    /// A provider answered. Ready: the current consent, then the user id, then every user
    /// property, then the queue oldest first - consent before the first event, because Firebase
    /// wants it so. Not ready: the slot is Failed and its queue is dropped with one line saying
    /// how many. A call that throws is reported once and the flush carries on.
    /// </summary>
    internal class FlushProviderCommand : Command
    {
        [Inject] private IAnalyticsModel _model { get; set; }

        [SignalParam] private IAnalyticsProvider _provider { get; set; }
        [SignalParam] private bool _ready { get; set; }

        public override void Execute()
        {
            if (!_model.TryGetSlot(_provider, out ProviderSlotVO slot))
            {
                FlowLogger.Log($"Flush - '{_provider.Name}' answered {_ready} after it was unplugged; ignored.");
                return;
            }

            if (!_ready)
            {
                slot.State = AnalyticsProviderState.Failed;
                int dropped = slot.ClearPending() + slot.TakeDropped();
                FlowLogger.Log($"Flush - '{_provider.Name}' did not initialize; {dropped} queued event(s) dropped.");
                return;
            }

            slot.State = AnalyticsProviderState.Ready;
            int capDropped = slot.TakeDropped();

            FlowLogger.Log(capDropped > 0
                ? $"Flush - '{_provider.Name}' ready; {capDropped} event(s) were dropped by the queue cap while it came up, {slot.PendingCount} follow."
                : $"Flush - '{_provider.Name}' ready; {slot.PendingCount} queued event(s) follow.");

            if (_model.Consent.HasValue)
            {
                AnalyticsConsentVO consent = _model.Consent.Value;
                Send(() => _provider.SetConsent(consent), "SetConsent");
            }

            if (!string.IsNullOrEmpty(_model.UserId))
            {
                string userId = _model.UserId;
                Send(() => _provider.SetUserId(userId), "SetUserId");
            }

            foreach (KeyValuePair<string, string> property in _model.UserProperties)
            {
                KeyValuePair<string, string> current = property;
                Send(() => _provider.SetUserProperty(current.Key, current.Value), "SetUserProperty " + current.Key);
            }

            while (slot.TryDequeue(out AnalyticsEventVO analyticsEvent))
            {
                AnalyticsEventVO current = analyticsEvent;
                Send(() => _provider.Log(current), "Log " + current.Name);
            }
        }

        private void Send(Action call, string what)
        {
            try
            {
                call();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Flush - '{_provider.Name}' threw on {what}: {exception.Message}");
            }
        }
    }
}
