using System.Collections.Generic;
using Modules.LoadingModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.LoadingModule.Data.UnityObjects
{
    /// <summary>
    /// Every set the game loads through, and the steps each one waits for. A step key appears in
    /// exactly one set, which is what lets a Command report by step name alone.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_LoadingSets", menuName = "FlowIoC/LoadingModule/Data/CD_LoadingSets")]
    public class CD_LoadingSets : ScriptableObject
    {
        public List<LoadingSetCVO> Sets = new();
    }
}
