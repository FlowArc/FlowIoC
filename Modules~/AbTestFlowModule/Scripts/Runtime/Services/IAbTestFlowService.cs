namespace Modules.AbTestFlowModule.Services
{
    /// <summary>
    /// The module's one counterpart. Injecting this interface is the sanctioned cross-module
    /// reference: being usable that way is what a Service is for, and it is the only thing the
    /// module binds cross-context - whatever else needs doing from outside goes through a method
    /// here, and the service reaches the model.
    /// </summary>
    public interface IAbTestFlowService
    {
        /// <summary>The group this player landed in, or null when they are outside the test.</summary>
        string GetGroup(string abTestId);

        bool IsInGroup(string abTestId, string groupName);

#if UNITY_EDITOR
        /// <summary>
        /// Puts the config assets back to what they were before a variant was written over them, so
        /// a play session leaves no diff behind. The Root calls it on quit.
        /// </summary>
        void RestoreEditorAssets();
#endif
    }
}