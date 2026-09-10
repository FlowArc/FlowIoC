using System;
using System.Collections.Generic;

namespace Modules.AbTestFlowModule.Data.ValueObjects
{
    /// <summary>
    /// One arm of an experiment. Tuning rarely moves alone - harder levels change what a run pays
    /// out - so a group carries every asset that has to change together, and they are applied as a
    /// set. The first group of an experiment is the control and leaves this list empty.
    /// </summary>
    [Serializable]
    public class AbTestGroupCVO
    {
        public string Name;
        public List<AbTestOverrideCVO> Overrides = new();
    }
}
