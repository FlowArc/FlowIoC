#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    /// <summary>
    /// The bar over a list the reader fills in - Execute Parameters, Injectables, Actions - in the
    /// shape of the Parent Module bar but green, with the one thing the list needs at its right
    /// edge: a square the bar's height with a plus in it. The heading and the button used to be
    /// two rows, a bold label over a button as wide as the window; one bar says what the list is
    /// and where a row comes from in the height of the heading alone.
    /// </summary>
    internal class GeneratorListBar
    {
        private const float HEIGHT = 33f;
        private const float ICON_WIDTH = 35f;

        private GUIStyle _title;

        /// <summary>
        /// Draws the bar and answers whether the plus was pressed. <paramref name="canAdd"/> is
        /// false when the list is full - a function past its last parameter - and the plus is
        /// then drawn disabled rather than left out, so the bar keeps its shape.
        /// </summary>
        public bool Draw(string title, string tooltip, bool canAdd = true)
        {
            EditorGUILayout.Space(10);

            Color previous = GUI.backgroundColor;

            // The Connector green given the lift the Parent Module bar gives its violet, so the two
            // bars read as one family. The Add buttons' own green is a multiplier meant for a button
            // face, and on the box a bar is made of it came out as a dark, muddy strip.
            GUI.backgroundColor = new ModulePanelTheme().HeaderFor(FlowRole.Connector);

            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(HEIGHT));

            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.Width(ICON_WIDTH), GUILayout.Height(HEIGHT));
            EditorGUILayout.LabelField(title, Title, GUILayout.Height(HEIGHT));

            GUI.backgroundColor = previous;

            // Square, the height of the bar it sits on, and an icon rather than a word: the bar
            // already says what the list is, and the button is the one thing on it to press.
            EditorGUI.BeginDisabledGroup(!canAdd);

            bool pressed = GUILayout.Button(new GUIContent(PlusIcon(), tooltip), GUILayout.Width(HEIGHT), GUILayout.Height(HEIGHT));

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            return pressed;
        }

        /// <summary>
        /// The minus at the end of a row under the bar: as wide as the plus and ending where the
        /// plus ends, so the two stand in one column. The plus sits inside the bar's box and is
        /// inset by the box's padding; the row has no box, so the same padding is put after the
        /// minus by hand.
        /// </summary>
        public bool DrawRemove()
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new ModulePanelTheme().ActionRemove;

            bool pressed = GUILayout.Button("-", GUILayout.Width(HEIGHT));

            GUI.backgroundColor = previous;

            GUILayout.Space(EditorStyles.helpBox.padding.right);

            return pressed;
        }

        /// <summary>
        /// The plus, under whichever name this Editor knows it by. Unity has renamed the built-in
        /// icon more than once, so the fallbacks matter more than which one wins.
        /// </summary>
        private Texture PlusIcon()
        {
            foreach (string name in new[] {"Toolbar Plus", "d_Toolbar Plus", "Toolbar Plus More"})
            {
                Texture icon = EditorGUIUtility.IconContent(name)?.image;

                if (icon != null) return icon;
            }

            return null;
        }

        /// <summary>Built on use: EditorStyles is not loaded when a window's fields are.</summary>
        private GUIStyle Title => _title ??= new GUIStyle(EditorStyles.whiteLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            richText = true
        };
    }
}

#endif