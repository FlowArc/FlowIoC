#if UNITY_EDITOR
using FlowIoC.BaseModule.Root;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Marks the thing that actually holds a Root's data, which is one of three places: the prefab
    /// stage it is open in, the prefab asset behind the instance it is part of, or the scene it
    /// sits in. Both the Root inspector and the Screens panel write to a Root, so neither owns
    /// this.
    /// </summary>
    internal class RootDirtyMarker
    {
        internal void Mark(RootBase root)
        {
            if (root == null)
                return;

            // The stage this Root is in, not whichever stage happens to be open: a Root edited in
            // the main scene while a prefab stage is open belongs to the scene.
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(root.gameObject);
            if (stage != null)
            {
                EditorSceneManager.MarkSceneDirty(stage.scene);
                return;
            }

            if (PrefabUtility.IsOutermostPrefabInstanceRoot(root.gameObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(root);
                return;
            }

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }
    }
}
#endif
