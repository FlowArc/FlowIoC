using System;
using FlowIoC.BaseModule.Signals;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Services;
using Modules.AdsModule.Shared.Data.ValueObjects;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Signals
{
    /// <summary>
    /// What the service and the listener say to the module's own commands. Dispatched by nothing
    /// outside this assembly. There is no Incoming and no Outgoing here: those two halves say
    /// what a module accepts and announces across a boundary, and an internal signal never
    /// crosses one.
    /// </summary>
    internal class AdsInternalSignals : ISignalHolder
    {
        public Signal<IAdsProvider> Plug = new();
        public Signal<IAdsProvider> Unplug = new();
        public Signal Launched = new();
        public Signal Initialize = new();
        public Signal<bool> ProviderInitialized = new();
        public Signal<AdFormat> Load = new();
        public Signal<AdFormat> Loaded = new();
        public Signal<AdFormat, string> LoadFailed = new();
        public Signal<AdFormat, string, Action<AdResultVO>> Show = new();
        public Signal<AdFormat> Displayed = new();
        public Signal<AdFormat, string> DisplayFailed = new();
        public Signal<AdRewardVO> RewardEarned = new();
        public Signal<AdFormat> Closed = new();
        public Signal<AdRevenueVO> RevenuePaid = new();
        public Signal<AdsConsentVO> SetConsent = new();
        public Signal<bool> SetAdsRemoved = new();
        public Signal<bool> SetMuted = new();
        public Signal ShowDebugger = new();
    }
}
