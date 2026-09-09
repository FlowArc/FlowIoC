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

        [Tooltip(
            "Controls detail panel display. If true, shows class info and full stack trace. If false, shows only the source line. Data is captured in Editor only; on mobile, no ConsoleLog is created.")]
        public bool DeepAnalysis;

        [Tooltip(
            "Which logs work out where they came from. Capturing a source means building the whole managed stack as a string and picking it apart, and the framework logs every signal, injection and command - so this is the most expensive thing the console does. Warnings and errors are the ones somebody follows back, so they are what it is spent on by default. Raise it to Always while following a flow.")]
        public FlowStackTraceCapture StackTraceCapture = FlowStackTraceCapture.WarningsAndErrors;

        [Tooltip(
            "How many logs the console keeps. The oldest are dropped past this, so a long play session does not hold every log it ever wrote. Zero keeps all of them.")]
        [Min(0)]
        public int MaxLogCount = 5000;

        [Tooltip("Sends logged messages to Unity console as well")]
        public bool SendLogsToUnityConsole;

        [Tooltip(
            "If true, FlowIoC keeps the ENABLE_LOG scripting define present on every platform. Turn this off when the project owns ENABLE_LOG itself (e.g. a build-mode tool that strips it for release builds) - otherwise the two would fight and recompile forever.")]
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

        [Space] [Header("Log Types")] [Tooltip("All log types (system and custom)")] [SerializeField]
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

        /// <summary>
        /// True while this object is the in-memory stand-in <see cref="FlowLogger"/> builds when
        /// CD_FlowConsole.asset will not load, and false once it has been written to disk or read
        /// from it. Unity does not serialise an internal field, so an object that came from an
        /// asset always answers false.
        ///
        /// It exists because ResetToDefaults fills a stand-in with exactly the mandatory channels,
        /// which makes it indistinguishable from a freshly authored asset by its contents alone.
        /// Editor tooling that deletes generated source on the settings' word has to be able to
        /// tell the two apart.
        /// </summary>
        [NonSerialized] internal bool IsStandIn;

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

            /// <summary>
            /// The name alone, because the name is what identifies a channel. Two channels used to
            /// count as equal when their numbers matched, which was true of every channel the
            /// project added while the numbers were being handed out in order - and would be true
            /// again of two modules added on two branches and then merged, since each branch was
            /// handed the same next number.
            /// </summary>
            public override bool Equals(object obj)
            {
                return obj is FlowConsoleLogTypeCVO other
                       && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }

            public override int GetHashCode()
            {
                return Name == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
            }
        }

        [Space] [Header("Log Profiles")] [Tooltip("Reusable formatting profiles that can be assigned to log types")] [SerializeField]
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
                settings.EnsureSystemProfilesExist();
            }
        }

#endif

        private void OnEnable()
        {
            EnsureSystemLogTypesExist();
            EnsureDefaultProjectLogTypeExists();
            EnsureDefaultProfileExists();
            EnsureSystemProfilesExist();
        }

        private void OnValidate()
        {
            EnsureSystemLogTypesExist();
            EnsureDefaultProjectLogTypeExists();
            EnsureDefaultProfileExists();
            EnsureSystemProfilesExist();
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
            EnsureSystemProfilesExist();
        }

        /// <summary>
        /// The set of names SystemLogType currently declares, which is what a mandatory row in a
        /// settings asset is checked against. A channel retired from the enum leaves a row behind
        /// otherwise, and a mandatory row cannot be deleted from the Filters panel - so the asset
        /// would carry a column nothing can fill and nobody can remove.
        /// </summary>
        private static HashSet<string> SystemChannelNames()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (SystemLogType channel in Enum.GetValues(typeof(SystemLogType)))
                names.Add(channel.ToString());

            return names;
        }

        /// <summary>
        /// Drops the mandatory rows whose channel the enum no longer declares. Only mandatory rows
        /// are considered: a project channel is the game's own and is never touched here, and the
        /// Default row is written as project-owned rather than mandatory.
        /// </summary>
        internal bool PruneRetiredSystemLogTypes()
        {
            var names = SystemChannelNames();
            bool removed = false;

            for (int i = _logTypes.Count - 1; i >= 0; i--)
            {
                var logType = _logTypes[i];

                if (!logType.IsMandatory || names.Contains(logType.Name)) continue;

                _logTypes.RemoveAt(i);
                removed = true;
            }

            // Every other place that touches _logTypes drops both caches, and this one has to as
            // well: a row still in the name cache answers TryGetLogType after it has left the list.
            if (removed)
            {
                _logTypeByValue = null;
                _logTypeByName = null;
            }

            return removed;
        }

        private void EnsureSystemLogTypesExist()
        {
            _logTypes ??= new List<FlowConsoleLogTypeCVO>();
            bool needsUpdate = PruneRetiredSystemLogTypes();
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
                int value = (int) defaultType;

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

        /// <summary>
        /// One profile per framework channel, written from code so a project that has just
        /// installed the package has them without anybody authoring a list.
        ///
        /// The profile is where the channel's tag lives - "[Command]", "[Signal]" - rather than in
        /// the message text. A message says what happened to what; which channel it is on is the
        /// column it is in, and repeating that in the text spends the width the message needs.
        ///
        /// They are mandatory and not editable: a channel whose tag somebody renamed no longer
        /// matches what the documentation says the console prints.
        /// </summary>
        /// <summary>
        /// The profile half of <see cref="PruneRetiredSystemLogTypes"/>. Default is mandatory and
        /// is not a channel, so it is named here rather than left to the enum to vouch for.
        /// </summary>
        internal bool PruneRetiredSystemProfiles()
        {
            var names = SystemChannelNames();
            bool removed = false;

            for (int i = _logProfiles.Count - 1; i >= 0; i--)
            {
                var profile = _logProfiles[i];

                if (!profile.IsMandatory) continue;
                if (names.Contains(profile.Name)) continue;
                if (string.Equals(profile.Name, "Default", StringComparison.OrdinalIgnoreCase)) continue;

                _logProfiles.RemoveAt(i);
                removed = true;
            }

            if (removed)
                InvalidateProfileCache();

            return removed;
        }

        private void EnsureSystemProfilesExist()
        {
            _logProfiles ??= new List<FlowLogProfileData>();

            bool changed = PruneRetiredSystemProfiles();

            foreach (SystemLogType channel in Enum.GetValues(typeof(SystemLogType)))
            {
                if (channel == SystemLogType.All) continue;

                string name = channel.ToString();
                FlowLogProfileData profile = _logProfiles.Find(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                TryGetLogType((int) channel, out var channelType);

                if (profile == null)
                {
                    profile = new FlowLogProfileData {Name = name};
                    _logProfiles.Add(profile);
                    changed = true;
                }

                // The tag follows the channel: same name, same colour. Kept in step here rather
                // than authored, so recolouring a channel recolours its tag and the two never say
                // different things about the same thing.
                Color channelColor = channelType != null
                    ? channelType.LogColor
                    : GetDefaultColorForLogType(channel);

                string prefix = "[" + name + "]";

                if (profile.Prefix != prefix || profile.PrefixColor != channelColor
                                             || profile.PrefixStyle != FlowTextStyle.None
                                             || !profile.IsMandatory || profile.IsEditable)
                {
                    profile.Prefix = prefix;
                    profile.PrefixColor = channelColor;
                    profile.PrefixStyle = FlowTextStyle.None;

                    // Mandatory so it cannot be deleted - a channel whose profile is gone prints no
                    // tag - and not editable, because a tag somebody renamed no longer matches what
                    // the documentation says the console prints.
                    profile.IsMandatory = true;
                    profile.IsEditable = false;
                    changed = true;
                }

                if (channelType != null && channelType.ProfileName != name)
                {
                    channelType.ProfileName = name;
                    changed = true;
                }
            }

            if (!changed) return;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
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

            // Only the order of the list is decided here. A project channel's number used to be
            // handed out along it - 1000, 1010, 1020 by alphabet - which meant a module whose name
            // sorted early moved every channel after it onto a different number, and with it every
            // saved filter, every row already recorded, and every constant a script had compiled
            // against. A project channel is identified by its name now and carries no number at
            // all; the framework's own channels keep theirs, because those are a fixed enum.

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
                    Value = (int) defaultType,
                    IsVisible = true,
                    IsMandatory = true,
                    LogColor = GetDefaultColorForLogType(defaultType)
                });
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// The colour a channel is drawn in, and - through its profile - the colour of the tag on
        /// the front of its lines. These are the values the console was tuned to by eye, so a
        /// project that has just installed the package reads the same console the framework was
        /// developed against. Changing one here changes it for new projects only: an asset that
        /// already holds the channel keeps whatever colour it was given.
        /// </summary>
        private Color GetDefaultColorForLogType(SystemLogType logType)
        {
            switch (logType)
            {
                case SystemLogType.All: return Color.white;
                case SystemLogType.Context: return new Color(0.11f, 1f, 0.535f);
                case SystemLogType.Injection: return new Color(0.104f, 0.809f, 0.528f);
                case SystemLogType.Signal: return new Color(1f, 0.78f, 0.224f);
                case SystemLogType.SignalOperation: return new Color(0.787f, 0.614f, 0.176f);
                case SystemLogType.Command: return Color.cyan;
                case SystemLogType.CommandOperation: return new Color(0f, 0.667f, 0.667f);
                case SystemLogType.Function: return new Color(0.231f, 0.765f, 1f);
                case SystemLogType.Screen: return new Color(0.953f, 0.912f, 0.211f);
                case SystemLogType.Pool: return new Color(0.629f, 0.533f, 1f);
                case SystemLogType.Asset: return new Color(0.922f, 0.902f, 0.808f);
                case SystemLogType.Unity: return new Color(0.962f, 0.937f, 0.84f);
                case SystemLogType.Compiler: return new Color(0.887f, 0.762f, 0.757f);
                case SystemLogType.Shader: return new Color(0.949f, 0.741f, 0.518f);
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

        /// <summary>
        /// The lookup a log goes through. A channel is addressed by name, so this answers for the
        /// framework's channels and the project's alike - the framework's are named for their
        /// <see cref="SystemLogType"/>.
        /// </summary>
        public bool TryGetLogType(string channel, out FlowConsoleLogTypeCVO result)
        {
            if (_logTypeByName == null) RebuildCache();

            if (channel == null)
            {
                result = null;
                return false;
            }

            return _logTypeByName.TryGetValue(channel, out result);
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
        private Dictionary<string, FlowLogProfile> _resolvedProfileByLogType;

        public FlowLogProfile GetResolvedProfile(string channel)
        {
            if (_resolvedProfileByLogType == null) BuildProfileCache();

            if (channel == null) return null;

            _resolvedProfileByLogType.TryGetValue(channel, out var profile);
            return profile;
        }

        public void InvalidateProfileCache()
        {
            _resolvedProfileByLogType = null;
            _profileByName = null;
        }

        private void BuildProfileCache()
        {
            _resolvedProfileByLogType =
                new Dictionary<string, FlowLogProfile>(StringComparer.OrdinalIgnoreCase);
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
                        _resolvedProfileByLogType[logType.Name] = profileData.ToProfile();
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