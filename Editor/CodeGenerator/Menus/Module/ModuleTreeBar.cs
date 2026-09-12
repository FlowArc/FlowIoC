#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The bar over a module tree, wherever one is drawn - the Parent Module bar of the single-file
    /// generators, and the bar Rename Module and Delete Module put over their lists. The Root's
    /// violet, an icon, the bar's name with the pick after it in the yellow the previews use, and
    /// at the right edge the search over the tree, where the eye already is rather than on a row
    /// of its own that would push every list down by a line.
    ///
    /// One class rather than a bar per window, so the six read as one thing and a change to the
    /// search reaches all of them. GeneratorListBar is its green sibling over the lists the reader
    /// fills in, with a plus where this has a search.
    /// </summary>
    internal class ModuleTreeBar
    {
        internal const float HEIGHT = 33f;
        private const float ICON_WIDTH = 35f;

        /// <summary>
        /// The search field's width. Narrow on purpose: a module name is a word or two, and the
        /// bar's own label needs the room to say what was picked.
        /// </summary>
        private const float SEARCH_WIDTH = 160f;

        private SearchField _field;

        private GUIStyle _title;

        /// <summary>
        /// Draws the bar and answers the search text as the reader left it. <paramref name="pick"/>
        /// is the module picked, or null while there is none; <paramref name="search"/> null draws
        /// no field at all, for a bar over a list too short to search.
        /// </summary>
        internal string Draw(string label, string pick, string search)
        {
            EditorGUILayout.Space(10);

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new ModulePanelTheme().Header;

            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(HEIGHT));

            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.Width(ICON_WIDTH), GUILayout.Height(HEIGHT));
            EditorGUILayout.LabelField(Title(label, pick), TitleStyle, GUILayout.Height(HEIGHT));

            if (search != null)
            {
                GUILayout.FlexibleSpace();

                // The field is given its rectangle by hand, centred in a slot the bar's height: laid
                // out, it would sit on the slot's top edge, because a text field takes its own height
                // and a horizontal group aligns what it holds to the top.
                Rect slot = GUILayoutUtility.GetRect(SEARCH_WIDTH, HEIGHT, GUILayout.Width(SEARCH_WIDTH), GUILayout.Height(HEIGHT));
                float height = EditorStyles.toolbarSearchField.fixedHeight > 0f
                    ? EditorStyles.toolbarSearchField.fixedHeight
                    : EditorGUIUtility.singleLineHeight;
                var field = new Rect(slot.x, slot.y + (slot.height - height) * 0.5f, slot.width, height);

                // In the editor's own colour rather than the bar's tint, the way the list bar's plus is.
                GUI.backgroundColor = previous;

                // The toolbar search field the Project window has: a magnifier while it is empty,
                // and once something is typed a cross at its right edge that clears it in one click.
                search = Field.OnToolbarGUI(field, search);
            }

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = previous;

            return search;
        }

        /// <summary>The label alone until something is picked, then the pick after it in yellow.</summary>
        internal string Title(string label, string pick)
        {
            if (string.IsNullOrEmpty(pick)) return label;

            return label + " <color=#ffdd00ff>" + pick + "</color>";
        }

        /// <summary>
        /// Built on use rather than with the fields: SearchField asks GUIUtility for a control id in
        /// its constructor, and a window being rebuilt after a domain reload runs its field
        /// initialisers before there is any GUI to ask - the bar came back null and the window threw
        /// on its first line.
        /// </summary>
        private SearchField Field => _field ??= new SearchField();

        /// <summary>Built on use: EditorStyles is not loaded when a window's fields are.</summary>
        private GUIStyle TitleStyle => _title ??= new GUIStyle(EditorStyles.whiteLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            richText = true
        };
    }
}
#endif