#if UNITY_EDITOR

using Modules.AbTestFlowModule.AbTestFlowTestModule.Data.UnityObjects;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Models
{
    public interface IAbTestFlowTestModel
    {
        /// <summary>The one experiment the scene runs, as CD_AbTests names it.</summary>
        string AbTestId { get; }

        int Version { get; }

        CD_AbTestProbe Probe { get; }

        void RaiseVersion();

        /// <summary>
        /// Puts CD_AbTests back to what it was before RaiseVersion touched it, so the play session
        /// leaves the module's own config with no diff either. A raise therefore lasts one session:
        /// the next launch reads the version on disk, and an assignment stored under the raised one
        /// is rolled again.
        /// </summary>
        void RestoreEditorAssets();
    }
}

#endif