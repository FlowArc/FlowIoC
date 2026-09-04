#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.CodeGenerator.Detector;
using FlowIoC.Editor.ModuleScanner;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{

    [CustomEditor(typeof(CD_FlowConsole))]
    public class FlowConsoleSettingsEditor : UnityEditor.Editor
    {
        private CD_FlowConsole _settings;
        private Vector2 _customTypesScrollPosition;
        private Vector2 _profileScrollPosition;

        private string _newLogTypeName = "";

        private string _newProfileName = "";
        private Dictionary<string, bool> _profileFoldouts = new();

        private const float ColLabel = 65f;
        private const float ColText = 150f;
        private const float ColStyle = 90f;
        private const float ColColor = 40f;
        private const float ColDelete = 20f;

        private void OnEnable()
        {
            _settings = (CD_FlowConsole)target;
            _profileFoldouts.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Flow Console Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsLoggingEnabled"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DeepAnalysis"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SendLogsToUnityConsole"));

            var autoAddDefine = serializedObject.FindProperty("AutoAddEnableLogDefine");
            EditorGUILayout.PropertyField(autoAddDefine);

            if (!autoAddDefine.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "ENABLE_LOG is owned by the project, not by FlowConsole. Turning this back on makes FlowConsole re-add the define on every Editor load, which fights any tool that strips it for release builds.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Log Types Management", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            DrawSystemLogTypes();

            EditorGUILayout.Space();

            DrawLogProfiles();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(EditorStyles.helpBox);

            DrawCustomLogTypesPanel();

            EditorGUILayout.Space();

            DrawAddNewLogTypePanel();

            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_settings);
                CD_FlowConsole.NotifySettingsChanged();
            }
        }

        private void DrawSystemLogTypes()
        {
            EditorGUILayout.LabelField("System Log Types", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Flow Console window background colors for system-level log filtering.", MessageType.Info);

            GUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("BG Color", EditorStyles.miniLabel, GUILayout.Width(55));
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Show All", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                SetAllSystemLogTypesVisibility(true);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(4);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Hide All", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                SetAllSystemLogTypesVisibility(false);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            foreach (SystemLogType logType in Enum.GetValues(typeof(SystemLogType)))
            {
                if (logType == SystemLogType.All)
                    continue;

                var logTypeSetting = _settings.LogTypes.FirstOrDefault(t => t.Value == (int)logType);
                if (logTypeSetting == null)
                    continue;

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(logType.ToString(), GUILayout.Width(120));
                EditorGUI.EndDisabledGroup();

                Color newColor = EditorGUILayout.ColorField(GUIContent.none, logTypeSetting.LogColor, false, false, false, GUILayout.Width(40));
                if (newColor != logTypeSetting.LogColor)
                {
                    logTypeSetting.LogColor = newColor;
                    EditorUtility.SetDirty(_settings);
                }

                GUI.backgroundColor = logTypeSetting.IsVisible ? Color.green : Color.red;
                if (GUILayout.Button(logTypeSetting.IsVisible ? "Visible" : "Hidden", GUILayout.Width(60)))
                {
                    logTypeSetting.IsVisible = !logTypeSetting.IsVisible;
                    EditorUtility.SetDirty(_settings);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private void DrawLogProfiles()
        {
            EditorGUILayout.LabelField("Log Profiles", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Reusable formatting profiles. Assign to project log types below.", MessageType.Info);

            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Column headers
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(ColLabel));
            EditorGUILayout.LabelField("Text", EditorStyles.miniLabel, GUILayout.Width(ColText));
            EditorGUILayout.LabelField("Style", EditorStyles.miniLabel, GUILayout.Width(ColStyle));
            EditorGUILayout.LabelField("Color", EditorStyles.miniLabel, GUILayout.Width(ColColor));
            EditorGUILayout.EndHorizontal();

            _profileScrollPosition = EditorGUILayout.BeginScrollView(_profileScrollPosition, GUILayout.MaxHeight(150));

            var profiles = _settings.LogProfiles;

            for (int p = 0; p < profiles.Count; p++)
            {
                var profile = profiles[p];
                string key = profile.Name ?? $"__unnamed_{p}";

                if (!_profileFoldouts.ContainsKey(key))
                    _profileFoldouts[key] = false;

                // Profile header row — rect-based for precise alignment
                string label = profile.IsMandatory ? $"{profile.Name} (Mandatory)" : profile.Name;
                Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                float colTotal = ColLabel + ColText + ColStyle + ColColor;

                string arrow = _profileFoldouts[key] ? "\u25BC" : "\u25B6";
                Rect arrowRect = new Rect(rowRect.x, rowRect.y, 16, rowRect.height);
                EditorGUI.LabelField(arrowRect, arrow);

                Rect nameRect = new Rect(rowRect.x + 16, rowRect.y, colTotal - 16, rowRect.height);
                EditorGUI.LabelField(nameRect, label, EditorStyles.boldLabel);

                Rect clickRect = new Rect(rowRect.x, rowRect.y, colTotal, rowRect.height);
                if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
                {
                    _profileFoldouts[key] = !_profileFoldouts[key];
                    Event.current.Use();
                    GUI.changed = true;
                }

                if (!profile.IsMandatory)
                {
                    Rect deleteRect = new Rect(rowRect.x + colTotal + 2, rowRect.y, ColDelete, rowRect.height);
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    if (GUI.Button(deleteRect, "X", EditorStyles.miniButton))
                    {
                        Undo.RecordObject(_settings, "Remove Log Profile");
                        if (_settings.RemoveProfile(profile.Name))
                        {
                            EditorUtility.SetDirty(_settings);
                            GUIUtility.ExitGUI();
                            return;
                        }
                    }
                    GUI.backgroundColor = prevBg;
                }

                if (_profileFoldouts[key])
                {
                    EditorGUI.BeginDisabledGroup(!profile.IsEditable);
                    EditorGUI.BeginChangeCheck();
                    DrawProfileFields(profile);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_settings, "Modify Log Profile");
                        _settings.InvalidateProfileCache();
                    }
                    EditorGUI.EndDisabledGroup();
                }

                if (p < profiles.Count - 1)
                    EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();

            // Add new profile
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(40));

            bool addProfileEnter = Event.current.type == EventType.KeyDown
                && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                && GUI.GetNameOfFocusedControl() == "AddProfileField";
            if (addProfileEnter) Event.current.Use();

            GUI.SetNextControlName("AddProfileField");
            _newProfileName = EditorGUILayout.TextField(_newProfileName, GUILayout.Width(150));

            if (GUILayout.Button("Add Profile", GUILayout.Width(100)) || addProfileEnter)
            {
                if (!string.IsNullOrWhiteSpace(_newProfileName))
                {
                    var existing = _settings.LogProfiles.FirstOrDefault(p =>
                        string.Equals(p.Name, _newProfileName, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        EditorUtility.DisplayDialog("Warning", $"Profile '{_newProfileName}' already exists.", "OK");
                    }
                    else
                    {
                        Undo.RecordObject(_settings, "Add Log Profile");
                        _settings.AddProfile(_newProfileName);
                        EditorUtility.SetDirty(_settings);
                        _newProfileName = "";
                        GUI.FocusControl(null);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Profile name cannot be empty.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawProfileFields(FlowLogProfileData profile)
        {
            // Prefix
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Prefix", EditorStyles.miniLabel, GUILayout.Width(ColLabel));
            profile.Prefix = EditorGUILayout.TextField(profile.Prefix ?? "", GUILayout.Width(ColText));
            profile.PrefixStyle = (FlowTextStyle)EditorGUILayout.EnumFlagsField(profile.PrefixStyle, GUILayout.Width(ColStyle));
            profile.PrefixColor = EditorGUILayout.ColorField(GUIContent.none, profile.PrefixColor, false, false, false, GUILayout.Width(ColColor));
            EditorGUILayout.EndHorizontal();

            // Message
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Message", EditorStyles.miniLabel, GUILayout.Width(ColLabel));
            EditorGUILayout.LabelField("", GUILayout.Width(ColText));
            profile.MessageStyle = (FlowTextStyle)EditorGUILayout.EnumFlagsField(profile.MessageStyle, GUILayout.Width(ColStyle));
            profile.MessageColor = EditorGUILayout.ColorField(GUIContent.none, profile.MessageColor, false, false, false, GUILayout.Width(ColColor));
            EditorGUILayout.EndHorizontal();

            // Postfix
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Postfix", EditorStyles.miniLabel, GUILayout.Width(ColLabel));
            profile.Postfix = EditorGUILayout.TextField(profile.Postfix ?? "", GUILayout.Width(ColText));
            profile.PostfixStyle = (FlowTextStyle)EditorGUILayout.EnumFlagsField(profile.PostfixStyle, GUILayout.Width(ColStyle));
            profile.PostfixColor = EditorGUILayout.ColorField(GUIContent.none, profile.PostfixColor, false, false, false, GUILayout.Width(ColColor));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCustomLogTypesPanel()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Project Log Types", EditorStyles.boldLabel);
            if (GUILayout.Button("Scan Modules", GUILayout.Width(100)))
            {
                ModuleAutoDetector.RescanModules();
                new ModuleScannerStartupReport().Report();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Regenerate", GUILayout.Width(90)))
            {
                FlowLogTypeGenerator.Generate();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Auto-registered module types and manually added types. Stored in this settings asset.", MessageType.Info);

            GUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", EditorStyles.miniLabel, GUILayout.Width(160));
            EditorGUILayout.LabelField("Profile", EditorStyles.miniLabel, GUILayout.Width(100));
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Show All", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                SetAllProjectLogTypesVisibility(true);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(4);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Hide All", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                SetAllProjectLogTypesVisibility(false);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            _customTypesScrollPosition = EditorGUILayout.BeginScrollView(_customTypesScrollPosition, GUILayout.Height(200));

            bool hasCustomTypes = false;
            string[] profileOptions = GetProfileOptions();

            foreach (var logType in _settings.LogTypes)
            {
                if (Enum.IsDefined(typeof(SystemLogType), logType.Value) || logType.IsMandatory)
                    continue;

                hasCustomTypes = true;

                EditorGUILayout.BeginHorizontal("box");

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(logType.Name, GUILayout.Width(160));
                EditorGUI.EndDisabledGroup();

                int currentProfileIndex = GetProfileIndex(logType.ProfileName, profileOptions);
                int newProfileIndex = EditorGUILayout.Popup(currentProfileIndex, profileOptions, GUILayout.Width(100));
                if (newProfileIndex != currentProfileIndex)
                {
                    Undo.RecordObject(_settings, "Change Log Type Profile");
                    logType.ProfileName = profileOptions[newProfileIndex];
                    _settings.InvalidateProfileCache();
                    EditorUtility.SetDirty(_settings);
                }

                GUI.backgroundColor = logType.IsVisible ? Color.green : Color.red;
                if (GUILayout.Button(logType.IsVisible ? "Visible" : "Hidden", GUILayout.Width(60)))
                {
                    logType.IsVisible = !logType.IsVisible;
                    EditorUtility.SetDirty(_settings);
                }
                GUI.backgroundColor = Color.white;

                bool isDefaultType = string.Equals(logType.Name, "Default", StringComparison.OrdinalIgnoreCase);
                if (!logType.IsAutoRegistered && !isDefaultType)
                {
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        if (_settings.RemoveLogType(logType.Name))
                        {
                            _settings.SortProjectLogTypes();
                            EditorUtility.SetDirty(_settings);
                            FlowLogTypeGenerator.Generate();
                            GUIUtility.ExitGUI();
                            return;
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (!hasCustomTypes)
            {
                EditorGUILayout.HelpBox("No project log types registered yet. Module types are auto-registered on editor startup.", MessageType.None);
            }

            EditorGUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void DrawAddNewLogTypePanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Add Custom Log Type", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Name:", GUILayout.Width(50));

            bool addLogTypeEnter = Event.current.type == EventType.KeyDown
                && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                && GUI.GetNameOfFocusedControl() == "AddLogTypeField";
            if (addLogTypeEnter) Event.current.Use();

            GUI.SetNextControlName("AddLogTypeField");
            _newLogTypeName = EditorGUILayout.TextField(_newLogTypeName, GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Add Log Type", GUILayout.Height(30)) || addLogTypeEnter)
            {
                if (!string.IsNullOrWhiteSpace(_newLogTypeName))
                {
                    var existing = _settings.LogTypes.FirstOrDefault(t =>
                        string.Equals(t.Name, _newLogTypeName, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        EditorUtility.DisplayDialog("Warning", $"Log type '{_newLogTypeName}' already exists.", "OK");
                    }
                    else
                    {
                        _settings.AddLogType(_newLogTypeName, FindNextAvailableValue());
                        _settings.SortProjectLogTypes();
                        EditorUtility.SetDirty(_settings);
                        FlowLogTypeGenerator.Generate();
                        _newLogTypeName = "";
                        GUI.FocusControl(null);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Log type name cannot be empty.", "OK");
                }
            }
        }

        private string[] GetProfileOptions()
        {
            var profiles = _settings.LogProfiles;
            var options = new string[profiles.Count];
            for (int i = 0; i < profiles.Count; i++)
                options[i] = profiles[i].Name;
            return options;
        }

        private int GetProfileIndex(string profileName, string[] options)
        {
            if (!string.IsNullOrEmpty(profileName))
            {
                for (int i = 0; i < options.Length; i++)
                {
                    if (string.Equals(options[i], profileName, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }
            return 0;
        }

        private void SetAllSystemLogTypesVisibility(bool visible)
        {
            foreach (var logType in _settings.LogTypes)
            {
                if (!logType.IsMandatory || logType.Value == (int)SystemLogType.All)
                    continue;

                logType.IsVisible = visible;
            }
            EditorUtility.SetDirty(_settings);
        }

        private void SetAllProjectLogTypesVisibility(bool visible)
        {
            foreach (var logType in _settings.LogTypes)
            {
                if (Enum.IsDefined(typeof(SystemLogType), logType.Value) || logType.IsMandatory)
                    continue;

                logType.IsVisible = visible;
            }
            EditorUtility.SetDirty(_settings);
        }

        private int FindNextAvailableValue()
        {
            var usedValues = new HashSet<int>();

            foreach (SystemLogType logType in Enum.GetValues(typeof(SystemLogType)))
                usedValues.Add((int)logType);

            foreach (var logType in _settings.LogTypes)
                usedValues.Add(logType.Value);

            int value = 1000;
            while (usedValues.Contains(value))
                value++;

            return value;
        }
    }
}
#endif
