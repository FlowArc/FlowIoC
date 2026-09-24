using System;
using Modules.WorldPointerModule.Enums;
using UnityEngine;

namespace Modules.WorldPointerModule.Data.ValueObjects
{
    /// <summary>One registered target as RD_WorldPointer shows it.</summary>
    [Serializable]
    public class WorldPointerTargetRVO
    {
        public Transform Target;

        [Tooltip("False after a Hide request until the next Show.")]
        public bool Visible = true;

        [Tooltip("True while a display has given this target an indicator; false while it waits for one.")]
        public bool Shown;

        [Tooltip("What the indicator was last told. Meaningful only while Shown.")]
        public WorldPointerState State;

        [Tooltip("The last content sent, as text - empty when none was sent.")]
        public string ContentText;

        [Tooltip("The last content sent, field by field, when it is a serializable class.")]
        [SerializeReference] public object Content;
    }
}
