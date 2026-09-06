#if UNITY_EDITOR

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    /// <summary>
    /// One sub-context entry that Delete Module found in somebody else's scene or prefab, and what
    /// became of it.
    ///
    /// It carries where as well as what, because the reader's question afterwards is not "how many"
    /// but "which Root in which scene", and a summary that cannot answer that is a count rather
    /// than a report.
    /// </summary>
    internal class SubContextUnwireEVO
    {
        /// <summary>The scene or prefab the Root lives in.</summary>
        internal string AssetPath { get; set; }

        /// <summary>The Root's name in that asset, which is what the reader looks for.</summary>
        internal string RootName { get; set; }

        /// <summary>The context the entry named.</summary>
        internal string ContextName { get; set; }

        internal SubContextUnwireOutcome Outcome { get; set; }

        /// <summary>One line for the console and the summary dialog.</summary>
        internal string Line()
        {
            switch (Outcome)
            {
                case SubContextUnwireOutcome.Removed:
                    return $"Removed {ContextName} from {RootName} in {AssetPath}";

                // Saying only "Removed" here would be a lie by omission: close the scene without
                // saving and the entry is back.
                case SubContextUnwireOutcome.RemovedNotSaved:
                    return $"Removed {ContextName} from {RootName} in {AssetPath} - scene not saved";

                case SubContextUnwireOutcome.Skipped:
                    return $"Left {ContextName} on {RootName} in {AssetPath}";

                // What cancelling leaves is not a thing that happened but a place to look, so it
                // reads as a statement about the Root rather than about an action.
                default:
                    return $"{RootName} in {AssetPath} lists {ContextName}";
            }
        }
    }
}

#endif