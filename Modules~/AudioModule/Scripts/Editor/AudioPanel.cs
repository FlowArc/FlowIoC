#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModulePanels;
using Modules.AudioModule.Constants;
using Modules.AudioModule.Controllers;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Data.UnityObjects;
using Modules.AudioModule.Shared.Data.ValueObjects;
using UnityEditor;
using UnityEngine;

namespace Modules.AudioModule.Editor
{
    /// <summary>
    /// The project's sound as the developer sees it from the Editor. The list on the left is every
    /// module, sub and test modules under their parents, marked where the module already has its
    /// keys and its bank; picking one shows what it has and what it lacks, and gives it the two
    /// halves when it has neither. Overview holds every bank and the player's stored choices on
    /// this machine. It reads and resets; a bank's rows are edited in the bank's own Inspector.
    /// </summary>
    internal class AudioPanel : ModulePanel
    {
        private const string MENU_PATH = "Tools/FlowIoC-Modules/Audio/Panel";

        /// <summary>Keeps Tools/FlowIoC-Modules the second entry under Tools, after Tools/FlowIoC.</summary>
        private const int MENU_PRIORITY = -1080;

        private const string INSTALLED = "installed";
        private const string PARTIAL = "half";

        private readonly AudioKeysScaffolder _scaffolder = new AudioKeysScaffolder();

        /// <summary>The module folders, read once and again after Refresh or a scaffold - not every repaint.</summary>
        private List<string> _modules;

        private IReadOnlyList<AudioKey> _declared;

        /// <summary>The picked module's folder, or null for Overview.</summary>
        private string _selected;

        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        private static void Open() => ModulePanelWindow.Open<AudioPanel>();

        public override string Title => "Audio";

        public override string Module => "AudioModule";

        public override string Subtitle => "Which modules play sound, their banks, and the player's settings";

        public override FlowRole Role => FlowRole.Service;

        public override string HelpPage => "Audio Module";

        public override bool HasSidebar => true;

        public override ModulePanelAction? BarAction => new ModulePanelAction("Refresh", Forget);

        public override void DrawSidebar(ModulePanelSidebarPainter sidebar)
        {
            sidebar.Item("Overview", _selected == null, null, () => _selected = null);
            sidebar.Space();
            sidebar.Heading("Modules");

            foreach (string folder in Modules())
            {
                string picked = folder;
                string label = new string(' ', _scaffolder.DepthOf(folder) * 4) + Path.GetFileName(folder);
                sidebar.Item(label, _selected == folder, Marker(_scaffolder.StatusOf(folder)), () => _selected = picked);
            }
        }

        public override void Draw(ModulePanelPainter painter)
        {
            if (_selected == null || !Directory.Exists(_selected))
            {
                _selected = null;
                DrawBanks(painter);
                painter.Space();
                DrawPlayerSettings(painter);
                return;
            }

            DrawModule(painter, _selected);
        }

        private void DrawModule(ModulePanelPainter painter, string folder)
        {
            string moduleName = Path.GetFileName(folder);
            AudioKeysStatus status = _scaffolder.StatusOf(folder);

            painter.Heading(moduleName);
            painter.Field("Folder", folder);
            painter.Field("Keys", _scaffolder.HasKeys(folder) ? _scaffolder.KeyFileOf(folder) : "not yet");
            painter.Field("Bank", _scaffolder.HasBank(folder) ? _scaffolder.BankPathOf(folder) : "not yet");

            if (status != AudioKeysStatus.Installed)
            {
                painter.Space();
                painter.Actions(
                    $"Writes {AudioKeysScaffolder.KEYS_FOLDER}/ with its asmref and a key file, and {AudioKeysScaffolder.BANK_FOLDER}/{AudioKeysScaffolder.BANK_FILE}, "
                    + "all inside the module, and adds Modules.Audio and Modules.Audio.Shared to its assembly. What is already there is kept.",
                    ModulePanelAction.Add(status == AudioKeysStatus.Partial ? "Add what is missing" : "Add audio keys and a bank",
                        () => Scaffold(folder), !EditorApplication.isPlaying));
                return;
            }

            var bank = AssetDatabase.LoadAssetAtPath<CD_AudioBank>(_scaffolder.BankPathOf(folder));
            List<AudioKey> declared = DeclaredFor(moduleName);
            var rows = new HashSet<string>();

            if (bank != null)
            {
                foreach (AudioClipCVO sound in bank.Sounds)
                {
                    if (sound != null && !string.IsNullOrEmpty(sound.Key))
                        rows.Add(sound.Key);
                }
            }

            painter.Space();
            painter.Heading("Sounds");
            painter.Field("Declared keys", declared.Count.ToString());
            painter.Field("Bank rows",
                bank != null ? $"{bank.Sounds.Count}, {(bank.PreloadAtBoot ? "loads at start" : "loaded by LoadBank")}" : "the bank does not load");

            foreach (AudioKey key in declared)
            {
                if (!rows.Contains(key.Id))
                    painter.Warning($"{key} is declared but has no row in the bank, so it plays nothing.");
            }

            var declaredIds = new HashSet<string>();

            foreach (AudioKey key in declared)
                declaredIds.Add(key.Id);

            foreach (string row in rows)
            {
                if (!declaredIds.Contains(row))
                    painter.Warning($"The bank has a row for {row}, which no key in {AudioKeysScaffolder.KEYS_FOLDER} declares.");
            }

            painter.Actions(
                new ModulePanelAction("Open key file", () => Open(_scaffolder.KeyFileOf(folder))),
                new ModulePanelAction("Select bank", () => Select(_scaffolder.BankPathOf(folder))));
        }

        private void DrawBanks(ModulePanelPainter painter)
        {
            painter.Heading("Banks");

            string[] guids = AssetDatabase.FindAssets("t:" + nameof(CD_AudioBank));

            if (guids.Length == 0)
            {
                painter.Note("No module has a bank yet. Pick one on the left to give it sound.");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var bank = AssetDatabase.LoadAssetAtPath<CD_AudioBank>(path);

                if (bank == null)
                    continue;

                string module = string.IsNullOrEmpty(bank.Module) ? "(no module)" : bank.Module;
                painter.Field(module, $"{bank.Sounds.Count} sounds, {(bank.PreloadAtBoot ? "loads at start" : "loaded by LoadBank")} - {path}");
            }
        }

        private void DrawPlayerSettings(ModulePanelPainter painter)
        {
            painter.Heading("Player settings on this machine");

            painter.Field("Music", Describe(AudioConstants.PREFS_MUSIC_ENABLED, AudioConstants.PREFS_MUSIC_VOLUME));
            painter.Field("Sound", Describe(AudioConstants.PREFS_SFX_ENABLED, AudioConstants.PREFS_SFX_VOLUME));

            painter.Actions(new ModulePanelAction("Reset to on and full", ResetPlayerSettings, !EditorApplication.isPlaying, destructive: true));
        }

        private List<string> Modules() => _modules ??= _scaffolder.FindModuleFolders();

        private List<AudioKey> DeclaredFor(string moduleName)
        {
            _declared ??= new ListDeclaredKeysFunction().Execute();

            var keys = new List<AudioKey>();

            foreach (AudioKey key in _declared)
            {
                if (key.Bank == moduleName)
                    keys.Add(key);
            }

            return keys;
        }

        private void Scaffold(string folder)
        {
            _scaffolder.Scaffold(folder);
            Forget();
        }

        /// <summary>Drops what was read, so the next repaint reads the folders and the keys again.</summary>
        private void Forget()
        {
            _modules = null;
            _declared = null;
        }

        private static string Marker(AudioKeysStatus status)
        {
            switch (status)
            {
                case AudioKeysStatus.Installed: return INSTALLED;
                case AudioKeysStatus.Partial: return PARTIAL;
                default: return null;
            }
        }

        private static void Open(string path)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

            if (script != null)
                AssetDatabase.OpenAsset(script);
        }

        private static void Select(string path)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static string Describe(string enabledKey, string volumeKey)
        {
            if (!PlayerPrefs.HasKey(enabledKey) && !PlayerPrefs.HasKey(volumeKey))
                return "nothing stored - on, full";

            bool on = PlayerPrefs.GetInt(enabledKey, 1) == 1;
            return $"{(on ? "on" : "off")}, {Mathf.RoundToInt(PlayerPrefs.GetFloat(volumeKey, 1f) * 100f)}%";
        }

        private static void ResetPlayerSettings()
        {
            PlayerPrefs.DeleteKey(AudioConstants.PREFS_MUSIC_ENABLED);
            PlayerPrefs.DeleteKey(AudioConstants.PREFS_MUSIC_VOLUME);
            PlayerPrefs.DeleteKey(AudioConstants.PREFS_SFX_ENABLED);
            PlayerPrefs.DeleteKey(AudioConstants.PREFS_SFX_VOLUME);
            PlayerPrefs.Save();
        }
    }
}

#endif