#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The half-sentence a folder row in the Create Module preview carries beside its name, for the
    /// folders whose name alone does not say what they are for.
    ///
    /// Two do, and they are the same two: a module has a `Signals` folder under `Scripts` and
    /// another under `Scripts/Runtime`, and read as names they are indistinguishable. They are not
    /// the same thing at all - one is the module's public surface and the other is what the module
    /// says to its own commands - and the folders share a name on purpose, because the namespace
    /// segment is the folder name and sharing it is what lets one `using` reach both holders.
    ///
    /// So the answer is not to rename either folder. It is to say, on the row, which is which.
    ///
    /// Every other folder is left without a hint. A row that explains itself does not need one, and
    /// a preview where every line carries prose is a preview nobody reads.
    /// </summary>
    internal class FolderPreviewHints
    {
        private static readonly Dictionary<FolderEVO.FolderType, string> Hints =
            new Dictionary<FolderEVO.FolderType, string>
            {
                {FolderEVO.FolderType.PublicSignals, "the module's public surface"},
                {FolderEVO.FolderType.Signals, "what the module says to itself"}
            };

        /// <summary>The hint for this folder, or null when the folder's name is answer enough.</summary>
        internal string For(FolderEVO folder)
        {
            if (folder == null) return null;

            return Hints.TryGetValue(folder.Type, out string hint) ? hint : null;
        }
    }
}

#endif
