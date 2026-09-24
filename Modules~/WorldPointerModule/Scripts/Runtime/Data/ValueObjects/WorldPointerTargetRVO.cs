using System;
using Modules.WorldPointerModule.Enums;
using Modules.WorldPointerModule.Services;
using UnityEngine;

namespace Modules.WorldPointerModule.Data.ValueObjects
{
    /// <summary>
    /// One registered target. The serialized fields are its row in RD_WorldPointer; the rest is
    /// what the service works from and is never serialized. One object is both, so the row is
    /// written in place as the target changes and nothing is rebuilt. The service reads only the
    /// working half: a row edited in the Inspector during play changes nothing it does.
    /// </summary>
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

        [Tooltip("The last content sent, field by field, when it is a serializable class.")] [SerializeReference]
        public object Content;

        /// <summary>The channel the target is registered under.</summary>
        [NonSerialized] internal WorldPointerChannelRVO Owner;

        /// <summary>The last content, whatever its type - Content holds it only when it can be serialized.</summary>
        [NonSerialized] internal object Value;

        [NonSerialized] internal bool HasContent;

        [NonSerialized] internal IWorldPointerIndicator Indicator;

        /// <summary>The root canvas's camera, or null for an overlay canvas.</summary>
        [NonSerialized] internal Camera CanvasCamera;

        /// <summary>False until the indicator is told its first state.</summary>
        [NonSerialized] internal bool HasState;

        /// <summary>Where the target sits in its channel's member list, so it leaves it in one step.</summary>
        [NonSerialized] internal int Index;
    }
}