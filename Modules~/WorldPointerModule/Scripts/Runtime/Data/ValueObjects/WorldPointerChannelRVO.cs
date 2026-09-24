using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.WorldPointerModule.Data.ValueObjects
{
    /// <summary>One id as RD_WorldPointer shows it: who draws it, and what it points at.</summary>
    [Serializable]
    public class WorldPointerChannelRVO
    {
        public string Id;

        [Tooltip("The registered display - the component and the GameObject it sits on - or empty when none is.")]
        public string Display;

        public List<WorldPointerTargetRVO> Targets = new();
    }
}
