using System;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Services;
using Modules.ResourceFlyModule.Data.UnityObjects;
using Modules.ResourceFlyModule.Data.ValueObjects;
using Modules.ResourceFlyModule.Entities;
using Modules.ResourceFlyModule.Models;
using UnityEngine;

namespace Modules.ResourceFlyModule.Services
{
    internal class ResourceFlyService : IResourceFlyService
    {
        [Inject] private IResourceFlyModel _model { get; set; }
        [Inject] private IPoolService _poolService { get; set; }

        public void RegisterCounter(string key, IResourceFlyCounter counter)
        {
            ResourceVO resource = _model.Get(key);
            resource.Counter = counter;
            counter.ShowValue(resource.Shown);
        }

        public void UnregisterCounter(string key, IResourceFlyCounter counter)
        {
            ResourceVO resource = _model.Get(key);

            // A counter that took the key since keeps it: only the one leaving lets go.
            if (ReferenceEquals(resource.Counter, counter))
                resource.Counter = null;
        }

        public void RegisterSource(string source, RectTransform rect) => _model.SetSource(source, rect);

        public void UnregisterSource(string source) => _model.RemoveSource(source);

        public void Reserve(string key, int amount)
        {
            if (amount <= 0)
                return;

            ResourceVO resource = _model.Get(key);
            resource.Pending += amount;
            FlowLogger.Log($"Reserve - {key} +{amount} pending={resource.Pending}");
        }

        public void SetValue(string key, int saved)
        {
            ResourceVO resource = _model.Get(key);
            resource.Saved = saved;
            Alive(resource.Counter)?.ShowValue(resource.Shown);
        }

        public int GetShown(string key) => _model.Get(key).Shown;

        public void Fly(ResourceFlyRouteVO route, int amount, Action finished)
        {
            ResourceVO resource = _model.Get(route.Key);
            // What is already flying is not flown twice: a second flight takes only what is left.
            int flying = Mathf.Min(amount, resource.Unflown);

            if (flying < amount)
                FlowLogger.LogWarning($"Fly - {route} asked for {amount} with {resource.Unflown} pending and not flying; flies {flying}.");

            if (flying <= 0)
            {
                finished?.Invoke();
                return;
            }

            IResourceFlyCounter counter = Alive(resource.Counter);

            if (counter == null || !_model.TryGetSource(route.Source, out RectTransform source))
            {
                FlowLogger.LogWarning(
                    $"Fly - {route} has no {(counter == null ? "counter" : "source")} registered; {flying} settled at once.");
                Settle(resource, flying);
                finished?.Invoke();
                return;
            }

            CD_ResourceFlyMotion motion = counter.Motion != null ? counter.Motion : _model.Options.Motion;

            if (motion == null)
            {
                FlowLogger.LogError($"Fly - {route} has no motion: neither its counter nor CD_ResourceFly names one; {flying} settled at once.",
                    counter as UnityEngine.Object);
                Settle(resource, flying);
                finished?.Invoke();
                return;
            }

            ResourceFlyOptionsCVO options = _model.Options;
            int icons = Mathf.Clamp(flying, 1, Mathf.Max(1, options.MaxIcons));
            var flight = new ResourceFlightVO {Resource = resource, Counter = counter, IconsOut = icons, Finished = finished};

            FlowLogger.Log($"Fly - {route} {flying} in {icons} icons");
            resource.Flying += flying;

            try
            {
                counter.BeginFlight();
            }
            catch (Exception exception)
            {
                // A listener's fault - a screen's FlightStarted - does not stop the coins: they still land.
                FlowLogger.LogError($"Fly - {route}: the counter's FlightStarted threw, the coins fly on: {exception}");
            }

            for (int index = 0; index < icons; index++)
            {
                // The first flying % icons carry one more, so the shares add up to what flies.
                int share = flying / icons + (index < flying % icons ? 1 : 0);
                Launch(flight, source, share, index * options.StaggerSeconds, motion);
            }
        }

        private void Launch(ResourceFlightVO flight, RectTransform source, int share, float delay, CD_ResourceFlyMotion motion)
        {
            IResourceFlyCounter counter = flight.Counter;
            IPoolableItem item = _poolService.Get(counter.IconPoolKey, counter.IconParent);

            if (item is not ResourceFlyIcon icon)
            {
                ReportLaunchFault(flight, counter, item);
                item?.Dismiss();
                Land(flight, share, false);
                return;
            }

            icon.Launch(source.position, counter.Target, motion, delay, arrived => Land(flight, share, arrived));
        }

        /// <summary>Once per flight: every icon of it meets the same fault.</summary>
        private static void ReportLaunchFault(ResourceFlightVO flight, IResourceFlyCounter counter, IPoolableItem item)
        {
            if (flight.LaunchFaultReported)
                return;

            flight.LaunchFaultReported = true;

            if (item == null)
                FlowLogger.LogError($"The pool has no item '{counter.IconPoolKey}' - is its pool group filed on a Root in the scene? "
                                    + "The flight lands at once.", counter as UnityEngine.Object);
            else
                FlowLogger.LogError($"The pool item '{counter.IconPoolKey}' is no ResourceFlyIcon; the flight lands at once.",
                    item as UnityEngine.Object);
        }

        /// <summary>An icon is down, or lost on the way; either way its share is no longer pending, and the flight ends with its last icon.</summary>
        private void Land(ResourceFlightVO flight, int share, bool arrived)
        {
            ResourceVO resource = flight.Resource;
            resource.Pending -= share;
            resource.Flying -= share;

            IResourceFlyCounter counter = Alive(resource.Counter);

            try
            {
                if (arrived)
                    counter?.Land(resource.Shown, _model.Options);
                else
                    counter?.ShowValue(resource.Shown);
            }
            catch (Exception exception)
            {
                // A screen's Landed listener - a sound, a haptic - does not hold the flight.
                FlowLogger.LogError($"Fly - {resource.Key}: the counter's Landed threw, the flight goes on: {exception}");
            }

            flight.IconsOut--;

            if (flight.IconsOut > 0)
                return;

            try
            {
                // The end is owed to the counter the flight began on, even if another holds the key now.
                Alive(flight.Counter)?.EndFlight();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Fly - {resource.Key}: the counter's FlightEnded threw: {exception}");
            }

            flight.Finished?.Invoke();
        }

        private static void Settle(ResourceVO resource, int amount)
        {
            resource.Pending -= amount;
            Alive(resource.Counter)?.ShowValue(resource.Shown);
        }

        /// <summary>A counter whose screen was destroyed is gone, though C# still holds it.</summary>
        private static IResourceFlyCounter Alive(IResourceFlyCounter counter) =>
            counter is UnityEngine.Object unityObject && unityObject == null ? null : counter;
    }
}