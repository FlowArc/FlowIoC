using System;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Services;
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

        public void Fly(ResourceFlyRouteVO route, int amount, Action finished) => Fly(route, amount, null, finished);

        public void Fly(ResourceFlyRouteVO route, int amount, ResourceFlyLookVO look, Action finished)
        {
            ResourceVO resource = _model.Get(route.Key);
            ResourceFlyLookVO named = Named(route, look);
            bool visualOnly = look is {VisualOnly: true} || named is {VisualOnly: true};
            // A visual flight only shows the amount; a counting one takes only what is pending and not already flying.
            int flying = visualOnly ? amount : Mathf.Min(amount, resource.Unflown);

            if (visualOnly && flying <= 0)
                FlowLogger.LogWarning($"Fly - {route} shows {amount}: a visual flight of nothing ends at once.");
            else if (flying < amount)
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
                    $"Fly - {route} has no {(counter == null ? "counter" : "source")} registered; "
                    + $"{(visualOnly ? "the flight ends" : $"{flying} settled")} at once.");

                if (!visualOnly)
                    Settle(resource, flying);

                finished?.Invoke();
                return;
            }

            ResourceFlyLookVO resolved = Resolve(look, named, counter);

            if (resolved.Motion == null)
            {
                FlowLogger.LogError(
                    $"Fly - {route} has no motion: neither the flight, its counter nor CD_ResourceFly names one; "
                    + $"{(visualOnly ? "the flight ends" : $"{flying} settled")} at once.",
                    counter as UnityEngine.Object);

                if (!visualOnly)
                    Settle(resource, flying);

                finished?.Invoke();
                return;
            }

            int unitsPerIcon = resolved.UnitsPerIcon;
            int wanted = unitsPerIcon > 0 ? flying / unitsPerIcon + (flying % unitsPerIcon > 0 ? 1 : 0) : flying;
            int icons = Mathf.Clamp(wanted, 1, Mathf.Max(1, resolved.MaxIcons));
            // Each icon carries its value and the last one what is left; capped by MaxIcons, the amount is shared out evenly.
            bool byValue = unitsPerIcon > 0 && icons == wanted;
            var flight = new ResourceFlightVO {Resource = resource, Counter = counter, IconsOut = icons, Finished = finished, Look = resolved};

            FlowLogger.Log($"Fly - {Describe(route, resolved)} {flying} in {icons} icons{(visualOnly ? ", visual only" : "")}");

            if (!visualOnly)
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
                // Shared evenly, the first flying % icons carry one more, so the shares add up to what flies.
                int share = byValue
                    ? Mathf.Min(unitsPerIcon, flying - index * unitsPerIcon)
                    : flying / icons + (index < flying % icons ? 1 : 0);
                Launch(flight, source, share, index * _model.Options.StaggerSeconds);
            }
        }

        /// <summary>The named look the flight picks - the given look's Name, else the route's; a name CD_ResourceFly lacks warns.</summary>
        private ResourceFlyLookVO Named(ResourceFlyRouteVO route, ResourceFlyLookVO look)
        {
            string name = !string.IsNullOrEmpty(look?.Name) ? look.Name : route.Look;

            if (string.IsNullOrEmpty(name))
                return null;

            if (_model.TryGetLook(name, out ResourceFlyLookVO named))
                return named;

            FlowLogger.LogWarning($"Fly - {route}: CD_ResourceFly holds no look '{name}'; the flight flies without it.");
            return null;
        }

        /// <summary>Field by field: the given look, the named look, the counter, CD_ResourceFly.</summary>
        private ResourceFlyLookVO Resolve(ResourceFlyLookVO given, ResourceFlyLookVO named, IResourceFlyCounter counter)
        {
            ResourceFlyOptionsCVO options = _model.Options;

            return new ResourceFlyLookVO
            {
                Name = named?.Name,
                IconPoolKey = FirstText(given?.IconPoolKey, named?.IconPoolKey, counter.IconPoolKey),
                Sprite = FirstAsset(given?.Sprite, named?.Sprite),
                UnitsPerIcon = FirstNumber(given?.UnitsPerIcon, named?.UnitsPerIcon, counter.UnitsPerIcon, options.UnitsPerIcon),
                MaxIcons = FirstNumber(given?.MaxIcons, named?.MaxIcons, counter.MaxIcons, options.MaxIcons),
                Motion = FirstAsset(given?.Motion, named?.Motion, counter.Motion, options.Motion),
                Landing = FirstAsset(given?.Landing, named?.Landing, counter.Landing, options.Landing),
                VisualOnly = given is {VisualOnly: true} || named is {VisualOnly: true}
            };
        }

        private static string Describe(ResourceFlyRouteVO route, ResourceFlyLookVO look) =>
            string.IsNullOrEmpty(look.Name) ? $"{route.Key} from {route.Source}" : $"{route.Key} from {route.Source}, look {look.Name}";

        private static string FirstText(params string[] levels)
        {
            foreach (string level in levels)
                if (!string.IsNullOrEmpty(level))
                    return level;

            return null;
        }

        private static int FirstNumber(params int?[] levels)
        {
            foreach (int? level in levels)
                if (level > 0)
                    return level.Value;

            return 0;
        }

        /// <summary>Unity's null: a destroyed asset counts as not set.</summary>
        private static T FirstAsset<T>(params T[] levels) where T : UnityEngine.Object
        {
            foreach (T level in levels)
                if (level != null)
                    return level;

            return null;
        }

        private void Launch(ResourceFlightVO flight, RectTransform source, int share, float delay)
        {
            IResourceFlyCounter counter = flight.Counter;
            string poolKey = flight.Look.IconPoolKey;
            IPoolableItem item = string.IsNullOrEmpty(poolKey) ? null : _poolService.Get(poolKey, counter.IconParent);

            if (item is not ResourceFlyIcon icon)
            {
                ReportLaunchFault(flight, counter, item);
                item?.Dismiss();
                Land(flight, share, false);
                return;
            }

            if (flight.Look.Sprite != null && !icon.CanShowSprite)
                ReportSpriteFault(flight, icon);

            icon.ShowSprite(flight.Look.Sprite);
            icon.Launch(source.position, counter.Target, flight.Look.Motion, delay, arrived => Land(flight, share, arrived));
        }

        /// <summary>Once per flight, on the icon so a double-click opens its prefab; the icon flies with its own picture.</summary>
        private static void ReportSpriteFault(ResourceFlightVO flight, ResourceFlyIcon icon)
        {
            if (flight.SpriteFaultReported)
                return;

            flight.SpriteFaultReported = true;
            FlowLogger.LogError($"The flight gives the icon '{flight.Look.IconPoolKey}' a sprite, but the icon has no Image slot; "
                                + "it flies with its own picture.", icon);
        }

        /// <summary>Once per flight: every icon of it meets the same fault.</summary>
        private static void ReportLaunchFault(ResourceFlightVO flight, IResourceFlyCounter counter, IPoolableItem item)
        {
            if (flight.LaunchFaultReported)
                return;

            flight.LaunchFaultReported = true;
            string poolKey = flight.Look.IconPoolKey;

            if (string.IsNullOrEmpty(poolKey))
                FlowLogger.LogError("The flight has no icon pool key: neither its look nor its counter names one. The flight lands at once.",
                    counter as UnityEngine.Object);
            else if (item == null)
                FlowLogger.LogError($"The pool has no item '{poolKey}' - is its pool group filed on a Root in the scene? "
                                    + "The flight lands at once.", counter as UnityEngine.Object);
            else
                FlowLogger.LogError($"The pool item '{poolKey}' is no ResourceFlyIcon; the flight lands at once.",
                    item as UnityEngine.Object);
        }

        /// <summary>An icon is down, or lost on the way; a counting icon's share is no longer pending, and the flight ends with its last icon.</summary>
        private void Land(ResourceFlightVO flight, int share, bool arrived)
        {
            ResourceVO resource = flight.Resource;

            // A visual flight's share was never pending: it lands the value already shown.
            if (!flight.Look.VisualOnly)
            {
                resource.Pending -= share;
                resource.Flying -= share;
            }

            IResourceFlyCounter counter = Alive(resource.Counter);

            try
            {
                if (arrived)
                    counter?.Land(resource.Shown, _model.Options.CountUpSeconds, flight.Look.Landing);
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