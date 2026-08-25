#if UNITY_EDITOR
using FlowIoC.Editor.Modules;
using UnityEditor;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    /// <summary>
    /// The deletion-side counterpart to ModuleIndexRegistrar. The deleted module's own entry is
    /// removed immediately so the index is correct even if the rebuild that follows cannot run
    /// (CodeGeneratorSettings missing); the rebuild that always follows then drops any nested
    /// children too, since a rescan simply never finds folders that are gone.
    /// </summary>
    internal class ModuleIndexDeregistrar
    {
        public void Deregister(string folderGuid)
        {
            if (string.IsNullOrEmpty(folderGuid)) return;

            FlowIoCModuleIndex index = new ModuleIndexProvider().LoadOrCreate();
            index.Remove(folderGuid);

            EditorUtility.SetDirty(index);
            AssetDatabase.SaveAssets();

            new ModuleIndexRebuilder().Rebuild();
        }
    }
}
#endif
