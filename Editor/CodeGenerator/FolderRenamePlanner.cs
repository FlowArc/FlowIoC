#if UNITY_EDITOR
using System;
using System.IO;

namespace FlowIoC.Editor.CodeGenerator
{
    /// <summary>
    /// Whether a folder already carries its configured name and, if not, the sibling path it
    /// should be renamed to. Plain string work with no AssetDatabase or disk access in it, which
    /// is what makes the rename decision itself testable on its own rather than only through the
    /// tool that calls it.
    /// </summary>
    internal class FolderRenamePlanner
    {
        public bool TryPlanRename(string currentAbsolutePath, string configuredName, out string newAbsolutePath)
        {
            newAbsolutePath = null;
            if (string.IsNullOrEmpty(currentAbsolutePath) || string.IsNullOrEmpty(configuredName))
                return false;

            string trimmed = currentAbsolutePath.TrimEnd('/', '\\');
            string currentName = Path.GetFileName(trimmed);
            if (string.Equals(currentName, configuredName, StringComparison.OrdinalIgnoreCase))
                return false;

            string parentPath = Path.GetDirectoryName(trimmed);
            newAbsolutePath = string.IsNullOrEmpty(parentPath)
                ? configuredName
                : Path.Combine(parentPath, configuredName);
            return true;
        }
    }
}
#endif
