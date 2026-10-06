using System;
using Modules.ResourceFlyModule.Data.UnityObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>
    /// How one flight looks. In CD_ResourceFly's Looks it is a named look a binding picks by name;
    /// passed to Fly from code, its Name - when set - is the named look it starts from. An empty
    /// field leaves the choice to the next level: the named look, the counter, then CD_ResourceFly.
    /// </summary>
    [Serializable]
    public class ResourceFlyLookVO
    {
        [Tooltip("In CD_ResourceFly: the look's name. From code: the named look to start from.")]
        public string Name;

        [Tooltip("The pool item the icons are. Empty: the counter's.")]
        public string IconPoolKey;

        [Tooltip("The picture the icons show, laid on the icon's Image. Empty: the prefab's own.")]
        public Sprite Sprite;

        [Min(0)] [Tooltip("How much one icon carries. 0: the counter's, else CD_ResourceFly's.")]
        public int UnitsPerIcon;

        [Min(0)] [Tooltip("The most icons the flight uses. 0: the counter's, else CD_ResourceFly's.")]
        public int MaxIcons;

        [Tooltip("The motion the icons play. Empty: the counter's, else CD_ResourceFly's.")]
        public CD_ResourceFlyMotion Motion;

        [Tooltip("How the counter answers each icon. Empty: the counter's, else CD_ResourceFly's.")]
        public CD_ResourceFlyLanding Landing;

        [Tooltip("The flight only shows: it needs no Reserve, changes no value, and its landings still play.")]
        public bool VisualOnly;
    }
}
