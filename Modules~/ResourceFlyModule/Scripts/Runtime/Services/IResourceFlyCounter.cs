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

        /// <summary>The pool item the icons are when a flight's look names none, in a group the screen's module files.</summary>
        string IconPoolKey { get; }

        /// <summary>The motion flights into this counter play when their look names none; null plays CD_ResourceFly's.</summary>
        CD_ResourceFlyMotion Motion { get; }

        /// <summary>How this counter answers a landing when the flight's look names none; null plays CD_ResourceFly's.</summary>
        CD_ResourceFlyLanding Landing { get; }

        /// <summary>The most icons one flight into this counter uses when its look names none; 0 uses CD_ResourceFly's.</summary>
        int MaxIcons { get; }

        /// <summary>How much one icon into this counter carries when the flight's look names none; 0 uses CD_ResourceFly's.</summary>
        int UnitsPerIcon { get; }

        /// <summary>Shows the value at once.</summary>
        void ShowValue(int value);

        /// <summary>An icon arrived: count up to the value over countUpSeconds and play the landing, if there is one.</summary>
        void Land(int value, float countUpSeconds, CD_ResourceFlyLanding landing);

        void BeginFlight();
        void EndFlight();
    }
}