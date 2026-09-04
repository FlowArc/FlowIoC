#if UNITY_EDITOR

namespace FlowIoC.Editor.AgentRules
{
    /// <summary>
    /// Whether FlowIoC keeps one kind of agent asset up to date on its own, per project. The rules
    /// and the skills each carry one, and they are separate: a project may want the rule block in
    /// AGENTS.md and no skill folders under .claude.
    ///
    /// The interface exists so the Agent Scanner can draw both switches with one method rather
    /// than one per kind. It lives here beside SyncFileState for the same reason that does: the
    /// rules came first, and the skills reuse what the rules already had.
    /// </summary>
    internal interface IAutoSyncSwitch
    {
        bool IsOff(string projectRoot);

        void TurnOff(string projectRoot);

        void TurnOn(string projectRoot);
    }
}

#endif
