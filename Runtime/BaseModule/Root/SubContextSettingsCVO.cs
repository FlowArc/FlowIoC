using System;

namespace FlowIoC.BaseModule.Root
{
    /// <summary>
    /// What a Root entry says about the one sub-context it lists - a screen's layer, the pool
    /// groups a module registers. Each kind of configurable sub-context derives its own, so an
    /// entry carries only the fields its context reads rather than every kind's fields side by
    /// side. A CVO, because it is authored in the Editor and constant at runtime.
    ///
    /// Held by reference on the entry. The inspector never changes one in place: an edit builds a
    /// new instance and writes the entry back, so Undo records what was there before and two
    /// entries never share one object.
    /// </summary>
    [Serializable]
    public abstract class SubContextSettingsCVO
    {
    }
}
