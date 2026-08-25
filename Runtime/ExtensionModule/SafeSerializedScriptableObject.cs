#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlowIoC.ExtensionModule
{
    public class SafeSerializedScriptableObject : SerializedScriptableObject
    {
#if UNITY_EDITOR
#pragma warning disable CS0414
        private bool _requireValidation = false;
#pragma warning restore CS0414

        private void OnValidate()
        {
            _requireValidation = true;
        }

        [ShowIf(nameof(_requireValidation))]
        [GUIColor(1f, .9f, .4f)]
        [PropertyOrder(100)][Button(100)]
        [PropertySpace(SpaceBefore = 20)]
        private void SaveProject()
        {
            _requireValidation = false;
            EditorApplication.ExecuteMenuItem("File/Save Project");
        }
#endif
    }
}
#endif
