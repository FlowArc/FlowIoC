using Modules.ResourceFlyModule.Data.UnityObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Services
{
    /// <summary>
    /// What a screen registers to show one resource. ResourceFlyCounterDisplay is the ready one:
    /// it sits beside the counter's icon, the icons fly under it, and it counts up and plays a landing.
    /// </summary>
    public interface IResourceFlyCounter
    {
        /// <summary>Where the icons land.</summary>
        RectTransform Target { get; }

        /// <summary>What the icons fly in - just below the target in the hierarchy, so they pass under it.</summary>
        RectTransform IconParent { get; }

        /// <summary>The pool item the icons are, in a group the screen's module files.</summary>
        string IconPoolKey { get; }

        /// <summary>The motion flights into this counter play; null plays CD_ResourceFly's default.</summary>
        CD_ResourceFlyMotion Motion { get; }

        /// <summary>How this counter answers a landing; null plays CD_ResourceFly's default.</summary>
        CD_ResourceFlyLanding Landing { get; }

        /// <summary>Shows the value at once.</summary>
        void ShowValue(int value);

        /// <summary>An icon arrived: count up to the value over countUpSeconds and play the landing, if there is one.</summary>
        void Land(int value, float countUpSeconds, CD_ResourceFlyLanding landing);

        void BeginFlight();
        void EndFlight();
    }
}