using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.AbTestFlowModule.Data.ValueObjects
{
    /// <summary>
    /// One experiment. Raising Version starts it over for everybody, including the players the
    /// rollout left outside it - an experiment is restarted rather than amended.
    /// </summary>
    [Serializable]
    public class AbTestCVO
    {
        public string Id;
        public int    Version;
        public bool   IsActive;

        [Range(0f, 100f)] public float RolloutPercent;
        public List<AbTestGroupCVO> Groups = new();
    }
}
