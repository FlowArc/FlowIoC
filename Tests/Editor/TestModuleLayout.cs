using System.Collections.Generic;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A directory layout a test writes out by hand. DirectoryStructureConfig is abstract and
    /// keeps RootFolders behind a protected setter, so the only way to describe an arbitrary
    /// folder tree is to derive from it - and describing one is the point: a test about missing
    /// folders should say which folders it means rather than inherit whatever the real main
    /// module layout happens to declare this month.
    /// </summary>
    internal class TestModuleLayout : DirectoryStructureConfig
    {
        internal static TestModuleLayout With(params FolderEVO[] rootFolders)
        {
            var layout = CreateInstance<TestModuleLayout>();
            layout.RootFolders = new List<FolderEVO>(rootFolders);

            return layout;
        }

        internal static FolderEVO Folder(
            string folderName,
            bool isMandatory = false,
            bool isOptional = false,
            bool isNamespaceProvider = true,
            params FolderEVO[] subFolders)
        {
            return new FolderEVO
            {
                FolderName = folderName,
                IsMandatory = isMandatory,
                IsOptional = isOptional,
                IsNamespaceProvider = isNamespaceProvider,
                SubFolders = new List<FolderEVO>(subFolders)
            };
        }
    }
}
