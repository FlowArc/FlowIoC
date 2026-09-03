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

        private SceneSubContextUsage _usage;
        private ScreenSubContextDeclarations _declarations;
        private FlowRoleResolver _roles;
        private FlowPalette _palette;
        private GUIStyle _nameStyle;
        private GUIStyle _usageStyle;

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

                _roles = new FlowRoleResolver();
                _palette = new FlowPalette();
                _declarations = new ScreenSubContextDeclarations();
                _usage = new SceneSubContextUsage(_root);

                // A context another Root already lists is offered last: it is a legitimate thing to
                // add twice, and a rare enough one that it should not sit among the ordinary
                // choices. Alphabetical inside each half, because assembly order is no order at all
                // to the reader.
                _contextTypeList = types
                    .Where(x => typeof(IContext).IsAssignableFrom(x))
                    .Where(IsNotABaseContext)
                    .Where(x => !rootOwned.Contains(x) ||
                                x.GetCustomAttribute<AllowAsSubContextAttribute>() != null)
                    .Where(x => _root.SubContextTypes?.All(a => a.ContextFullName != x.FullName) ?? true)
                    .Where(x => x.GetCustomAttribute<ExcludeFromContextWindowAttribute>() == null)
                    .Where(x => IsConnector(_roles, x) == (_rootRole == FlowRole.Connector))
                    .OrderBy(x => _usage.UsedBy(x.FullName) == null ? 0 : 1)
                    .ThenBy(x => x.Name)
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

            EnsureStyles();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            bool separatorDrawn = false;

            foreach (var type in filteredList)
            {
                string usedBy = _usage?.UsedBy(type.FullName);

                if (usedBy != null && !separatorDrawn)
                {
                    separatorDrawn = true;
                    GUILayout.Space(6f);
                    GUILayout.Label("Already on another Root", EditorStyles.miniBoldLabel);
                }

                if (Row(type, usedBy))
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

        /// <summary>
        /// One row: the context's name, what kind it is, and the Roots that already list it. The
        /// whole row is the button, and the three labels are drawn over it, so a click anywhere
        /// adds the context however much the row has to say.
        /// </summary>
        private bool Row(Type type, string usedBy)
        {
            Rect row = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button, GUILayout.ExpandWidth(true));

            bool clicked = GUI.Button(row, GUIContent.none);

            KindOf(type, out string badge, out Color badgeColor);

            float usageWidth = usedBy == null ? 0f : Mathf.Min(180f, row.width * 0.4f);
            float badgeWidth = badge == null ? 0f : 74f;

            var nameRect = new Rect(row.x + 6f, row.y, row.width - usageWidth - badgeWidth - 12f, row.height);
            GUI.Label(nameRect, type.Name, _nameStyle);

            if (badge != null)
            {
                Color previous = GUI.color;
                GUI.color = badgeColor;
                GUI.Label(new Rect(row.xMax - usageWidth - badgeWidth, row.y, badgeWidth, row.height),
                    badge, EditorStyles.miniBoldLabel);
                GUI.color = previous;
            }

            if (usedBy != null)
                GUI.Label(new Rect(row.xMax - usageWidth - 6f, row.y, usageWidth, row.height), usedBy, _usageStyle);

            return clicked;
        }

        /// <summary>
        /// The badge a row wears, the same one the Root's own list of sub-contexts shows: a screen
        /// in the screen colour, a connector in the connector's, and nothing at all for a context
        /// that is neither.
        /// </summary>
        private void KindOf(Type type, out string badge, out Color color)
        {
            bool proSkin = EditorGUIUtility.isProSkin;

            if (_declarations != null && _declarations.IsScreenContext(type))
            {
                badge = "SCREEN";
                color = _palette.Accent(FlowRole.Screen, proSkin);
                return;
            }

            if (IsConnector(_roles, type))
            {
                badge = "CONNECTOR";
                color = _palette.Accent(FlowRole.Connector, proSkin);
                return;
            }

            badge = null;
            color = Color.clear;
        }

        private void EnsureStyles()
        {
            _nameStyle ??= new GUIStyle(EditorStyles.label) {alignment = TextAnchor.MiddleLeft};

            _usageStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                clipping = TextClipping.Clip
            };
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