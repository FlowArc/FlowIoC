using System;
using System.Collections.Generic;
using Modules.WorldPointerModule.Services.Sub;
using UnityEngine;

namespace Modules.WorldPointerModule.Data.ValueObjects
{
    /// <summary>
    /// One channel: what the world side registers targets under and a screen registers one display
    /// for. The serialized fields are its row in RD_WorldPointer; the display and the member list
    /// the service walks are never serialized, so a row edited in the Inspector changes nothing.
    /// </summary>
    [Serializable]
    public class WorldPointerChannelRVO
    {
        public string Id;

        [Tooltip("The registered display, or empty when none is and the targets wait.")]
        public string Display;

        public List<WorldPointerTargetRVO> Targets = new();

        [NonSerialized] internal WorldPointerDisplaySlot Slot;

        /// <summary>The targets the service walks. Targets is the same list for the Inspector.</summary>
        [NonSerialized] internal readonly List<WorldPointerTargetRVO> Members = new();

        /// <summary>Where the channel sits in the model's channel list, so it leaves it in one step.</summary>
        [NonSerialized] internal int Index;
    }
}