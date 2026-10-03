#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The folder a new module is made in, as the absolute path every step of a run compares
    /// against Application.dataPath. The panel hands that form already; a script is as likely to
    /// hand the folder as Unity names it, Assets/Modules/GameplayModule. That form used to go
    /// through untouched: the folders were made, since the Editor's working directory is the
    /// project root, but every conversion back to an Assets path failed - one "not within the
    /// Assets folder" error per folder, minutes of main thread work, and a module with no scene and
    /// no Root listing. Resolved here against the same working directory, it is the same folder.
    ///
    /// A folder outside Assets is refused before anything is written, because no module can live
    /// there and a partly written one is the thing this is here to prevent.
    /// </summary>
    internal class ModuleParentFolder
    {
        internal bool TryResolve(string parentModulePath, out string resolved, out string refusal)
        {
            resolved = Path.GetFullPath(parentModulePath);
            refusal = null;

            string full = resolved.Replace('\\', '/').TrimEnd('/');
            string assets = Application.dataPath.Replace('\\', '/').TrimEnd('/');

            if (full.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase))
                return true;

            refusal = $"<color=cyan>[FlowIoC]</color> Create Module was handed '{parentModulePath}' as the parent "
                      + "folder, which is not inside Assets. Pass Assets/Modules, a module's folder under it, or "
                      + "either one as an absolute path. Nothing was written.";
            return false;
        }
    }
}
#endif
