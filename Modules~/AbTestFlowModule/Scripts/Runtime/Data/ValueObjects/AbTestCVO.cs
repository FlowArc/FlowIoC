using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.AbTestFlowModule.Data.ValueObjects
{
    /// <summary>
    /// One experiment. Whether it runs is not its own to say: CD_AbTests names the one active test
    /// by id. Raising Version starts it over for everybody, including the players the share left
    /// outside it - an experiment is restarted rather than amended.
    ///
    /// TestUserPercent is the share of players who enter the test at all: 80 puts 80 of every 100
    /// into one of the groups, split evenly, and leaves the other 20 playing the original
    /// configuration outside the test, where they count for nothing.
    /// </summary>
    [Serializable]
    public class AbTestCVO
    {
        public string Id;
        public int    Version;

        [Range(0f, 100f)] public float TestUserPercent;
        public List<AbTestGroupCVO> Groups = new();
    }
}
