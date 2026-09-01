using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    [CreateAssetMenu(fileName = "CD_FlowConsole", menuName = "FlowIoC/Flow Console Settings")]
    public class CD_FlowConsole : ScriptableObject
    {
        [Header("Log Settings")]
        [Tooltip("If true, the entire FlowConsole logging system is active. If false, no logs will be processed or displayed.")]
        public bool IsLoggingEnabled = true;

        [Tooltip("Controls detail panel display. If true, shows class info and full stack trace. If false, shows only the source line. Data is captured in Editor only; on mobile, no ConsoleLog is created.")]
        public bool DeepAnalysis;

        [Tooltip("Sends logged messages to Unity console as well")]
        public bool SendLogsToUnityConsole;

        [Tooltip("If true, FlowIoC keeps the ENABLE_LOG scripting define present on every platform. Turn this off when the project owns ENABLE_LOG itself (e.g. a build-mode tool that strips it for release builds) - otherwise the two would fight and recompile forever.")]
        public bool AutoAddEnableLogDefine = true;

        #if UNITY_EDITOR
        public static event Action OnProjectLogTypesChanged;
        public static event Action OnSettingsValidated;

        public static void NotifySettingsChanged()
        {
            OnSettingsValidated?.Invoke();
        }

        public static void NotifyProjectLogTypesChanged()
        {
            OnProjectLogTypesChanged?.Invoke();
        }
        #endif

        [Space]
        [Header("Log Types")]
        [Tooltip("All log types (system and custom)")]
        [SerializeField]
        private List<FlowConsoleLogTypeCVO> _logTypes = new();

        public List<FlowConsoleLogTypeCVO> LogTypes
        {
            get
            {
                if (_logTypes == null)
                {
                    _logTypes = new List<FlowConsoleLogTypeCVO>();
                    ResetToDefaults();
                }
                return _logTypes;
            }
            private set => _logTypes = value;
        }

        [Serializable]
        public class FlowConsoleLogTypeCVO
        {
            public string Name;
            public int Value;
            public Color LogColor = Color.white;
            public bool IsVisible = true;

            [Tooltip("If true, this log type cannot be removed")]
            public bool IsMandatory;

            [Tooltip("If true, this log type was auto-registered by module detection")]
            public bool IsAutoRegistered;

            [Tooltip("Name of the linked log profile")]
            public string ProfileName;

            public override bool Equals(object obj)
            {
                if (obj is FlowConsoleLogTypeCVO other)
                {
                    return Value == other.Value || string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode() ^ (Name?.GetHashCode() ?? 0);
            }
        }

        [Space]
        [Header("Log Profiles")]
        [Tooltip("Reusable formatting profiles that can be assigned to log types")]
        [SerializeField]
        private List<FlowLogProfileData> _logProfiles = new();

        public List<FlowLogProfileData> LogProfiles
        {
            get
            {
                if (_logProfiles == null)
                {
                    _logProfiles = new List<FlowLogProfileData>();
                }
                return _logProfiles;
            }
            private set => _logProfiles = value;
        }

        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterCallbacks()
        {
            UnityEditor.EditorApplication.projectChanged += OnProjectChanged;
        }

        private static void OnProjectChanged()
        {
            var settings = FlowLogger.Settings;
            if (settings != null)
            {
                settings.EnsureSystemLogTypesExist();
                settings.EnsureDefaultProjectLogTypeExists();
                settings.EnsureDefaultProfileExists();
            }
        }

        #endif

        private void OnEnable()
        {
            EnsureSystemLogTypesExist();
            EnsureDefaultProjectLogTypeExists();
            EnsureDefaultProfileExists();
        }

        private void OnValidate()
        {
            EnsureSystemLogTypesExist();
            EnsureDefaultProjectLogTypeExists();
            EnsureDefaultProfileExists();
            EnsureLogTypesHaveProfile();
            ValidateLogTypes();
            SortProjectLogTypes();
            InvalidateProfileCache();

            #if UNITY_EDITOR
            OnSettingsValidated?.Invoke();
            #endif
        }

        private void EnsureLogTypesHaveProfile()
        {
            if (_logTypes == null) return;

            bool needsUpdate = false;
            foreach (var logType in _logTypes)
            {
                if (logType.IsMandatory) continue;
                if (string.IsNullOrEmpty(logType.ProfileName))
                {
                    logType.ProfileName = "Default";
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }

        private void Reset()
        {
            _logTypes ??= new List<FlowConsoleLogTypeCVO>();
            ResetToDefaults();
            EnsureDefaultProjectLogTypeExists();
            _logProfiles ??= new List<FlowLogProfileData>();
            EnsureDefaultProfileExists();
        }

        private void EnsureSystemLogTypesExist()
        {
            _logTypes ??= new List<FlowConsoleLogTypeCVO>();
            bool needsUpdate = false;
            var existingValues = new HashSet<int>();
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingLogTypes = new Dictionary<string, FlowConsoleLogTypeCVO>(StringComparer.OrdinalIgnoreCase);

            foreach (var logType in _logTypes)
            {
                existingValues.Add(logType.Value);
                existingNames.Add(logType.Name);
                existingLogTypes[logType.Name] = logType;
            }

            foreach (SystemLogType defaultType in Enum.GetValues(typeof(SystemLogType)))
            {
                string name = defaultType.ToString();
                int value = (int)defaultType;

                if (existingLogTypes.ContainsKey(name))
                {
                    var logType = existingLogTypes[name];

                    if (logType.Value != value)
                    {
                        logType.Value = value;
                        needsUpdate = true;
                    }

                    if (!logType.IsMandatory)
                    {
                        logType.IsMandatory = true;
                        needsUpdate = true;
                    }
                }
                else
                {
                    _logTypes.Add(new FlowConsoleLogTypeCVO
                    {
                        Name = name,
                        Value = value,
                        IsVisible = true,
                        IsMandatory = true,
                        LogColor = GetDefaultColorForLogType(defaultType)
                    });
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }

        private void EnsureDefaultProjectLogTypeExists()
        {
            _logTypes ??= new List<FlowConsoleLogTypeCVO>();

            foreach (var logType in _logTypes)
            {
                if (string.Equals(logType.Name, "Default", StringComparison.OrdinalIgnoreCase)
                    && !logType.IsMandatory)
                    return;
            }

            int insertIndex = 0;
            for (int i = 0; i < _logTypes.Count; i++)
            {
                if (_logTypes[i].IsMandatory)
                    insertIndex = i + 1;
                else
                    break;
            }

            _logTypes.Insert(insertIndex, new FlowConsoleLogTypeCVO
            {
                Name = "Default",
                Value = 100,
                LogColor = Color.white,
                IsVisible = true,
                IsMandatory = false,
                IsAutoRegistered = false,
                ProfileName = "Default"
            });

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        private void EnsureDefaultProfileExists()
        {
            _logProfiles ??= new List<FlowLogProfileData>();

            bool hasDefault = false;
            foreach (var profile in _logProfiles)
            {
                if (string.Equals(profile.Name, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    hasDefault = true;
                    if (!profile.IsMandatory || profile.IsEditable)
                    {
                        profile.IsMandatory = true;
                        profile.IsEditable = false;
                        #if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
                        #endif
                    }
                    break;
                }
            }

            if (!hasDefault)
            {
                _logProfiles.Insert(0, new FlowLogProfileData
                {
                    Name = "Default",
                    IsMandatory = true,
                    IsEditable = false,
                    PrefixColor = Color.white,
                    MessageColor = Color.white,
                    PostfixColor = Color.white,
                });

                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }

        private void ValidateLogTypes()
        {
            var usedValues = new HashSet<int>();
            var usedNames = new HashSet<string>();
            var validLogTypes = new List<FlowConsoleLogTypeCVO>();

            foreach (var logType in _logTypes)
            {
                string newName = logType.Name;
                if (usedNames.Contains(newName))
                {
                    newName = FindNextAvailableName(logType.Name, usedNames);
                    logType.Name = newName;
                }

                int newValue = logType.Value;
                if (usedValues.Contains(newValue))
                {
                    newValue = FindNextAvailableValue(usedValues);
                    logType.Value = newValue;
                }

                usedNames.Add(newName);
                usedValues.Add(newValue);
                validLogTypes.Add(logType);
            }

            if (validLogTypes.Count != _logTypes.Count)
            {
                _logTypes = validLogTypes;
            }
        }

        public void SortProjectLogTypes()
        {
            if (_logTypes == null || _logTypes.Count <= 1) return;

            var systemTypes = new List<FlowConsoleLogTypeCVO>();
            FlowConsoleLogTypeCVO defaultType = null;
            var projectTypes = new List<FlowConsoleLogTypeCVO>();

            foreach (var lt in _logTypes)
            {
                if (lt.IsMandatory)
                    systemTypes.Add(lt);
                else if (string.Equals(lt.Name, "Default", StringComparison.OrdinalIgnoreCase))
                    defaultType = lt;
                else
                    projectTypes.Add(lt);
            }

            projectTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            if (defaultType != null)
                defaultType.Value = 100;

            const int baseValue = 1000;
            const int step = 10;
            for (int i = 0; i < projectTypes.Count; i++)
                projectTypes[i].Value = baseValue + i * step;

            _logTypes.Clear();
            _logTypes.AddRange(systemTypes);
            if (defaultType != null)
                _logTypes.Add(defaultType);
            _logTypes.AddRange(projectTypes);

            _logTypeByValue = null;
            _logTypeByName = null;
        }

        private int FindNextAvailableValue(HashSet<int> usedValues)
        {
            int value = 100;

            while (usedValues.Contains(value))
            {
                value++;
            }

            return value;
        }

        private string FindNextAvailableName(string baseName, HashSet<string> usedNames)
        {
            string name = baseName;
            int suffix = 1;

            while (usedNames.Contains(name))
            {
                name = $"{baseName}_{suffix}";
                suffix++;
            }

            return name;
        }

        public void ResetToDefaults()
        {
            _logTypes.Clear();

            foreach (SystemLogType defaultType in Enum.GetValues(typeof(SystemLogType)))
            {
                _logTypes.Add(new FlowConsoleLogTypeCVO
                {
                    Name = defaultType.ToString(),
                    Value = (int)defaultType,
                    IsVisible = true,
                    IsMandatory = true,
                    LogColor = GetDefaultColorForLogType(defaultType)
                });
            }

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        private Color GetDefaultColorForLogType(SystemLogType logType)
        {
            switch (logType)
            {
                case SystemLogType.All: return Color.white;
                case SystemLogType.Context: return new Color(0.2f, 0.6f, 1f);
                case SystemLogType.Injection: return new Color(0.2f, 1f, 0.2f);
                case SystemLogType.Command: return new Color(1f, 0.7f, 0.2f);
                case SystemLogType.CommandOperation: return new Color(1f, 0.7f, 0.2f);
                case SystemLogType.Function: return new Color(0.8f, 0.4f, 1f);
                case SystemLogType.Screen: return new Color(0.3f, 0.8f, 0.8f);
                case SystemLogType.Pool: return new Color(1f, 0.4f, 0.7f);
                case SystemLogType.Model: return new Color(0.6f, 1f, 0.6f);
                default: return Color.white;
            }
        }

        private Dictionary<int, FlowConsoleLogTypeCVO> _logTypeByValue;
        private Dictionary<string, FlowConsoleLogTypeCVO> _logTypeByName;

        public void RebuildCache()
        {
            _logTypeByValue = new Dictionary<int, FlowConsoleLogTypeCVO>(_logTypes.Count);
            _logTypeByName = new Dictionary<string, FlowConsoleLogTypeCVO>(_logTypes.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var lt in _logTypes)
            {
                _logTypeByValue[lt.Value] = lt;
                if (!string.IsNullOrEmpty(lt.Name))
                    _logTypeByName[lt.Name] = lt;
            }
        }

        public bool TryGetLogType(int logTypeValue, out FlowConsoleLogTypeCVO result)
        {
            if (_logTypeByValue == null) RebuildCache();
            return _logTypeByValue.TryGetValue(logTypeValue, out result);
        }

        public bool IsLogTypeVisible(int logTypeValue)
        {
            if (_logTypeByValue == null) RebuildCache();
            return !_logTypeByValue.TryGetValue(logTypeValue, out var type) || type.IsVisible;
        }

        public bool IsLogTypeVisible(string typeName)
        {
            if (_logTypeByName == null) RebuildCache();
            return !_logTypeByName.TryGetValue(typeName, out var type) || type.IsVisible;
        }

        public FlowConsoleLogTypeCVO AddLogType(string name, int value = -1, Color? color = null)
        {

            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogError("Log type name cannot be empty.");
                return null;
            }

            foreach (var logType in _logTypes)
            {
                if (string.Equals(logType.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"Log type '{name}' already exists.");
                    return logType;
                }
            }

            if (value < 0)
            {
                var usedValues = new HashSet<int>();
                foreach (var logType in _logTypes)
                {
                    usedValues.Add(logType.Value);
                }
                value = FindNextAvailableValue(usedValues);
            }
            else
            {
                foreach (var logType in _logTypes)
                {
                    if (logType.Value == value)
                    {
                        Debug.LogWarning($"Value '{value}' is already in use. Assigning a new value.");
                        var usedValues = new HashSet<int>();
                        foreach (var lt in _logTypes)
                        {
                            usedValues.Add(lt.Value);
                        }
                        value = FindNextAvailableValue(usedValues);
                        break;
                    }
                }
            }

            var newLogType = new FlowConsoleLogTypeCVO
            {
                Name = name,
                Value = value,
                LogColor = color ?? Color.white,
                IsVisible = true,
                IsMandatory = false,
                ProfileName = "Default"
            };

            _logTypes.Add(newLogType);
            _logTypeByValue = null;
            _logTypeByName = null;
            InvalidateProfileCache();

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif

            return newLogType;
        }

        public bool RemoveLogType(int value)
        {
            for (int i = 0; i < _logTypes.Count; i++)
            {
                var logType = _logTypes[i];
                if (logType.Value == value)
                {
                    if (logType.IsMandatory)
                    {
                        Debug.LogWarning($"Cannot remove mandatory log type: {logType.Name}");
                        return false;
                    }

                    _logTypes.RemoveAt(i);
                    _logTypeByValue = null;
                    _logTypeByName = null;
                    InvalidateProfileCache();

                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
                    #endif
                    return true;
                }
            }

            return false;
        }

        public bool RemoveLogType(string logTypeName)
        {
            for (int i = 0; i < _logTypes.Count; i++)
            {
                var logType = _logTypes[i];
                if (string.Equals(logType.Name, logTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    if (logType.IsMandatory)
                    {
                        Debug.LogWarning($"Cannot remove mandatory log type: {logType.Name}");
                        return false;
                    }

                    _logTypes.RemoveAt(i);
                    _logTypeByValue = null;
                    _logTypeByName = null;
                    InvalidateProfileCache();

                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
                    #endif
                    return true;
                }
            }

            return false;
        }

        private Dictionary<string, FlowLogProfileData> _profileByName;
        private Dictionary<int, FlowLogProfile> _resolvedProfileByLogType;

        public FlowLogProfile GetResolvedProfile(int logTypeValue)
        {
            if (_resolvedProfileByLogType == null) BuildProfileCache();
            _resolvedProfileByLogType.TryGetValue(logTypeValue, out var profile);
            return profile;
        }

        public void InvalidateProfileCache()
        {
            _resolvedProfileByLogType = null;
            _profileByName = null;
        }

        private void BuildProfileCache()
        {
            _resolvedProfileByLogType = new Dictionary<int, FlowLogProfile>();
            _profileByName = new Dictionary<string, FlowLogProfileData>(StringComparer.OrdinalIgnoreCase);

            if (_logProfiles != null)
            {
                foreach (var profile in _logProfiles)
                {
                    if (!string.IsNullOrEmpty(profile.Name))
                        _profileByName[profile.Name] = profile;
                }
            }

            if (_logTypes != null)
            {
                foreach (var logType in _logTypes)
                {
                    if (string.IsNullOrEmpty(logType.ProfileName)) continue;

                    if (_profileByName.TryGetValue(logType.ProfileName, out var profileData) && profileData.IsEffective())
                    {
                        _resolvedProfileByLogType[logType.Value] = profileData.ToProfile();
                    }
                }
            }
        }

        public FlowLogProfileData AddProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                Debug.LogError("Profile name cannot be empty.");
                return null;
            }

            foreach (var profile in _logProfiles)
            {
                if (string.Equals(profile.Name, profileName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"Profile '{profileName}' already exists.");
                    return profile;
                }
            }

            var newProfile = new FlowLogProfileData
            {
                Name = profileName,
                IsEditable = true,
                IsMandatory = false,
                PrefixColor = Color.white,
                MessageColor = Color.white,
                PostfixColor = Color.white,
            };

            _logProfiles.Add(newProfile);
            InvalidateProfileCache();

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif

            return newProfile;
        }

        public bool RemoveProfile(string profileName)
        {
            for (int i = 0; i < _logProfiles.Count; i++)
            {
                var profile = _logProfiles[i];
                if (string.Equals(profile.Name, profileName, StringComparison.OrdinalIgnoreCase))
                {
                    if (profile.IsMandatory)
                    {
                        Debug.LogWarning($"Cannot remove mandatory profile: {profile.Name}");
                        return false;
                    }

                    if (_logTypes != null)
                    {
                        foreach (var logType in _logTypes)
                        {
                            if (string.Equals(logType.ProfileName, profileName, StringComparison.OrdinalIgnoreCase))
                            {
                                logType.ProfileName = "";
                            }
                        }
                    }

                    _logProfiles.RemoveAt(i);
                    InvalidateProfileCache();

                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
                    #endif
                    return true;
                }
            }

            return false;
        }
    }
}