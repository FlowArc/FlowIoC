#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FlowIoC.Editor.Config.ScreenConfigEditor
{
    /// <summary>
    /// Catalogue of every <see cref="CD_Screen"/> asset in the project, edited in place.
    /// Each cell binds straight to the asset's SerializedProperty, so Undo, the dirty flag
    /// and OnValidate all come from Unity rather than from this window.
    /// </summary>
    public class ScreenConfigManagerWindow : EditorWindow
    {
        [MenuItem("Tools/FlowIoC/Screen Config Manager", false, 100)]
        public static void ShowWindow()
        {
            ScreenConfigManagerWindow window = GetWindow<ScreenConfigManagerWindow>("Screen Config Manager");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        private const string PACKAGE_NAME = "com.flowarc.flowioc.core";
        private const string UI_FOLDER = "Editor/Config/ScreenConfigEditor";
        private const string UXML_FILE = "ScreenConfigManager.uxml";
        private const string USS_FILE = "ScreenConfigManager.uss";

        private const string PROP_LAYER = "_defaultLayer";
        private const string PROP_LOAD_TYPE = "_loadType";
        private const string PROP_TAG = "_screenTag";
        private const string PROP_RESOURCE_PATH = "_resourcePath";
        private const string PROP_ADDRESSABLE_KEY = "_addressableKey";
        private const string PROP_DIRECT_PREFAB = "_directPrefab";
        private const string PROP_SHOW_ANIMATION = "_hasShowAnimation";
        private const string PROP_HIDE_ANIMATION = "_hasHideAnimation";
        private const string PROP_VIEW_TYPE = "_viewTypeName";
        private const string PROP_MEDIATOR_TYPE = "_mediatorTypeName";

        private const string COLUMN_NAME = "name";
        private const string COLUMN_LAYER = "layer";
        private const string COLUMN_LOAD_TYPE = "loadType";
        private const string COLUMN_TAG = "tag";
        private const string COLUMN_PATH = "path";
        private const string COLUMN_KEY = "key";
        private const string COLUMN_PREFAB = "prefab";
        private const string COLUMN_SHOW = "show";
        private const string COLUMN_HIDE = "hide";
        private const string COLUMN_VIEW = "view";
        private const string COLUMN_MEDIATOR = "mediator";

        private const string PREF_PREFIX = "FlowIoC.ScreenConfigManager.";
        private const string ALL = "All";

        private readonly List<CD_Screen> _allConfigs = new List<CD_Screen>();
        private readonly List<CD_Screen> _filteredConfigs = new List<CD_Screen>();

        private readonly Dictionary<CD_Screen, SerializedObject> _serializedObjects =
            new Dictionary<CD_Screen, SerializedObject>();

        private MultiColumnListView _table;
        private VisualElement _emptyState;
        private Label _emptyLabel;
        private Label _countLabel;
        private ToolbarSearchField _searchField;
        private DropdownField _loadTypeFilter;
        private DropdownField _tagFilter;
        private DropdownField _layerFilter;

        private string _sortColumn = COLUMN_NAME;
        private bool _sortAscending = true;

        private void CreateGUI()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResolveUiPath(UXML_FILE));
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ResolveUiPath(USS_FILE));

            if (tree == null)
            {
                rootVisualElement.Add(new HelpBox(
                    $"Could not load {UXML_FILE}. The FlowIoC package looks incomplete.",
                    HelpBoxMessageType.Error));
                return;
            }

            tree.CloneTree(rootVisualElement);

            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            QueryElements();
            BuildFilters();
            BuildColumns();
            LoadPreferences();

            EditorApplication.projectChanged += Reload;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            Reload();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= Reload;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            SavePreferences();
            DisposeSerializedObjects();
        }

        private static string ResolveUiPath(string fileName)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ScreenConfigManagerWindow).Assembly);

            string root = packageInfo != null ? packageInfo.assetPath : $"Packages/{PACKAGE_NAME}";
            return $"{root}/{UI_FOLDER}/{fileName}";
        }

        #region Construction

        private void QueryElements()
        {
            _table = rootVisualElement.Q<MultiColumnListView>("table");
            _emptyState = rootVisualElement.Q<VisualElement>("empty");
            _emptyLabel = rootVisualElement.Q<Label>("empty-label");
            _countLabel = rootVisualElement.Q<Label>("count");
            _searchField = rootVisualElement.Q<ToolbarSearchField>("search");

            _searchField.RegisterValueChangedCallback(_ => ApplyFilters());

            rootVisualElement.Q<ToolbarButton>("refresh").clicked += Reload;

            _table.itemsSource = _filteredConfigs;
            _table.sortingMode = ColumnSortingMode.Custom;
            _table.columnSortingChanged += OnColumnSortingChanged;
            _table.selectionChanged += OnSelectionChanged;
            _table.itemsChosen += OnItemsChosen;
        }

        private void BuildFilters()
        {
            VisualElement container = rootVisualElement.Q<VisualElement>("filters");
            container.Clear();

            _loadTypeFilter = new DropdownField("Load")
            {
                choices = BuildEnumChoices<ScreenLoadType>(),
                value = ALL,
                tooltip = "Only show configs loaded this way."
            };

            _tagFilter = new DropdownField("Tag")
            {
                choices = BuildEnumChoices<ScreenTag>(),
                value = ALL,
                tooltip = "Only show configs carrying this tag."
            };

            _layerFilter = new DropdownField("Layer")
            {
                choices = new List<string> {ALL},
                value = ALL,
                tooltip = "Only show configs opening on this default layer."
            };

            _loadTypeFilter.RegisterValueChangedCallback(_ => ApplyFilters());
            _tagFilter.RegisterValueChangedCallback(_ => ApplyFilters());
            _layerFilter.RegisterValueChangedCallback(_ => ApplyFilters());

            // The layer choices are the layers the assets actually use, and editing a Layer
            // cell changes that set. Rebuild them as the dropdown is about to open rather
            // than watching every cell for a change.
            _layerFilter.RegisterCallback<PointerDownEvent>(_ => RebuildLayerChoices(), TrickleDown.TrickleDown);

            container.Add(_loadTypeFilter);
            container.Add(_tagFilter);
            container.Add(_layerFilter);
        }

        private static List<string> BuildEnumChoices<T>() where T : Enum
        {
            List<string> choices = new List<string> {ALL};
            choices.AddRange(Enum.GetNames(typeof(T)));
            return choices;
        }

        private void BuildColumns()
        {
            Columns columns = _table.columns;
            columns.Clear();

            columns.Add(new Column
            {
                name = COLUMN_NAME,
                title = "Name",
                width = 260,
                minWidth = 120,
                sortable = true,
                makeCell = MakeNameCell,
                bindCell = BindNameCell,
                unbindCell = UnbindCell
            });

            columns.Add(new Column
            {
                name = COLUMN_LAYER,
                title = "Layer",
                width = 56,
                minWidth = 44,
                optional = true,
                sortable = true,
                makeCell = () => new IntegerField {isDelayed = true},
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_LAYER),
                unbindCell = UnbindCell
            });

            columns.Add(new Column
            {
                name = COLUMN_LOAD_TYPE,
                title = "Load Type",
                width = 110,
                minWidth = 80,
                optional = true,
                sortable = true,
                makeCell = () => new EnumField(ScreenLoadType.Addressable),
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_LOAD_TYPE),
                unbindCell = UnbindCell
            });

            columns.Add(new Column
            {
                name = COLUMN_TAG,
                title = "Tag",
                width = 100,
                minWidth = 70,
                optional = true,
                sortable = true,
                makeCell = () => new EnumField(ScreenTag.Default),
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_TAG),
                unbindCell = UnbindCell
            });

            // One column per source field rather than one that follows the load type: a
            // config's path can be filled in before its load type is switched over, and
            // each column hides on its own from the header's context menu.
            columns.Add(new Column
            {
                name = COLUMN_PATH,
                title = "Resource Path",
                width = 200,
                minWidth = 100,
                optional = true,
                sortable = true,
                makeCell = () => new TextField {isDelayed = true},
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_RESOURCE_PATH),
                unbindCell = UnbindCell
            });

            columns.Add(new Column
            {
                name = COLUMN_KEY,
                title = "Addressable Key",
                width = 200,
                minWidth = 100,
                optional = true,
                sortable = true,
                makeCell = () => new TextField {isDelayed = true},
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_ADDRESSABLE_KEY),
                unbindCell = UnbindCell
            });

            columns.Add(new Column
            {
                name = COLUMN_PREFAB,
                title = "Direct Prefab",
                width = 170,
                minWidth = 100,
                optional = true,
                sortable = true,
                makeCell = () => new ObjectField {objectType = typeof(GameObject), allowSceneObjects = false},
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_DIRECT_PREFAB),
                unbindCell = UnbindCell
            });

            columns.Add(new Column
            {
                name = COLUMN_SHOW,
                title = "Show",
                width = 46,
                minWidth = 40,
                optional = true,
                sortable = true,
                makeCell = MakeCenteredToggle,
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_SHOW_ANIMATION),
                unbindCell = UnbindCell
            });

            columns.Add(new Column
            {
                name = COLUMN_HIDE,
                title = "Hide",
                width = 46,
                minWidth = 40,
                optional = true,
                sortable = true,
                makeCell = MakeCenteredToggle,
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_HIDE_ANIMATION),
                unbindCell = UnbindCell
            });

            columns.Add(new Column
            {
                name = COLUMN_VIEW,
                title = "View Type",
                width = 170,
                minWidth = 100,
                optional = true,
                visible = false,
                sortable = true,
                makeCell = () => new TextField {isDelayed = true},
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_VIEW_TYPE),
                unbindCell = UnbindCell
            });

            columns.Add(new Column
            {
                name = COLUMN_MEDIATOR,
                title = "Mediator Type",
                width = 170,
                minWidth = 100,
                optional = true,
                visible = false,
                sortable = true,
                makeCell = () => new TextField {isDelayed = true},
                bindCell = (cell, index) => BindPropertyCell(cell, index, PROP_MEDIATOR_TYPE),
                unbindCell = UnbindCell
            });
        }

        #endregion

        #region Cells

        private VisualElement MakeNameCell()
        {
            Label label = new Label {tooltip = "Right click for asset actions."};
            label.AddToClassList("scm-cell__label");
            label.AddManipulator(new ContextualMenuManipulator(evt => BuildRowMenu(evt, label)));
            return label;
        }

        private void BindNameCell(VisualElement cell, int index)
        {
            Label label = (Label) cell;
            CD_Screen config = GetConfigAt(index);

            label.userData = index;
            label.text = config != null ? config.name : string.Empty;
        }

        private static VisualElement MakeCenteredToggle()
        {
            Toggle toggle = new Toggle();
            toggle.AddToClassList("scm-cell--center");
            return toggle;
        }

        private void BindPropertyCell(VisualElement cell, int index, string propertyName)
        {
            SerializedObject serializedObject = GetSerializedObject(GetConfigAt(index));
            SerializedProperty property = serializedObject?.FindProperty(propertyName);

            if (property == null || cell is not IBindable bindable)
            {
                return;
            }

            cell.userData = index;
            bindable.BindProperty(property);
        }

        private static void UnbindCell(VisualElement cell, int index)
        {
            cell.Unbind();
            cell.userData = null;
        }

        private void BuildRowMenu(ContextualMenuPopulateEvent evt, VisualElement cell)
        {
            CD_Screen config = GetConfigAt(cell.userData);
            if (config == null)
            {
                return;
            }

            evt.menu.AppendAction("Ping in Project", _ => EditorGUIUtility.PingObject(config));
            evt.menu.AppendAction("Select Asset", _ => Selection.activeObject = config);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", _ => DeleteConfig(config));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Show in Explorer",
                _ => EditorUtility.RevealInFinder(AssetDatabase.GetAssetPath(config)));
        }

        #endregion

        #region Data

        private void Reload()
        {
            _allConfigs.Clear();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(CD_Screen)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CD_Screen config = AssetDatabase.LoadAssetAtPath<CD_Screen>(path);

                if (config != null)
                {
                    _allConfigs.Add(config);
                }
            }

            PruneSerializedObjects();
            RebuildLayerChoices();
            ApplyFilters();
        }

        private void RebuildLayerChoices()
        {
            List<string> choices = new List<string> {ALL};
            choices.AddRange(_allConfigs
                .Select(config => config.DefaultLayer)
                .Distinct()
                .OrderBy(layer => layer)
                .Select(layer => layer.ToString()));

            string previous = _layerFilter.value;
            _layerFilter.choices = choices;
            _layerFilter.SetValueWithoutNotify(choices.Contains(previous) ? previous : ALL);
        }

        private void ApplyFilters()
        {
            string search = _searchField.value;

            _filteredConfigs.Clear();

            foreach (CD_Screen config in _allConfigs)
            {
                if (!MatchesSearch(config, search))
                {
                    continue;
                }

                if (_loadTypeFilter.value != ALL && config.LoadType.ToString() != _loadTypeFilter.value)
                {
                    continue;
                }

                if (_tagFilter.value != ALL && config.Tag.ToString() != _tagFilter.value)
                {
                    continue;
                }

                if (_layerFilter.value != ALL && config.DefaultLayer.ToString() != _layerFilter.value)
                {
                    continue;
                }

                _filteredConfigs.Add(config);
            }

            SortFilteredConfigs();
            RefreshTable();
        }

        private static bool MatchesSearch(CD_Screen config, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            return Contains(config.name, search)
                   || Contains(config.ResourcePath, search)
                   || Contains(config.AddressableKey, search)
                   || Contains(GetPrefabName(config), search);
        }

        private static bool Contains(string value, string search)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetPrefabName(CD_Screen config)
        {
            return config.DirectPrefab != null ? config.DirectPrefab.name : string.Empty;
        }

        private void SortFilteredConfigs()
        {
            Comparison<CD_Screen> comparison = GetComparison(_sortColumn);

            _filteredConfigs.Sort((left, right) => _sortAscending
                ? comparison(left, right)
                : comparison(right, left));
        }

        private static Comparison<CD_Screen> GetComparison(string columnName)
        {
            switch (columnName)
            {
                case COLUMN_LAYER:
                    return (left, right) => left.DefaultLayer.CompareTo(right.DefaultLayer);

                case COLUMN_LOAD_TYPE:
                    return (left, right) => left.LoadType.CompareTo(right.LoadType);

                case COLUMN_TAG:
                    return (left, right) => left.Tag.CompareTo(right.Tag);

                case COLUMN_PATH:
                    return (left, right) => string.Compare(left.ResourcePath, right.ResourcePath,
                        StringComparison.OrdinalIgnoreCase);

                case COLUMN_KEY:
                    return (left, right) => string.Compare(left.AddressableKey, right.AddressableKey,
                        StringComparison.OrdinalIgnoreCase);

                case COLUMN_PREFAB:
                    return (left, right) => string.Compare(GetPrefabName(left), GetPrefabName(right),
                        StringComparison.OrdinalIgnoreCase);

                case COLUMN_SHOW:
                    return (left, right) => left.HasShowAnimation.CompareTo(right.HasShowAnimation);

                case COLUMN_HIDE:
                    return (left, right) => left.HasHideAnimation.CompareTo(right.HasHideAnimation);

                case COLUMN_VIEW:
                    return (left, right) => string.Compare(left.ViewTypeName, right.ViewTypeName,
                        StringComparison.OrdinalIgnoreCase);

                case COLUMN_MEDIATOR:
                    return (left, right) => string.Compare(left.MediatorTypeName, right.MediatorTypeName,
                        StringComparison.OrdinalIgnoreCase);

                default:
                    return (left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
            }
        }

        private CD_Screen GetConfigAt(object index)
        {
            return index is int rowIndex ? GetConfigAt(rowIndex) : null;
        }

        private CD_Screen GetConfigAt(int index)
        {
            return index >= 0 && index < _filteredConfigs.Count ? _filteredConfigs[index] : null;
        }

        private SerializedObject GetSerializedObject(CD_Screen config)
        {
            if (config == null)
            {
                return null;
            }

            if (_serializedObjects.TryGetValue(config, out SerializedObject cached) && cached.targetObject != null)
            {
                return cached;
            }

            SerializedObject created = new SerializedObject(config);
            _serializedObjects[config] = created;
            return created;
        }

        private void PruneSerializedObjects()
        {
            List<CD_Screen> dead = _serializedObjects.Keys
                .Where(config => config == null || !_allConfigs.Contains(config))
                .ToList();

            foreach (CD_Screen config in dead)
            {
                _serializedObjects[config]?.Dispose();
                _serializedObjects.Remove(config);
            }
        }

        private void DisposeSerializedObjects()
        {
            foreach (SerializedObject serializedObject in _serializedObjects.Values)
            {
                serializedObject?.Dispose();
            }

            _serializedObjects.Clear();
        }

        #endregion

        #region View state

        private void RefreshTable()
        {
            bool hasRows = _filteredConfigs.Count > 0;

            _table.style.display = hasRows ? DisplayStyle.Flex : DisplayStyle.None;
            _emptyState.style.display = hasRows ? DisplayStyle.None : DisplayStyle.Flex;

            _emptyLabel.text = _allConfigs.Count == 0
                ? "No screen config assets in this project yet. Create Module writes one for every screen module."
                : "No screen config matches the current filters.";

            _countLabel.text = $"{_filteredConfigs.Count} / {_allConfigs.Count}";

            _table.RefreshItems();
        }

        /// <summary>
        /// Bound cells pick up an undone value on their own, but the layer filter's choices
        /// and the sort order are built from the values, so they have to be rebuilt too.
        /// </summary>
        private void OnUndoRedoPerformed()
        {
            RebuildLayerChoices();
            ApplyFilters();
        }

        private void OnColumnSortingChanged()
        {
            SortColumnDescription sorted = _table.sortedColumns.FirstOrDefault();

            if (sorted != null)
            {
                _sortColumn = sorted.columnName;
                _sortAscending = sorted.direction == SortDirection.Ascending;
            }

            SortFilteredConfigs();
            _table.RefreshItems();
        }

        private static void OnSelectionChanged(IEnumerable<object> items)
        {
            if (items.FirstOrDefault() is CD_Screen config)
            {
                EditorGUIUtility.PingObject(config);
            }
        }

        private static void OnItemsChosen(IEnumerable<object> items)
        {
            if (items.FirstOrDefault() is CD_Screen config)
            {
                Selection.activeObject = config;
            }
        }

        #endregion

        #region Actions

        private void DeleteConfig(CD_Screen config)
        {
            string path = AssetDatabase.GetAssetPath(config);

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Screen Config",
                $"Delete {config.name}?\n\n{path}\n\n"
                + "Anything still referencing it - a Screen Config Adapter in a scene, a prefab - loses that reference.",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            AssetDatabase.DeleteAsset(path);
            Reload();
        }

        #endregion

        #region Preferences

        private void SavePreferences()
        {
            if (_table == null)
            {
                return;
            }

            EditorPrefs.SetString($"{PREF_PREFIX}Search", _searchField.value);
            EditorPrefs.SetString($"{PREF_PREFIX}Filter.LoadType", _loadTypeFilter.value);
            EditorPrefs.SetString($"{PREF_PREFIX}Filter.Tag", _tagFilter.value);
            EditorPrefs.SetString($"{PREF_PREFIX}Filter.Layer", _layerFilter.value);
            EditorPrefs.SetString($"{PREF_PREFIX}Sort.Column", _sortColumn);
            EditorPrefs.SetBool($"{PREF_PREFIX}Sort.Ascending", _sortAscending);

            foreach (Column column in _table.columns)
            {
                EditorPrefs.SetFloat($"{PREF_PREFIX}Column.{column.name}.Width", column.width.value);
                EditorPrefs.SetBool($"{PREF_PREFIX}Column.{column.name}.Visible", column.visible);
            }
        }

        private void LoadPreferences()
        {
            _searchField.SetValueWithoutNotify(EditorPrefs.GetString($"{PREF_PREFIX}Search", string.Empty));

            RestoreDropdown(_loadTypeFilter, $"{PREF_PREFIX}Filter.LoadType");
            RestoreDropdown(_tagFilter, $"{PREF_PREFIX}Filter.Tag");

            _sortColumn = EditorPrefs.GetString($"{PREF_PREFIX}Sort.Column", COLUMN_NAME);
            _sortAscending = EditorPrefs.GetBool($"{PREF_PREFIX}Sort.Ascending", true);

            foreach (Column column in _table.columns)
            {
                string widthKey = $"{PREF_PREFIX}Column.{column.name}.Width";
                string visibleKey = $"{PREF_PREFIX}Column.{column.name}.Visible";

                if (column.resizable && EditorPrefs.HasKey(widthKey))
                {
                    column.width = EditorPrefs.GetFloat(widthKey);
                }

                if (column.optional && EditorPrefs.HasKey(visibleKey))
                {
                    column.visible = EditorPrefs.GetBool(visibleKey);
                }
            }
        }

        private static void RestoreDropdown(DropdownField dropdown, string key)
        {
            string stored = EditorPrefs.GetString(key, ALL);
            dropdown.SetValueWithoutNotify(dropdown.choices.Contains(stored) ? stored : ALL);
        }

        #endregion
    }
}

#endif