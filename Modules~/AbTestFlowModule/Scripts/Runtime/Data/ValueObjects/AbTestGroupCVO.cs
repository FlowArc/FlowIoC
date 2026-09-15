using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.AbTestFlowModule.Data.ValueObjects
{
    /// <summary>
    /// One arm of an experiment. Tuning rarely moves alone - harder levels change what a run pays
    /// out - so an experiment changes a set of assets, and a group replaces them as a set. The
    /// first group is the control: its list is the game's own assets, the ones the experiment
    /// changes, and it is what a player outside every other group plays. Every other group lists,
    /// at the same index, the asset that replaces each of them. The slots are typed
    /// ScriptableObject so a designer can file another module's CD_ asset here without this
    /// module referencing that module's assembly.
    /// </summary>
    [Serializable]
    public class AbTestGroupCVO
    {
        public string Name;
        public List<ScriptableObject> Assets = new();
    }
}
