using System;
using System.Collections.Generic;
using Facebook.Unity;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Enums;
using Modules.AnalyticsModule.Services;
using UnityEngine;

namespace Modules.AnalyticsModule.FacebookAnalyticsModule.Services
{
    /// <summary>
    /// Facebook App Events behind the socket. FB.Init once, ActivateApp on init and on every
    /// regained focus (what the SDK asks for on resume), events cut to Facebook's limits and the
    /// cut reported. Facebook has no user properties, so that call is ignored and says so once.
    /// FB.Mobile exists only on a phone, so the user id and the consent flags are skipped in the
    /// Editor with one log line. Until SetConsent is called the SDK runs with what its own
    /// FacebookSettings asset says - the SDK's default, not this module's decision.
    /// </summary>
    public class FacebookAnalyticsProvider : IAnalyticsProvider
    {
        private readonly AnalyticsEventTrimmer _trimmer = new();
        private readonly AnalyticsLimitsVO _limits = new(40, 40, 100, 25);
        private bool _userPropertyReported;
        private bool _focusHooked;

        public string Name => "Facebook";

        public void Initialize(Action<bool> ready)
        {
            if (FB.IsInitialized)
            {
                Activate();
                ready(true);
                return;
            }

            FB.Init(() =>
            {
                if (FB.IsInitialized)
                {
                    FlowLogger.Log("Initialize - Facebook is up.");
                    Activate();
                }
                else
                {
                    FlowLogger.LogError("Initialize - the Facebook SDK did not initialize; check the App ID in FacebookSettings.");
                }

                ready(FB.IsInitialized);
            });
        }

        public void Log(AnalyticsEventVO analyticsEvent)
        {
            AnalyticsEventVO trimmed = _trimmer.Trim(analyticsEvent, _limits, out string report);

            if (report.Length > 0)
                FlowLogger.LogWarning($"Log - '{analyticsEvent.Name}' cut for Facebook: {report}");

            if (trimmed.Parameters.Count == 0)
            {
                FB.LogAppEvent(trimmed.Name);
                return;
            }

            var parameters = new Dictionary<string, object>(trimmed.Parameters.Count);

            foreach (AnalyticsParameterVO parameter in trimmed.Parameters)
                parameters[parameter.Name] = ToValue(parameter);

            FB.LogAppEvent(trimmed.Name, null, parameters);
        }

        public void SetUserProperty(string name, string value)
        {
            if (_userPropertyReported)
                return;

            _userPropertyReported = true;
            FlowLogger.Log($"SetUserProperty - Facebook app events carry no user properties; '{name}' and the ones after it are not sent here.");
        }

        public void SetUserId(string userId)
        {
            if (Application.isEditor)
            {
                FlowLogger.Log("SetUserId - FB.Mobile exists only on a device; skipped in the Editor.");
                return;
            }

            FB.Mobile.UserID = userId;
        }

        public void SetConsent(AnalyticsConsentVO consent)
        {
            if (Application.isEditor)
            {
                FlowLogger.Log("SetConsent - FB.Mobile exists only on a device; skipped in the Editor.");
                return;
            }

            FB.Mobile.SetAutoLogAppEventsEnabled(consent.AnalyticsStorage);
            FB.Mobile.SetAdvertiserIDCollectionEnabled(consent.AdUserData);
            FB.Mobile.SetAdvertiserTrackingEnabled(consent.AdPersonalization);
            FB.Mobile.SetDataProcessingOptions(consent.AdUserData ? new string[0] : new[] {"LDU"}, 0, 0);
        }

        private void Activate()
        {
            FB.ActivateApp();

            if (_focusHooked)
                return;

            _focusHooked = true;
            Application.focusChanged += OnFocusChanged;
        }

        private void OnFocusChanged(bool focused)
        {
            if (focused && FB.IsInitialized)
                FB.ActivateApp();
        }

        private object ToValue(AnalyticsParameterVO parameter) => parameter.Kind switch
        {
            AnalyticsParameterKind.Long => parameter.LongValue,
            AnalyticsParameterKind.Double => parameter.DoubleValue,
            AnalyticsParameterKind.Bool => parameter.BoolValue ? 1 : 0,
            _ => parameter.StringValue ?? string.Empty
        };
    }
}
