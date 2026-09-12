#if UNITY_EDITOR

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// One sub-context entry Rename Module rewrote, and where. It carries the Root and the asset
    /// because the reader's question afterwards is which scene to save, not how many were touched.
    /// </summary>
    internal class SubContextRenameEVO
    {
        internal string AssetPath { get; set; }
        internal string RootName { get; set; }
        internal string OldName { get; set; }
        internal string NewName { get; set; }

        /// <summary>The entry was changed in a scene that was open, and the scene was left for its owner to save.</summary>
        internal bool SceneLeftUnsaved { get; set; }

        internal string Line()
        {
            string line = RootName + " in " + AssetPath + ": " + OldName + " → " + NewName;

            return SceneLeftUnsaved ? line + " - scene not saved" : line;
        }
    }
}

#endif
