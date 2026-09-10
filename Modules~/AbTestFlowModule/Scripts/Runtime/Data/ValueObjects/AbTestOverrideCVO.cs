using System;
using UnityEngine;

namespace Modules.AbTestFlowModule.Data.ValueObjects
{
    /// <summary>
    /// One asset this group replaces. Both fields are typed ScriptableObject so a designer can file
    /// another module's CD_ asset here without this module referencing that module's assembly.
    /// </summary>
    [Serializable]
    public class AbTestOverrideCVO
    {
        public ScriptableObject Original;
        public ScriptableObject Variant;
    }
}
