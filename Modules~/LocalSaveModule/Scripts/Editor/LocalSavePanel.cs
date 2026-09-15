#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModulePanels;
using Modules.LocalSaveModule.RootsContexts;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modules.LocalSaveModule.Editor
{
    /// <summary>
    /// The save file as the developer sees it from the Editor: where it is, what state it is in,
    /// what it says, and the two things a developer does to it between play sessions - throw it
    /// away, or write it again under another password. It reads and never edits: a value changed
    /// here would skip the rules the Model exists to keep, and the file is the Model's.
    ///
    /// The password comes off the LocalSaveRoot in the open scene when there is one, so the
    /// panel reads the file the game would read without being told the password twice; a scene
    /// without the Root gets a field instead.
    /// </summary>
    internal class LocalSavePanel : ModulePanel
    {
        private const string MENU_PATH = "Tools/FlowIoC-Modules/Local Save/Panel";

        /// <summary>
        /// Between Tools/FlowIoC, whose items sit around -1300 to -1100, and Tools/FlowIoC-dev at
        /// -1050: a submenu takes its place from its lowest item, so this is what makes
        /// FlowIoC-Modules the second entry under Tools.
        /// </summary>
        private const int MENU_PRIORITY = -1080;

        /// <summary>More than this and the panel shows the head of the file and says so.</summary>
        private const int MAX_SHOWN_CHARACTERS = 64 * 1024;

        private const string PLAYING =
            "The game is running and holds the save in memory; what it writes next would overwrite "
            + "anything done here. Leave play mode first.";

        private readonly LocalSaveFileTools _tools = new LocalSaveFileTools();

        private string _typedPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _contents;
        private string _readError;

        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        private static void Open() => ModulePanelWindow.Open<LocalSavePanel>();

        public override string Title => "Local Save";

        public override string Module => "LocalSaveModule";

        public override string Subtitle => "The save file on this machine";

        public override FlowRole Role => FlowRole.Service;

        public override string HelpPage => "Local Save";

        public override void Draw(ModulePanelPainter painter)
        {
            bool playing = EditorApplication.isPlaying;

            if (playing)
                painter.Warning(PLAYING);

            DrawFile(painter);
            painter.Space();
            DrawPassword(painter);
            painter.Space();
            DrawContents(painter);
            painter.Space();
            DrawActions(painter, playing);
            painter.Space();
            DrawRewrite(painter, playing);
        }

        private void DrawFile(ModulePanelPainter painter)
        {
            painter.Heading("File");
            painter.Field("Path", _tools.Path);

            if (!_tools.Exists)
            {
                painter.Field("State", "no save file yet - nothing has been written on this machine");
                return;
            }

            painter.Field("State", _tools.IsEncrypted ? "encrypted" : "plain JSON");
            painter.Field("Size", Size(_tools.Length));
            painter.Field("Last write", _tools.LastWrite.ToString("yyyy-MM-dd HH:mm:ss"));

            if (_tools.HasTemporary)
            {
                painter.Note(
                    "A .tmp file sits beside the save: a write was interrupted before it could be "
                    + "moved into place. The save itself is the previous one, whole. Reset removes both.");
            }
        }

        private void DrawPassword(ModulePanelPainter painter)
        {
            painter.Heading("Password");

            LocalSaveRootAdapter adapter = AdapterInScene();

            if (adapter != null && !string.IsNullOrEmpty(adapter.Password))
            {
                painter.Field("Source", "LocalSaveRoot in the open scene - " + adapter.gameObject.scene.name);
                painter.Note("The file is read with the password the Root carries, the way the game reads it.");
                return;
            }

            _typedPassword = painter.TextField("Password", _typedPassword, true);

            painter.Note(adapter != null
                ? "The LocalSaveRoot in the open scene carries no password. Type one only to read a file written encrypted."
                : "No LocalSaveRoot in the open scene. Type the password the file was written with; leave it empty for a plain file.");
        }

        private void DrawContents(ModulePanelPainter painter)
        {
            painter.Heading("Contents");

            painter.Actions(
                new ModulePanelAction("Read", Read, _tools.Exists),
                new ModulePanelAction("Clear", () => { _contents = null; _readError = null; }, _contents != null || _readError != null));

            if (_readError != null)
                painter.Warning(_readError);

            if (_contents == null)
                return;

            if (_contents.Length > MAX_SHOWN_CHARACTERS)
            {
                painter.Note($"The file is {Size(_contents.Length)} of text; the first {Size(MAX_SHOWN_CHARACTERS)} is shown.");
                painter.Text(_contents.Substring(0, MAX_SHOWN_CHARACTERS));
            }
            else
            {
                painter.Text(_contents.Length == 0 ? "(empty)" : _contents);
            }
        }

        private void DrawActions(ModulePanelPainter painter, bool playing)
        {
            painter.Heading("Actions");

            painter.Actions(
                new ModulePanelAction("Open folder", OpenFolder),
                new ModulePanelAction("Reset save", ResetSave, _tools.Exists && !playing, true),
                new ModulePanelAction("Clear PlayerPrefs", ClearPlayerPrefs, !playing, true));

            painter.Note(
                "Reset deletes the save file, so the next run starts from the values authored in the "
                + "Editor. Clear PlayerPrefs wipes everything any module keeps there - Haptic's on/off "
                + "choice among them - not only this module's.");
        }

        private void DrawRewrite(ModulePanelPainter painter, bool playing)
        {
            painter.Heading("Rewrite under another password");

            _newPassword = painter.TextField("New password", _newPassword, true);

            painter.Actions(new ModulePanelAction("Rewrite", Rewrite, _tools.Exists && !playing, true));

            painter.Note(
                "Writes the same contents again encrypted with the new password, or plain when it "
                + "is left empty. For the day the password on the Root changes: the save you were "
                + "testing with keeps reading.");
        }

        private void Read()
        {
            _readError = null;

            try
            {
                _contents = _tools.ReadText(Password());
            }
            catch (Exception exception)
            {
                _contents = null;
                _readError = "The file could not be read: " + exception.Message;
            }
        }

        private void OpenFolder()
        {
            if (_tools.Exists)
                EditorUtility.RevealInFinder(_tools.Path);
            else
                EditorUtility.RevealInFinder(_tools.Folder);
        }

        private void ResetSave()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Reset the save file",
                "This deletes\n\n" + _tools.Path + "\n\nThe next run starts from the values authored in the "
                + "Editor. There is no undo.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            _tools.Reset();
            _contents = null;
            _readError = null;
        }

        private void ClearPlayerPrefs()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear PlayerPrefs",
                "This deletes every PlayerPrefs key of this project on this machine - what any module "
                + "keeps there, not only the save module. There is no undo.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        private void Rewrite()
        {
            bool plain = string.IsNullOrEmpty(_newPassword);

            bool confirmed = EditorUtility.DisplayDialog(
                "Rewrite the save file",
                plain
                    ? "The file is written again as plain JSON."
                    : "The file is written again encrypted with the new password. Put the same password on "
                      + "LocalSaveRoot, or the game reads an empty save.",
                "Rewrite",
                "Cancel");

            if (!confirmed)
                return;

            try
            {
                _tools.Rewrite(Password(), _newPassword);
                _readError = null;
                _contents = null;
            }
            catch (Exception exception)
            {
                _readError = "The file could not be rewritten: " + exception.Message;
            }
        }

        /// <summary>The Root's password when the open scene has one, otherwise what was typed.</summary>
        private string Password()
        {
            LocalSaveRootAdapter adapter = AdapterInScene();

            return adapter != null && !string.IsNullOrEmpty(adapter.Password) ? adapter.Password : _typedPassword;
        }

        private static LocalSaveRootAdapter AdapterInScene() =>
            Object.FindFirstObjectByType<LocalSaveRootAdapter>(FindObjectsInactive.Include);

        private static string Size(long bytes) =>
            bytes < 1024 ? bytes + " B" : (bytes / 1024f).ToString("0.#") + " KB";
    }
}

#endif
