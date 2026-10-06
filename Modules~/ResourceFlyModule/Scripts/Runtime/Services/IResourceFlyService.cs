using System;
using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Services
{
    /// <summary>
    /// Keeps the value a counter shows apart from the saved one while a reward flies in, per
    /// resource key: shown = saved - pending. A grant is reserved before it is saved, so the
    /// counter stays where it was; a flight lands it icon by icon. A flight that cannot play is
    /// settled at once, so the two values never drift. It knows no game and plays no feedback:
    /// the counter's events are for the screen that hosts it.
    /// </summary>
    public partial interface IResourceFlyService
    {
        /// <summary>Makes the counter the one showing the key, and shows it the key's value.</summary>
        void RegisterCounter(string key, IResourceFlyCounter counter);

        /// <summary>Lets the key go, if that counter still holds it; a counter that took the key since keeps it.</summary>
        void UnregisterCounter(string key, IResourceFlyCounter counter);

        /// <summary>Names a RectTransform flights can leave from.</summary>
        void RegisterSource(string source, RectTransform rect);

        void UnregisterSource(string source);

        /// <summary>Raises the pending amount; call it before the grant is saved and announced.</summary>
        void Reserve(string key, int amount);

        /// <summary>The saved value as it now is; the counter shows it less what is pending.</summary>
        void SetValue(string key, int saved);

        /// <summary>The value a counter of the key shows.</summary>
        int GetShown(string key);

        /// <summary>
        /// Flies up to that much of the pending amount from the route's source to the key's counter,
        /// with the route's named look if it names one, and calls finished once the last icon is down,
        /// or at once when the flight cannot play.
        /// </summary>
        void Fly(ResourceFlyRouteVO route, int amount, Action finished);

        /// <summary>
        /// Fly with a look of its own: each field it sets comes before its named look - its Name, else
        /// the route's - the counter's and CD_ResourceFly's. A VisualOnly look flies the amount without
        /// a Reserve and changes no value.
        /// </summary>
        void Fly(ResourceFlyRouteVO route, int amount, ResourceFlyLookVO look, Action finished);

        public static partial class Commands
        {
        }
    }
}