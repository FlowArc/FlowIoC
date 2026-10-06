#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// The question an update asks when some of the game's files meet a change of the package's:
    /// what the update does without asking, then one row per such file with the reader's choice.
    /// A row starts on the game's side, so an update run without reading the list keeps the
    /// game's work. A modal window rather than a dialog, because a dialog offers one answer for
    /// every file and a CD_Ads full of the game's ad unit ids is not decided the way a prefab the
    /// package fixed is.
    /// </summary>
    internal class ModuleUpdateWindow : EditorWindow
    {
        private static readonly string[] _replaceOrKeep = {"Mine", "Package's"};
        private static readonly string[] _keepOrRemove = {"Keep", "Remove"};

        private string _question;
        private ModuleUpdatePlanEVO _plan;
        private IReadOnlyList<string> _conflicts;
        private readonly HashSet<string> _takeTheirs = new HashSet<string>(StringComparer.Ordinal);
        private Action<ICollection<string>> _onUpdate;
        private Vector2 _scroll;

        internal static void Show(string title, string question, ModuleUpdatePlanEVO plan,
            Action<ICollection<string>> onUpdate)
        {
            var window = CreateInstance<ModuleUpdateWindow>();
            window.titleContent = new GUIContent("Update " + title);
            window._question = question;
            window._plan = plan;
            window._conflicts = plan.Of(ModuleUpdateVerdict.Conflict);
            window._onUpdate = onUpdate;
            window.minSize = new Vector2(600f, 440f);
            window.ShowModalUtility();
        }

        private void OnGUI()
        {
            if (_plan == null)
            {
                Close();
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(_question, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Your files", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("All mine", EditorStyles.miniButtonLeft, GUILayout.Width(90f)))
                _takeTheirs.Clear();

            if (GUILayout.Button("All the package's", EditorStyles.miniButtonRight, GUILayout.Width(120f)))
                _takeTheirs.UnionWith(_conflicts);

            EditorGUILayout.EndHorizontal();

            foreach (string path in _conflicts)
                DrawRow(path);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(90f)))
                Close();

            if (GUILayout.Button("Update", GUILayout.Width(90f)))
            {
                var chosen = new HashSet<string>(_takeTheirs, StringComparer.Ordinal);
                Action<ICollection<string>> onUpdate = _onUpdate;

                // After the window is gone: the update writes files and shows its own report.
                EditorApplication.delayCall += () => onUpdate?.Invoke(chosen);
                Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f);
        }

        private void DrawRow(string path)
        {
            bool unshipped = _plan.Unshipped.Contains(path);

            EditorGUILayout.BeginHorizontal();

            var label = new GUIContent(path, unshipped ? "The package no longer ships this file." : path);
            EditorGUILayout.LabelField(label, GUILayout.MinWidth(200f));

            int choice = _takeTheirs.Contains(path) ? 1 : 0;
            int chosen = GUILayout.Toolbar(choice, unshipped ? _keepOrRemove : _replaceOrKeep,
                EditorStyles.miniButton, GUILayout.Width(170f));

            if (chosen != choice)
            {
                if (chosen == 1)
                    _takeTheirs.Add(path);
                else
                    _takeTheirs.Remove(path);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}

#endif
