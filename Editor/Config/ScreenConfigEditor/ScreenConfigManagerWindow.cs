#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Config.ScreenConfigEditor
{
    public class ScreenConfigManagerWindow : EditorWindow
    {
        [MenuItem("Tools/FlowIoC/Screen Config Manager", false, 100)]
        public static void ShowWindow()
        {
            ScreenConfigManagerWindow window = GetWindow<ScreenConfigManagerWindow>("Screen Config Manager");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        private const float cellPadding = 8f;
        private List<CD_Screen> _allScreenConfigs = new List<CD_Screen>();
        private List<CD_Screen> _filteredScreenConfigs = new List<CD_Screen>();
        private List<CD_Screen> _selectedScreenConfigs = new List<CD_Screen>();
        private List<ScreenTag> _availableTags = new List<ScreenTag>();

        private int _selectedFilterType = 0;
        private string _searchText = "";
        private ScreenTag _selectedTag = ScreenTag.Default;
        private ScreenLoadType _selectedLoadType = ScreenLoadType.Resource;
        private int _selectedLayerIndex = 0;
        private Vector2 _scrollPosition;
        private bool _showFilterOptions = true;
        private bool _showBulkEditOptions = false;

        private int _bulkEditLayerIndex = 0;
        private ScreenLoadType _bulkEditLoadType = ScreenLoadType.Resource;
        private ScreenTag _bulkEditTag = ScreenTag.Default;
        private string _bulkEditResourcePath = "";
        private string _bulkEditAddressableKey = "";
        private bool _bulkEditHasOpenAnimation = false;
        private bool _bulkEditHasCloseAnimation = false;

        private float _nameColumnWidth = 200f;
        private float _layerColumnWidth = 60f;
        private float _loadTypeColumnWidth = 100f;
        private float _tagColumnWidth = 120f;
        private float _pathColumnWidth = 200f;
        private float _animationColumnWidth = 100f;

        private GUIStyle _headerStyle;
        private GUIStyle _centeredBoldLabel;
        private GUIStyle _titleStyle;
        private GUIStyle _rowButtonStyle;
        private GUIStyle _actionButtonStyle;
        private GUIStyle _toolbarBackground;
        private GUIStyle _tableRowStyle;
        private GUIStyle _tableHeaderStyle;
        private GUIStyle _foldoutStyle;
        private GUIStyle _searchFieldStyle;

        private Color _headerColor;
        private Color _alternateRowColor;
        private Color _selectedRowColor;
        private Color _hoverRowColor;
        private Color _titleBackgroundColor;
        private Color _buttonColor;
        private Color _activeButtonColor;

        private Texture2D _headerBackgroundTexture;
        private Texture2D _titleBackgroundTexture;
        private Texture2D _rowBackgroundTexture;
        private Texture2D _alternateRowBackgroundTexture;
        private Texture2D _selectedRowBackgroundTexture;
        private Texture2D _hoverRowBackgroundTexture;
        private Texture2D _buttonBackgroundTexture;
        private Texture2D _activeButtonBackgroundTexture;

        private int _sortColumnIndex = 0;
        private bool _sortAscending = true;

        private Vector2 _bulkEditScrollPosition = Vector2.zero;

        // Hover kontrolü için değişken
        private int _hoveredRowIndex = -1;

        private void OnEnable()
        {
            _headerColor = new Color(0.15f, 0.15f, 0.18f);
            _alternateRowColor = new Color(0.13f, 0.13f, 0.16f);
            _selectedRowColor = new Color(0.2f, 0.4f, 0.8f, 0.2f);
            _hoverRowColor = new Color(0.2f, 0.4f, 0.8f, 0.4f);
            _titleBackgroundColor = new Color(0.1f, 0.1f, 0.1f);
            _buttonColor = new Color(0.2f, 0.2f, 0.25f);
            _activeButtonColor = new Color(0.682f, 0.839f, 0.945f); // HEX #aed6f1

            CreateTextures();
            InitializeStyles();

            FindAllScreenConfigs();
            FindAllTags();

            _filteredScreenConfigs = new List<CD_Screen>(_allScreenConfigs);
            _showFilterOptions = EditorPrefs.GetBool("ScreenConfigManager_ShowFilters", true);
            _showBulkEditOptions = EditorPrefs.GetBool("ScreenConfigManager_ShowEditPanel", false);

            LoadColumnLayout();

            SortConfigs();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            position = new Rect(this.position.x, this.position.y, this.position.width, this.position.height);

            UpdateStyleTextures();

            // Pencereyi yeniden çiz
            Repaint();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            DestroyTexture(_headerBackgroundTexture);
            DestroyTexture(_titleBackgroundTexture);
            DestroyTexture(_rowBackgroundTexture);
            DestroyTexture(_alternateRowBackgroundTexture);
            DestroyTexture(_selectedRowBackgroundTexture);
            DestroyTexture(_hoverRowBackgroundTexture);
            DestroyTexture(_buttonBackgroundTexture);
            DestroyTexture(_activeButtonBackgroundTexture);
        }

        private void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                DestroyImmediate(texture);
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                FindAllScreenConfigs();
                FindAllTags();
                Repaint();
            }
        }

        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                padding = new RectOffset(10, 10, 8, 8),
                normal = {textColor = new Color(0.95f, 0.95f, 1f)}
            };

            _centeredBoldLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = {textColor = new Color(0.9f, 0.9f, 1f)}
            };

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 17,
                padding = new RectOffset(15, 15, 10, 10),
                margin = new RectOffset(0, 0, 0, 0),
                fontStyle = FontStyle.Bold,
                normal = {textColor = new Color(0.9f, 0.9f, 1f)}
            };

            _rowButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 24,
                padding = new RectOffset(10, 10, 6, 6),
                margin = new RectOffset(2, 2, 2, 2),
                fontSize = 12,
                normal = {textColor = new Color(0.85f, 0.85f, 0.9f)},
                hover = {textColor = new Color(1f, 1f, 1f)}
            };

            _actionButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 24,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(2, 2, 2, 2),
                fontSize = 12,
                normal = {textColor = Color.white},
                hover = {textColor = Color.white},
                wordWrap = false,
                stretchWidth = true
            };

            _toolbarBackground = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = 38,
                padding = new RectOffset(12, 12, 8, 8)
            };

            _tableRowStyle = new GUIStyle
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 0, 0, 0),
                fontSize = 12,
                normal = {textColor = new Color(0.8f, 0.8f, 0.85f)}
            };

            _tableHeaderStyle = new GUIStyle
            {
                padding = new RectOffset(12, 12, 8, 8),
                normal = {background = _headerBackgroundTexture, textColor = new Color(0.9f, 0.9f, 1f)},
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                fixedHeight = 24
            };

            _foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                padding = new RectOffset(24, 5, 7, 7),
                normal = {textColor = new Color(0.85f, 0.85f, 0.9f)}
            };

            _searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                fixedWidth = 250,
                fixedHeight = 24,
                fontSize = 12
            };

            UpdateStyleTextures();
        }

        private void CreateTextures()
        {
            _headerBackgroundTexture = CreateGradientTexture(
                new Color(0.16f, 0.16f, 0.19f),
                new Color(0.18f, 0.18f, 0.21f));

            _titleBackgroundTexture = CreateGradientTexture(
                new Color(0.14f, 0.14f, 0.17f),
                new Color(0.16f, 0.16f, 0.19f));

            // Primary row - çok daha açık ton
            _rowBackgroundTexture = CreateGradientTexture(
                new Color(0.28f, 0.28f, 0.32f, 0.9f),
                new Color(0.26f, 0.26f, 0.30f, 0.9f));

            // Alternatif satır - daha açık ton
            _alternateRowBackgroundTexture = CreateGradientTexture(
                new Color(0.22f, 0.22f, 0.26f, 0.9f),
                new Color(0.20f, 0.20f, 0.24f, 0.9f));

            _selectedRowBackgroundTexture = CreateGradientTexture(
                new Color(0.1f, 0.4f, 0.8f, 0.7f),
                new Color(0.15f, 0.45f, 0.85f, 0.7f));

            // Hover efekti için daha belirgin renk
            _hoverRowBackgroundTexture = CreateGradientTexture(
                new Color(0.25f, 0.5f, 0.9f, 0.3f),
                new Color(0.3f, 0.55f, 0.95f, 0.3f));

            _buttonBackgroundTexture = CreateGradientTexture(
                new Color(0.25f, 0.25f, 0.28f),
                new Color(0.27f, 0.27f, 0.30f));

            _activeButtonBackgroundTexture = CreateGradientTexture(
                new Color(0.682f, 0.839f, 0.945f), // HEX #aed6f1
                new Color(0.682f, 0.839f, 0.945f)); // HEX #aed6f1
        }

        private Texture2D CreateGradientTexture(Color topColor, Color bottomColor)
        {
            Texture2D texture = new Texture2D(1, 32);
            for (int i = 0; i < 32; i++)
            {
                float t = i / 31f;
                Color color = Color.Lerp(topColor, bottomColor, t);
                texture.SetPixel(0, i, color);
            }

            texture.Apply();
            return texture;
        }

        private void FindAllScreenConfigs()
        {
            _allScreenConfigs.Clear();
            string[] guids = AssetDatabase.FindAssets("t:CD_Screen");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CD_Screen config = AssetDatabase.LoadAssetAtPath<CD_Screen>(path);
                if (config != null)
                {
                    _allScreenConfigs.Add(config);
                }
            }

            _filteredScreenConfigs = new List<CD_Screen>(_allScreenConfigs);
            SortConfigs();
        }

        private void FindAllTags()
        {
            _availableTags.Clear();
            _availableTags = Enum.GetValues(typeof(ScreenTag)).Cast<ScreenTag>().ToList();
        }

        private void OnGUI()
        {
            if (_headerStyle == null)
            {
                InitializeStyles();
            }

            DrawToolbar();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(_showFilterOptions ? "Hide Filters ▲" : "Show Filters ▼",
                    EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                _showFilterOptions = !_showFilterOptions;
                EditorPrefs.SetBool("ScreenConfigManager_ShowFilters", _showFilterOptions);
                Repaint();
            }

            string infoText = $"Total Configs: {_allScreenConfigs.Count} | Filtered: {_filteredScreenConfigs.Count}";
            GUILayout.Label(infoText, EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            if (_selectedScreenConfigs.Count > 0)
            {
                GUILayout.Label($"Selected: {_selectedScreenConfigs.Count}", EditorStyles.toolbarButton, GUILayout.Width(100));

                GUIStyle editButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
                if (_showBulkEditOptions)
                {
                    editButtonStyle.normal.textColor = new Color(0.3f, 0.6f, 1f);
                    editButtonStyle.fontStyle = FontStyle.Bold;
                }

                if (GUILayout.Button(_showBulkEditOptions ? "Hide Edit Panel ▲" : "Show Edit Panel ▼",
                        editButtonStyle, GUILayout.Width(130)))
                {
                    _showBulkEditOptions = !_showBulkEditOptions;
                    EditorPrefs.SetBool("ScreenConfigManager_ShowEditPanel", _showBulkEditOptions);
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            if (_showFilterOptions)
            {
                DrawFilterOptions();
            }

            if (_showBulkEditOptions && _selectedScreenConfigs.Count > 0)
            {
                DrawBulkEditOptions();
            }

            DrawConfigTable();
            
            // Her frame'de yeniden çiz
            if (Event.current.type == EventType.Repaint)
            {
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            Rect toolbarRect = EditorGUILayout.GetControlRect(false, 40);

            Color toolbarBgColor = new Color(0.11f, 0.11f, 0.13f);
            EditorGUI.DrawRect(toolbarRect, toolbarBgColor);

            Rect bottomLine = new Rect(0, toolbarRect.y + toolbarRect.height - 1, position.width, 1);
            Color lineColor = new Color(0.25f, 0.25f, 0.3f, 0.6f);
            EditorGUI.DrawRect(bottomLine, lineColor);

            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedHeight = 24,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(12, 12, 4, 4),
                margin = new RectOffset(4, 4, 6, 6),
                normal = {textColor = new Color(0.8f, 0.8f, 0.85f)},
                hover = {textColor = Color.white}
            };

            float xPosition = 15;
            float smallButtonWidth = 90;
            float spacing = 10;

            Rect refreshButtonRect = new Rect(xPosition, toolbarRect.y + 8, smallButtonWidth, 24);
            if (DrawToolbarButton(refreshButtonRect, "Refresh", Color.white))
            {
                FindAllScreenConfigs();
                FindAllTags();
                Repaint();
            }

            xPosition += smallButtonWidth + spacing;

            Rect selectAllRect = new Rect(xPosition, toolbarRect.y + 8, smallButtonWidth, 24);
            if (DrawToolbarButton(selectAllRect, "Select All", new Color(0.4f, 0.8f, 0.4f)))
            {
                _selectedScreenConfigs.Clear();
                _selectedScreenConfigs.AddRange(_filteredScreenConfigs);
                Repaint();
            }

            xPosition += smallButtonWidth + spacing;

            Rect deselectAllRect = new Rect(xPosition, toolbarRect.y + 8, smallButtonWidth + 20, 24);
            if (DrawToolbarButton(deselectAllRect, "Deselect All", new Color(0.8f, 0.4f, 0.4f)))
            {
                _selectedScreenConfigs.Clear();
                Repaint();
            }

            Rect columnsButtonRect = new Rect(position.width - 120, toolbarRect.y + 8, 100, 24);

            if (GUI.Button(columnsButtonRect, "Columns ▼", buttonStyle))
            {
                GenericMenu columnsMenu = new GenericMenu();

                columnsMenu.AddItem(new GUIContent("Name"), _nameColumnWidth > 0, () => ToggleColumnVisibility(ref _nameColumnWidth, 200f));
                columnsMenu.AddItem(new GUIContent("Layer"), _layerColumnWidth > 0, () => ToggleColumnVisibility(ref _layerColumnWidth, 60f));
                columnsMenu.AddItem(new GUIContent("LoadByTag Type"), _loadTypeColumnWidth > 0, () => ToggleColumnVisibility(ref _loadTypeColumnWidth, 100f));
                columnsMenu.AddItem(new GUIContent("Tag"), _tagColumnWidth > 0, () => ToggleColumnVisibility(ref _tagColumnWidth, 120f));
                columnsMenu.AddItem(new GUIContent("Path/Key"), _pathColumnWidth > 0, () => ToggleColumnVisibility(ref _pathColumnWidth, 200f));
                columnsMenu.AddItem(new GUIContent("Animation"), _animationColumnWidth > 0, () => ToggleColumnVisibility(ref _animationColumnWidth, 100f));

                columnsMenu.AddSeparator("");
                columnsMenu.AddItem(new GUIContent("Reset Defaults"), false, ResetColumnLayout);

                columnsMenu.DropDown(columnsButtonRect);
            }
        }

        private bool DrawToolbarButton(Rect rect, string text, Color accentColor)
        {
            bool isHover = rect.Contains(Event.current.mousePosition);

            Color bgColor = isHover
                ? new Color(0.18f, 0.18f, 0.21f)
                : new Color(0.15f, 0.15f, 0.18f);

            DrawRoundedRect(rect, bgColor, 4);

            if (isHover)
            {
                Rect accentRect = new Rect(rect.x, rect.y + rect.height - 2, rect.width, 2);
                DrawRoundedRect(accentRect, new Color(accentColor.r, accentColor.g, accentColor.b, 0.7f), 1);
            }

            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                normal = {textColor = isHover ? accentColor : new Color(0.8f, 0.8f, 0.85f)},
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            GUI.Label(rect, text, style);

            bool clicked = Event.current.type == EventType.MouseDown
                           && rect.Contains(Event.current.mousePosition)
                           && Event.current.button == 0;

            if (clicked)
            {
                Event.current.Use();
                GUI.changed = true;
            }

            return clicked && Event.current.type == EventType.Used;
        }

        private void ToggleColumnVisibility(ref float columnWidth, float defaultWidth)
        {
            columnWidth = columnWidth > 0 ? 0 : defaultWidth;
            Repaint();
        }

        private void SaveColumnLayout()
        {
            EditorPrefs.SetFloat("ScreenConfigManager_NameColumnWidth", _nameColumnWidth);
            EditorPrefs.SetFloat("ScreenConfigManager_LayerColumnWidth", _layerColumnWidth);
            EditorPrefs.SetFloat("ScreenConfigManager_LoadTypeColumnWidth", _loadTypeColumnWidth);
            EditorPrefs.SetFloat("ScreenConfigManager_TagColumnWidth", _tagColumnWidth);
            EditorPrefs.SetFloat("ScreenConfigManager_PathColumnWidth", _pathColumnWidth);
            EditorPrefs.SetFloat("ScreenConfigManager_AnimationColumnWidth", _animationColumnWidth);

            EditorUtility.DisplayDialog("Layout Saved", "Column layout has been saved.", "OK");
        }

        private void LoadColumnLayout()
        {
            _nameColumnWidth = EditorPrefs.GetFloat("ScreenConfigManager_NameColumnWidth", 200f);
            _layerColumnWidth = EditorPrefs.GetFloat("ScreenConfigManager_LayerColumnWidth", 60f);
            _loadTypeColumnWidth = EditorPrefs.GetFloat("ScreenConfigManager_LoadTypeColumnWidth", 100f);
            _tagColumnWidth = EditorPrefs.GetFloat("ScreenConfigManager_TagColumnWidth", 120f);
            _pathColumnWidth = EditorPrefs.GetFloat("ScreenConfigManager_PathColumnWidth", 200f);
            _animationColumnWidth = EditorPrefs.GetFloat("ScreenConfigManager_AnimationColumnWidth", 100f);

            Repaint();
        }

        private void ResetColumnLayout()
        {
            _nameColumnWidth = 200f;
            _layerColumnWidth = 60f;
            _loadTypeColumnWidth = 100f;
            _tagColumnWidth = 120f;
            _pathColumnWidth = 200f;
            _animationColumnWidth = 100f;

            Repaint();
        }

        private void DrawFilterOptions()
        {
            if (!_showFilterOptions)
                return;

            Rect filterArea = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(filterArea, new Color(0.3f, 0.3f, 0.35f));

            GUILayout.BeginVertical(EditorStyles.helpBox);

            Rect bgRect = EditorGUILayout.GetControlRect(false,
                (_selectedFilterType == 1 || _selectedFilterType == 2 || _selectedFilterType == 3) ? 70 : 35);
            EditorGUI.DrawRect(new Rect(bgRect.x - 5, bgRect.y - 5,
                position.width - 15, bgRect.height + 10), new Color(0.25f, 0.25f, 0.28f));

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Search by Name:", GUILayout.Width(120));

            GUIStyle searchStyle = new GUIStyle(EditorStyles.textField)
            {
                fixedHeight = 24,
                normal = {textColor = Color.white}
            };
            string newSearchText = EditorGUILayout.TextField(_searchText, searchStyle);
            if (newSearchText != _searchText)
            {
                _searchText = newSearchText;
                ApplyFilters();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Filter Type:", GUILayout.Width(120));
            string[] filterTypes = {"All", "By LoadByTag Type", "By Tag", "By Layer"};
            int newFilterType = EditorGUILayout.Popup(_selectedFilterType, filterTypes, EditorStyles.popup);
            if (newFilterType != _selectedFilterType)
            {
                _selectedFilterType = newFilterType;
                ApplyFilters();
            }

            EditorGUILayout.EndHorizontal();

            if (_selectedFilterType == 1)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("LoadByTag Type:", GUILayout.Width(120));
                
                ScreenLoadType newLoadType = (ScreenLoadType)EditorGUILayout.EnumPopup(_selectedLoadType, EditorStyles.popup);
                if (newLoadType != _selectedLoadType)
                {
                    _selectedLoadType = newLoadType;
                    ApplyFilters();
                }

                EditorGUILayout.EndHorizontal();
            }
            else if (_selectedFilterType == 2)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Tag:", GUILayout.Width(120));

                ScreenTag newTag = (ScreenTag)EditorGUILayout.EnumPopup(_selectedTag, EditorStyles.popup);
                if (newTag != _selectedTag)
                {
                    _selectedTag = newTag;
                    ApplyFilters();
                }

                EditorGUILayout.EndHorizontal();
            }
            else if (_selectedFilterType == 3)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Layer Index:", GUILayout.Width(120));

                string[] layerNames = new string[11];
                layerNames[0] = "All";
                for (int i = 0; i <= 9; i++)
                {
                    layerNames[i + 1] = i.ToString();
                }

                int newLayerIndex = EditorGUILayout.Popup(_selectedLayerIndex, layerNames, EditorStyles.popup);
                if (newLayerIndex != _selectedLayerIndex)
                {
                    _selectedLayerIndex = newLayerIndex;
                    ApplyFilters();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBulkEditOptions()
        {
            if (_selectedScreenConfigs.Count == 0 || !_showBulkEditOptions)
                return;

            EditorGUILayout.Space(5);
            
            // Başlık alanı
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white }
            };

            GUILayout.Label("Edit Options (" + _selectedScreenConfigs.Count + " selected)", titleStyle, GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);

            _bulkEditScrollPosition = EditorGUILayout.BeginScrollView(_bulkEditScrollPosition, GUILayout.MinHeight(300));

            DrawBulkEditSection<int>("Layer Index", ref _bulkEditLayerIndex, ApplyBulkEditLayer);
            DrawBulkEditEnumSection<ScreenLoadType>("LoadByTag Type", ref _bulkEditLoadType, typeof(ScreenLoadType), ApplyBulkEditLoadType);
            DrawBulkEditEnumSection<ScreenTag>("Tag", ref _bulkEditTag, typeof(ScreenTag), ApplyBulkEditTag);
            DrawBulkEditSection<string>("Resource Path", ref _bulkEditResourcePath, ApplyBulkEditResourcePath);
            DrawBulkEditSection<string>("Addressable Key", ref _bulkEditAddressableKey, ApplyBulkEditAddressableKey);

            // Animations bölümü
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Başlık alanı
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white }
            };

            GUILayout.Label("Animations:", headerStyle, GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Toggle'lar için stil
            GUIStyle toggleStyle = new GUIStyle(EditorStyles.toggle)
            {
                fontSize = 12,
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { textColor = new Color(0.8f, 0.8f, 0.85f) }
            };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            _bulkEditHasOpenAnimation = EditorGUILayout.Toggle("Open Animation", _bulkEditHasOpenAnimation, toggleStyle, GUILayout.Height(24));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            _bulkEditHasCloseAnimation = EditorGUILayout.Toggle("Close Animation", _bulkEditHasCloseAnimation, toggleStyle, GUILayout.Height(24));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            DrawStandardApplyButton(() => ApplyBulkEditAnimations());

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);

            EditorGUILayout.EndScrollView();
        }

        private void DrawBulkEditSection<T>(string label, ref T value, Action applyAction) where T : IConvertible
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Başlık alanı
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white }
            };

            GUILayout.Label(label + ":", headerStyle, GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Input alanı
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            GUIStyle textFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fixedHeight = 24,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(0, 0, 0, 0),
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            if (typeof(T) == typeof(string))
            {
                string strValue = Convert.ToString(value);
                string newValue = EditorGUILayout.TextField(strValue, textFieldStyle, GUILayout.ExpandWidth(true));

                if (newValue != strValue)
                {
                    value = (T)(object)newValue;
                }
            }
            else if (typeof(T) == typeof(int))
            {
                int intValue = Convert.ToInt32(value);
                int newValue = EditorGUILayout.IntField(intValue, textFieldStyle, GUILayout.ExpandWidth(true));

                if (newValue != intValue)
                {
                    value = (T)(object)newValue;
                }
            }

            GUILayout.Space(8);

            DrawStandardApplyButton(() => applyAction());

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawBulkEditEnumSection<T>(string label, ref T enumValue, Type enumType, Action applyAction) where T : Enum
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Başlık alanı
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white }
            };

            GUILayout.Label(label + ":", headerStyle, GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Enum popup alanı
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            GUIStyle popupStyle = new GUIStyle(EditorStyles.popup)
            {
                fixedHeight = 24,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(0, 0, 0, 0),
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            T newValue = (T)EditorGUILayout.EnumPopup(enumValue, popupStyle, GUILayout.ExpandWidth(true));
            if (!EqualityComparer<T>.Default.Equals(newValue, enumValue))
            {
                enumValue = newValue;
            }

            GUILayout.Space(8);

            DrawStandardApplyButton(() => applyAction());

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawConfigTable()
        {
            EditorGUILayout.Space(5);

            // Tablo arka plan rengi - çok daha açık ton
            Color tableBgColor = new Color(0.24f, 0.24f, 0.28f);
            // Başlık arka plan rengi
            Color headerBgColor = new Color(0.18f, 0.18f, 0.22f);

            Rect headerRect = EditorGUILayout.GetControlRect(false, 24);
            
            // Gölge efekti - daha belirgin
            Rect shadowRect = new Rect(headerRect.x + 3, headerRect.y + 3, headerRect.width, headerRect.height);
            EditorGUI.DrawRect(shadowRect, new Color(0.05f, 0.05f, 0.05f, 0.5f));
            
            // Yuvarlatılmış köşeli başlık - daha belirgin köşeler
            DrawRoundedRect(new Rect(headerRect.x, headerRect.y, headerRect.width, headerRect.height), headerBgColor, 5f);

            // Başlık üst kenar çizgisi - daha belirgin
            Rect topBorderRect = new Rect(headerRect.x, headerRect.y, headerRect.width, 1);
            EditorGUI.DrawRect(topBorderRect, new Color(0.3f, 0.3f, 0.35f));

            float startX = 35f;

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            Rect checkboxHeaderRect = new Rect(10, headerRect.y, 25, headerRect.height);
            EditorGUI.LabelField(checkboxHeaderRect, "", headerStyle);

            if (_nameColumnWidth > 0)
            {
                Rect nameHeaderRect = new Rect(startX, headerRect.y, _nameColumnWidth - cellPadding, headerRect.height);
                EditorGUI.LabelField(nameHeaderRect, "Name", headerStyle);
                
                // Sıralama ikonu
                if (_sortColumnIndex == 0)
                {
                    Rect sortIconRect = new Rect(nameHeaderRect.x + 50, nameHeaderRect.y + 8, 14, 14);
                    string sortIcon = _sortAscending ? "▲" : "▼";
                    EditorGUI.LabelField(sortIconRect, sortIcon, new GUIStyle(EditorStyles.label) { fontSize = 10, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } });
                }
                
                startX += _nameColumnWidth;
            }

            if (_layerColumnWidth > 0)
            {
                Rect layerHeaderRect = new Rect(startX, headerRect.y, _layerColumnWidth - cellPadding, headerRect.height);
                EditorGUI.LabelField(layerHeaderRect, "Layer", headerStyle);
                
                // Sıralama ikonu
                if (_sortColumnIndex == 1)
                {
                    Rect sortIconRect = new Rect(layerHeaderRect.x + 50, layerHeaderRect.y + 8, 14, 14);
                    string sortIcon = _sortAscending ? "▲" : "▼";
                    EditorGUI.LabelField(sortIconRect, sortIcon, new GUIStyle(EditorStyles.label) { fontSize = 10, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } });
                }
                
                startX += _layerColumnWidth;
            }

            if (_loadTypeColumnWidth > 0)
            {
                Rect loadTypeHeaderRect = new Rect(startX, headerRect.y, _loadTypeColumnWidth - cellPadding, headerRect.height);
                EditorGUI.LabelField(loadTypeHeaderRect, "LoadByTag Type", headerStyle);
                startX += _loadTypeColumnWidth;
            }

            if (_tagColumnWidth > 0)
            {
                Rect tagHeaderRect = new Rect(startX, headerRect.y, _tagColumnWidth - cellPadding, headerRect.height);
                EditorGUI.LabelField(tagHeaderRect, "Tag", headerStyle);
                startX += _tagColumnWidth;
            }

            if (_pathColumnWidth > 0)
            {
                Rect pathHeaderRect = new Rect(startX, headerRect.y, _pathColumnWidth - cellPadding, headerRect.height);
                EditorGUI.LabelField(pathHeaderRect, "Path/Key", headerStyle);
                startX += _pathColumnWidth;
            }

            if (_animationColumnWidth > 0)
            {
                Rect animHeaderRect = new Rect(startX, headerRect.y, _animationColumnWidth - cellPadding, headerRect.height);
                EditorGUI.LabelField(animHeaderRect, "Animation", headerStyle);
            }

            Rect separatorRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.3f, 0.35f));

            float tableHeight = position.height - separatorRect.y - 35;

            if (_showBulkEditOptions)
            {
                tableHeight = Mathf.Max(150, position.height - separatorRect.y - 325);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
                GUILayout.Height(tableHeight));

            if (_filteredScreenConfigs.Count == 0)
            {
                EditorGUILayout.HelpBox("No screen configurations match your filter criteria.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(5);

                for (int i = 0; i < _filteredScreenConfigs.Count; i++)
                {
                    CD_Screen config = _filteredScreenConfigs[i];
                    bool isSelected = _selectedScreenConfigs.Contains(config);
                    bool isAlternateRow = i % 2 == 1;
                    bool isHovered = i == _hoveredRowIndex;

                    Rect rowRect = EditorGUILayout.GetControlRect(false, 24);

                    // Hover kontrolü
                    if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
                    {
                        if (rowRect.Contains(Event.current.mousePosition))
                        {
                            if (_hoveredRowIndex != i)
                            {
                                _hoveredRowIndex = i;
                                Repaint();
                            }
                        }
                        else if (_hoveredRowIndex == i)
                        {
                            _hoveredRowIndex = -1;
                            Repaint();
                        }
                    }

                    if (isSelected)
                    {
                        // Seçili satır için yuvarlatılmış köşeler ve gölge - daha belirgin
                        Rect rowShadowRect = new Rect(rowRect.x + 3, rowRect.y + 3, rowRect.width - 6, rowRect.height - 3);
                        EditorGUI.DrawRect(rowShadowRect, new Color(0.05f, 0.05f, 0.05f, 0.4f));
                        DrawRoundedRect(new Rect(rowRect.x, rowRect.y, rowRect.width, rowRect.height), _selectedRowColor, 4f);
                    }
                    else if (isHovered)
                    {
                        // Hover satır için yuvarlatılmış köşeler - daha belirgin
                        DrawRoundedRect(new Rect(rowRect.x, rowRect.y, rowRect.width, rowRect.height), _hoverRowColor, 4f);
                    }
                    else if (isAlternateRow)
                    {
                        // Alternatif satır için yuvarlatılmış köşeler - daha belirgin
                        DrawRoundedRect(new Rect(rowRect.x, rowRect.y, rowRect.width, rowRect.height), _alternateRowColor, 4f);
                    }
                    else
                    {
                        // Normal satır için yuvarlatılmış köşeler - daha belirgin
                        DrawRoundedRect(new Rect(rowRect.x, rowRect.y, rowRect.width, rowRect.height), tableBgColor, 4f);
                    }
                    
                    // Satır ayırıcı çizgisi
                    Rect rowSeparatorRect = new Rect(rowRect.x, rowRect.y + rowRect.height - 1, rowRect.width, 1);
                    EditorGUI.DrawRect(rowSeparatorRect, new Color(0.1f, 0.1f, 0.12f, 0.5f));

                    float startXPos = 35f;

                    Rect checkboxRect = new Rect(10, rowRect.y + 5, 20, 20);
                    bool wasSelected = isSelected;
                    bool newSelected = EditorGUI.Toggle(checkboxRect, wasSelected);

                    if (wasSelected != newSelected)
                    {
                        if (newSelected)
                        {
                            _selectedScreenConfigs.Add(config);
                        }
                        else
                        {
                            _selectedScreenConfigs.Remove(config);
                        }
                        
                        Event.current.Use();
                        Repaint();
                    }

                    GUIStyle cellStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = isSelected ? Color.white : new Color(0.85f, 0.85f, 0.85f) },
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(8, 8, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0),
                        fixedHeight = 24
                    };

                    if (_nameColumnWidth > 0)
                    {
                        Rect nameRect = new Rect(startXPos, rowRect.y, _nameColumnWidth - cellPadding, rowRect.height);
                        EditorGUI.LabelField(nameRect, config.name, cellStyle);
                        startXPos += _nameColumnWidth;
                    }

                    if (_layerColumnWidth > 0)
                    {
                        Rect layerRect = new Rect(startXPos, rowRect.y, _layerColumnWidth - cellPadding, rowRect.height);
                        EditorGUI.LabelField(layerRect, config.DefaultLayer.ToString(), cellStyle);
                        startXPos += _layerColumnWidth;
                    }

                    if (_loadTypeColumnWidth > 0)
                    {
                        Rect loadTypeRect = new Rect(startXPos, rowRect.y, _loadTypeColumnWidth - cellPadding, rowRect.height);
                        EditorGUI.LabelField(loadTypeRect, config.LoadType.ToString(), cellStyle);
                        startXPos += _loadTypeColumnWidth;
                    }

                    if (_tagColumnWidth > 0)
                    {
                        string tagText = config.Tag != ScreenTag.Default ? config.Tag.ToString() : "Default";
                        Rect tagRect = new Rect(startXPos, rowRect.y, _tagColumnWidth - cellPadding, rowRect.height);
                        EditorGUI.LabelField(tagRect, tagText, cellStyle);
                        startXPos += _tagColumnWidth;
                    }

                    if (_pathColumnWidth > 0)
                    {
                        string pathText = config.LoadType == ScreenLoadType.Resource
                            ? config.ResourcePath
                            : config.AddressableKey;

                        Rect pathRect = new Rect(startXPos, rowRect.y, _pathColumnWidth - cellPadding, rowRect.height);
                        EditorGUI.LabelField(pathRect, pathText, cellStyle);
                        startXPos += _pathColumnWidth;
                    }

                    if (_animationColumnWidth > 0)
                    {
                        string animText = "";
                        if (config.HasShowAnimation && config.HasHideAnimation)
                            animText = "Open/Close";
                        else if (config.HasShowAnimation)
                            animText = "Open";
                        else if (config.HasHideAnimation)
                            animText = "Close";
                        else
                            animText = "None";

                        Rect animRect = new Rect(startXPos, rowRect.y, _animationColumnWidth - cellPadding, rowRect.height);
                        EditorGUI.LabelField(animRect, animText, cellStyle);
                    }

                    bool controlHeld = (Event.current.modifiers & EventModifiers.Control) != 0;
                    bool shiftHeld = (Event.current.modifiers & EventModifiers.Shift) != 0;

                    if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                    {
                        if (shiftHeld && _selectedScreenConfigs.Count > 0)
                        {
                            int lastSelectedIdx = _filteredScreenConfigs.IndexOf(_selectedScreenConfigs[_selectedScreenConfigs.Count - 1]);
                            int minIdx = Mathf.Min(lastSelectedIdx, i);
                            int maxIdx = Mathf.Max(lastSelectedIdx, i);

                            for (int j = minIdx; j <= maxIdx; j++)
                            {
                                if (!_selectedScreenConfigs.Contains(_filteredScreenConfigs[j]))
                                    _selectedScreenConfigs.Add(_filteredScreenConfigs[j]);
                            }
                        }
                        else if (controlHeld)
                        {
                            if (isSelected)
                                _selectedScreenConfigs.Remove(config);
                            else
                                _selectedScreenConfigs.Add(config);
                        }
                        else
                        {
                            _selectedScreenConfigs.Clear();
                            _selectedScreenConfigs.Add(config);
                        }
                        
                        Event.current.Use();
                        Repaint();
                    }
                }

                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ApplyFilters()
        {
            _filteredScreenConfigs = new List<CD_Screen>(_allScreenConfigs);

            if (!string.IsNullOrEmpty(_searchText))
            {
                string searchLower = _searchText.ToLower();
                _filteredScreenConfigs = _filteredScreenConfigs.Where(c =>
                    c.name.ToLower().Contains(searchLower) ||
                    (c.ResourcePath != null && c.ResourcePath.ToLower().Contains(searchLower)) ||
                    (c.AddressableKey != null && c.AddressableKey.ToLower().Contains(searchLower)) ||
                    c.Tag.ToString().ToLower().Contains(searchLower)
                ).ToList();
            }

            switch (_selectedFilterType)
            {
                case 1:
                    _filteredScreenConfigs = _filteredScreenConfigs.Where(c => c.LoadType == _selectedLoadType).ToList();
                    break;

                case 2:
                    _filteredScreenConfigs = _filteredScreenConfigs.Where(c => c.Tag == _selectedTag).ToList();
                    break;

                case 3:
                    int selectedLayer = _selectedLayerIndex;
                    _filteredScreenConfigs = _filteredScreenConfigs.Where(c => c.DefaultLayer == selectedLayer).ToList();
                    break;
            }

            _selectedScreenConfigs = _selectedScreenConfigs.Where(c => _filteredScreenConfigs.Contains(c)).ToList();

            SortConfigs();
        }

        private void SortConfigs()
        {
            switch (_sortColumnIndex)
            {
                case 0:
                    _filteredScreenConfigs = _sortAscending
                        ? _filteredScreenConfigs.OrderBy(c => c.name).ToList()
                        : _filteredScreenConfigs.OrderByDescending(c => c.name).ToList();
                    break;
                case 1:
                    _filteredScreenConfigs = _sortAscending
                        ? _filteredScreenConfigs.OrderBy(c => c.DefaultLayer).ToList()
                        : _filteredScreenConfigs.OrderByDescending(c => c.DefaultLayer).ToList();
                    break;
                case 2:
                    _filteredScreenConfigs = _sortAscending
                        ? _filteredScreenConfigs.OrderBy(c => c.LoadType).ToList()
                        : _filteredScreenConfigs.OrderByDescending(c => c.LoadType).ToList();
                    break;
                case 3:
                    _filteredScreenConfigs = _sortAscending
                        ? _filteredScreenConfigs.OrderBy(c => c.Tag.ToString()).ToList()
                        : _filteredScreenConfigs.OrderByDescending(c => c.Tag.ToString()).ToList();
                    break;
                case 4:
                    _filteredScreenConfigs = _sortAscending
                        ? _filteredScreenConfigs.OrderBy(c => GetSortPathKey(c)).ToList()
                        : _filteredScreenConfigs.OrderByDescending(c => GetSortPathKey(c)).ToList();
                    break;
                case 5:
                    _filteredScreenConfigs = _sortAscending
                        ? _filteredScreenConfigs.OrderBy(c => GetAnimationSortValue(c)).ToList()
                        : _filteredScreenConfigs.OrderByDescending(c => GetAnimationSortValue(c)).ToList();
                    break;
            }
        }

        private string GetSortPathKey(CD_Screen config)
        {
            switch (config.LoadType)
            {
                case ScreenLoadType.Resource:
                    return config.ResourcePath;
                case ScreenLoadType.Addressable:
                    return config.AddressableKey;
                case ScreenLoadType.DirectPrefab:
                    return config.DirectPrefab != null ? config.DirectPrefab.name : "";
                default:
                    return "";
            }
        }

        private int GetAnimationSortValue(CD_Screen config)
        {
            int value = 0;
            if (config.HasShowAnimation) value += 1;
            if (config.HasHideAnimation) value += 2;
            return value;
        }

        private void ApplyBulkEditLayer()
        {
            Undo.RecordObjects(_selectedScreenConfigs.ToArray(), "Bulk Edit Layer");

            foreach (CD_Screen config in _selectedScreenConfigs)
            {
                SerializedObject serializedObject = new SerializedObject(config);
                SerializedProperty layerProperty = serializedObject.FindProperty("_defaultLayer");
                layerProperty.intValue = _bulkEditLayerIndex;
                serializedObject.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
        }

        private void ApplyBulkEditLoadType()
        {
            Undo.RecordObjects(_selectedScreenConfigs.ToArray(), "Bulk Edit LoadByTag Type");

            foreach (CD_Screen config in _selectedScreenConfigs)
            {
                SerializedObject serializedObject = new SerializedObject(config);
                SerializedProperty loadTypeProperty = serializedObject.FindProperty("_loadType");
                loadTypeProperty.enumValueIndex = (int)_bulkEditLoadType;
                serializedObject.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
        }

        private void ApplyBulkEditTag()
        {
            Undo.RecordObjects(_selectedScreenConfigs.ToArray(), "Bulk Edit Tag");

            foreach (CD_Screen config in _selectedScreenConfigs)
            {
                SerializedObject serializedObject = new SerializedObject(config);
                SerializedProperty tagProperty = serializedObject.FindProperty("_screenTag");
                tagProperty.enumValueIndex = (int)_bulkEditTag;
                serializedObject.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
        }

        private void ApplyBulkEditResourcePath()
        {
            Undo.RecordObjects(_selectedScreenConfigs.ToArray(), "Bulk Edit Resource Path");

            foreach (CD_Screen config in _selectedScreenConfigs)
            {
                SerializedObject serializedObject = new SerializedObject(config);
                SerializedProperty pathProperty = serializedObject.FindProperty("_resourcePath");
                pathProperty.stringValue = _bulkEditResourcePath;
                serializedObject.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
        }

        private void ApplyBulkEditAddressableKey()
        {
            Undo.RecordObjects(_selectedScreenConfigs.ToArray(), "Bulk Edit Addressable Key");

            foreach (CD_Screen config in _selectedScreenConfigs)
            {
                SerializedObject serializedObject = new SerializedObject(config);
                SerializedProperty keyProperty = serializedObject.FindProperty("_addressableKey");
                keyProperty.stringValue = _bulkEditAddressableKey;
                serializedObject.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
        }

        private void ApplyBulkEditAnimations()
        {
            Undo.RecordObjects(_selectedScreenConfigs.ToArray(), "Bulk Edit Animations");

            foreach (CD_Screen config in _selectedScreenConfigs)
            {
                SerializedObject serializedObject = new SerializedObject(config);
                SerializedProperty openAnimProperty = serializedObject.FindProperty("_hasOpenAnimation");
                SerializedProperty closeAnimProperty = serializedObject.FindProperty("_hasCloseAnimation");

                openAnimProperty.boolValue = _bulkEditHasOpenAnimation;
                closeAnimProperty.boolValue = _bulkEditHasCloseAnimation;

                serializedObject.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
        }

        private void UpdateStyleTextures()
        {
            if (_buttonBackgroundTexture != null && _activeButtonBackgroundTexture != null && _actionButtonStyle != null)
            {
                _actionButtonStyle.normal.background = _buttonBackgroundTexture;
                _actionButtonStyle.hover.background = _activeButtonBackgroundTexture;
                _actionButtonStyle.active.background = _activeButtonBackgroundTexture;
                _actionButtonStyle.normal.textColor = Color.white;
                _actionButtonStyle.hover.textColor = Color.white;
                _actionButtonStyle.active.textColor = Color.white;
            }

            if (_headerBackgroundTexture != null && _headerStyle != null)
            {
                _headerStyle.normal.background = _headerBackgroundTexture;
                _headerStyle.normal.textColor = Color.white;
            }

            if (_titleBackgroundTexture != null && _titleStyle != null)
            {
                _titleStyle.normal.background = _titleBackgroundTexture;
                _titleStyle.normal.textColor = Color.white;
            }

            if (_rowBackgroundTexture != null && _tableRowStyle != null)
            {
                _tableRowStyle.normal.background = _rowBackgroundTexture;
                _tableRowStyle.normal.textColor = Color.white;
            }

            if (_headerBackgroundTexture != null && _tableHeaderStyle != null)
            {
                _tableHeaderStyle.normal.background = _headerBackgroundTexture;
                _tableHeaderStyle.normal.textColor = Color.white;
            }

            if (_buttonBackgroundTexture != null && _rowButtonStyle != null)
            {
                _rowButtonStyle.normal.background = _buttonBackgroundTexture;
                _rowButtonStyle.hover.background = _activeButtonBackgroundTexture;
                _rowButtonStyle.active.background = _activeButtonBackgroundTexture;
                _rowButtonStyle.normal.textColor = Color.white;
                _rowButtonStyle.hover.textColor = Color.white;
                _rowButtonStyle.active.textColor = Color.white;
            }

            if (_toolbarBackground != null && _buttonBackgroundTexture != null)
            {
                _toolbarBackground.normal.background = _buttonBackgroundTexture;
                _toolbarBackground.normal.textColor = Color.white;
            }

            if (_foldoutStyle != null)
            {
                _foldoutStyle.normal.textColor = Color.white;
                _foldoutStyle.onNormal.textColor = Color.white;
            }

            if (_centeredBoldLabel != null)
            {
                _centeredBoldLabel.normal.textColor = Color.white;
            }

            if (_searchFieldStyle != null)
            {
                _searchFieldStyle.normal.textColor = Color.white;
            }

            // Pencereyi yeniden çiz
            Repaint();
        }

        private void DrawRoundedRect(Rect rect, Color color, float radius)
        {
            // Daha belirgin yuvarlatılmış köşeler için
            radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
            
            Handles.BeginGUI();
            Handles.color = color;
            
            // Köşeleri yuvarlatılmış dikdörtgen çizimi
            Vector3 topLeft = new Vector3(rect.x + radius, rect.y + radius, 0);
            Vector3 topRight = new Vector3(rect.x + rect.width - radius, rect.y + radius, 0);
            Vector3 bottomRight = new Vector3(rect.x + rect.width - radius, rect.y + rect.height - radius, 0);
            Vector3 bottomLeft = new Vector3(rect.x + radius, rect.y + rect.height - radius, 0);
            
            // Kenarları çiz - daha kalın
            Handles.DrawLine(new Vector3(topLeft.x, rect.y, 0), new Vector3(topRight.x, rect.y, 0), 2f);
            Handles.DrawLine(new Vector3(topRight.x, topRight.y, 0), new Vector3(rect.x + rect.width, topRight.y, 0), 2f);
            Handles.DrawLine(new Vector3(rect.x + rect.width, topRight.y, 0), new Vector3(rect.x + rect.width, bottomRight.y, 0), 2f);
            Handles.DrawLine(new Vector3(bottomRight.x, rect.y + rect.height, 0), new Vector3(bottomLeft.x, rect.y + rect.height, 0), 2f);
            Handles.DrawLine(new Vector3(rect.x, bottomLeft.y, 0), new Vector3(rect.x, topLeft.y, 0), 2f);
            Handles.DrawLine(new Vector3(rect.x, topLeft.y, 0), new Vector3(topLeft.x, rect.y, 0), 2f);
            
            // Köşeleri çiz - daha belirgin
            Handles.DrawWireArc(topLeft, Vector3.forward, Vector3.left, 90, radius, 2f);
            Handles.DrawWireArc(topRight, Vector3.forward, Vector3.up, 90, radius, 2f);
            Handles.DrawWireArc(bottomRight, Vector3.forward, Vector3.right, 90, radius, 2f);
            Handles.DrawWireArc(bottomLeft, Vector3.forward, Vector3.down, 90, radius, 2f);
            
            Handles.EndGUI();
            
            // İç kısmı doldur
            EditorGUI.DrawRect(new Rect(rect.x + radius, rect.y, rect.width - 2 * radius, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + radius, rect.width, rect.height - 2 * radius), color);
            
            // Köşeleri doldur
            GUI.BeginClip(new Rect(rect.x, rect.y, radius, radius));
            Handles.color = color;
            Handles.DrawSolidArc(new Vector3(radius, radius, 0), Vector3.forward, Vector3.left, 90, radius);
            GUI.EndClip();
            
            GUI.BeginClip(new Rect(rect.x + rect.width - radius, rect.y, radius, radius));
            Handles.color = color;
            Handles.DrawSolidArc(new Vector3(0, radius, 0), Vector3.forward, Vector3.up, 90, radius);
            GUI.EndClip();
            
            GUI.BeginClip(new Rect(rect.x + rect.width - radius, rect.y + rect.height - radius, radius, radius));
            Handles.color = color;
            Handles.DrawSolidArc(new Vector3(0, 0, 0), Vector3.forward, Vector3.right, 90, radius);
            GUI.EndClip();
            
            GUI.BeginClip(new Rect(rect.x, rect.y + rect.height - radius, radius, radius));
            Handles.color = color;
            Handles.DrawSolidArc(new Vector3(radius, 0, 0), Vector3.forward, Vector3.down, 90, radius);
            GUI.EndClip();
        }

        private void DrawStandardApplyButton(Action applyAction)
        {
            // Özel buton stili oluştur
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fixedHeight = 24;
            buttonStyle.fixedWidth = 90;
            buttonStyle.fontSize = 12;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.margin = new RectOffset(0, 8, 0, 0);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            
            // Buton renkleri
            Color buttonColor = new Color(0.682f, 0.839f, 0.945f); // HEX #aed6f1
            
            // Buton arka planını çiz
            Rect buttonRect = GUILayoutUtility.GetRect(90, 24, buttonStyle);
            
            // Buton çizimi
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = buttonColor;
            
            if (GUI.Button(buttonRect, "Apply", buttonStyle))
            {
                applyAction();
            }
            
            GUI.backgroundColor = oldColor;
        }
    }
}
#endif