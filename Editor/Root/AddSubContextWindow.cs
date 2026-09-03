#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.Inspector;
using FlowIoC.ScreenModule.RootsContexts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlowIoC.Editor.Root
{
    public class AddSubContextWindow : EditorWindow
    {
        private string searchText = "";
        private Vector2 scrollPosition;

        private RootBase _root;
        private FlowRole _rootRole;
        private List<Type> _contextTypeList;

        public static void ShowWindow(RootBase root, FlowRole rootRole)
        {
            if (root == null)
            {
                Debug.LogError("Cannot open AddSubContextWindow: root is null");
                return;
            }

            var window = GetWindow<AddSubContextWindow>("Add Sub Context");
            window._root = root;
            window._rootRole = rootRole;
            window.LoadContextTypes();
        }

        private void LoadContextTypes()
        {
            try
            {
                var types = AssemblyHelper.GetAllTypesFromAssemblies();

                if (types == null || types.Count == 0)
                {
                    Debug.LogWarning("No types found from assemblies");
                    _contextTypeList = new List<Type>();
                    return;
                }

                if (_root == null)
                {
                    Debug.LogError("Root reference is null in AddSubContextWindow");
                    _contextTypeList = new List<Type>();
                    return;
                }

                HashSet<Type> rootOwned = new RootOwnedContextTypes().Collect();
                var roles = new FlowRoleResolver();

                _contextTypeList = types
                    .Where(x => typeof(IContext).IsAssignableFrom(x))
                    .Where(IsNotABaseContext)
                    .Where(x => !rootOwned.Contains(x) ||
                                x.GetCustomAttribute<AllowAsSubContextAttribute>() != null)
                    .Where(x => _root.SubContextTypes?.All(a => a.ContextFullName != x.FullName) ?? true)
                    .Where(x => x.GetCustomAttribute<ExcludeFromContextWindowAttribute>() == null)
                    .Where(x => IsConnector(roles, x) == (_rootRole == FlowRole.Connector))
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading context types: {ex.Message}\n{ex.StackTrace}");
                _contextTypeList = new List<Type>();
            }
        }

        /// <summary>
        /// The two contexts a module derives from rather than declares. They are concrete and
        /// public, so nothing else would keep them out of a list of things to add.
        /// </summary>
        private bool IsNotABaseContext(Type type) =>
            type != typeof(Context) && type != typeof(BaseScreenContext);

        /// <summary>
        /// Whether a context is a Connector's. A Connector sub-context is offered on the Connector
        /// Root and nowhere else, and every other Root is offered everything but those: the wiring
        /// between two modules lives in one place, and the list says so before the reader has to
        /// know it.
        /// </summary>
        private bool IsConnector(FlowRoleResolver roles, Type type) =>
            roles.TryResolve(type, out FlowRole role) && role == FlowRole.Connector;

        private void OnGUI()
        {
            GUILayout.Label("Search Context:", EditorStyles.boldLabel);
            var newSearchText = GUILayout.TextField(searchText, "ToolbarSearchTextField");
            if (GUILayout.Button("", searchText != "" ? "ToolbarSearchCancelButton" : "ToolbarSearchCancelButtonEmpty"))
            {
                newSearchText = "";
                GUI.FocusControl(null);
            }

            if (newSearchText != null && newSearchText != searchText)
            {
                searchText = newSearchText;
            }

            if (_contextTypeList == null)
            {
                GUILayout.Label("No context types found. Please check if assemblies are loaded correctly.", EditorStyles.boldLabel);
                return;
            }

            IEnumerable<Type> filteredList = _contextTypeList;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredList = _contextTypeList
                    .Where(x => x.Name.ToLowerInvariant().Contains(searchText.ToLowerInvariant()));
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            foreach (var type in filteredList)
            {
                if (GUILayout.Button(type.Name))
                {
                    _root.SubContextTypes.Add(new SubContextData
                    {
                        ContextFullName = type.FullName,
                        ContextName = type.Name
                    });

                    MarkDirty();
                    Close();
                }
            }

            GUILayout.EndScrollView();
        }

        private void MarkDirty()
        {
            var prefabScene = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabScene != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabScene.scene);
            }
            else if (PrefabUtility.IsOutermostPrefabInstanceRoot(_root.gameObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(_root);
            }
            else
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif