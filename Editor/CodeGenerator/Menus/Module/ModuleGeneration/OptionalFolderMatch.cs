#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The optional folders the Create Module window ticked, as entries of the layout a run writes
    /// from. Folder creation finds a ticked folder by reference, and a test module's layout is a
    /// copy with its asset folders moved under Editor/ - so each tick is found again in it by type
    /// and name. A tick the layout has no folder for is dropped, which is what folder creation did
    /// with it anyway.
    /// </summary>
    internal class OptionalFolderMatch
    {
        internal List<FolderEVO> In(List<FolderEVO> ticked, DirectoryStructureConfig layout)
        {
            var matched = new List<FolderEVO>();

            if (ticked == null || layout?.RootFolders == null)
                return matched;

            List<FolderEVO> all = Flatten(layout.RootFolders).ToList();

            foreach (FolderEVO tick in ticked.Where(folder => folder != null))
            {
                FolderEVO found = all.FirstOrDefault(folder => ReferenceEquals(folder, tick))
                                  ?? all.FirstOrDefault(folder => folder.Type == tick.Type && folder.FolderName == tick.FolderName);

                if (found != null && !matched.Contains(found))
                    matched.Add(found);
            }

            return matched;
        }

        private IEnumerable<FolderEVO> Flatten(IEnumerable<FolderEVO> folders)
        {
            foreach (FolderEVO folder in folders.Where(folder => folder != null))
            {
                yield return folder;

                if (folder.SubFolders == null) continue;

                foreach (FolderEVO child in Flatten(folder.SubFolders))
                    yield return child;
            }
        }
    }
}
#endif
