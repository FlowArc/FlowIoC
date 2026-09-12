#if UNITY_EDITOR
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The scroll a module tree sits in: as tall as its rows up to a dozen of them, and a scroll
    /// bar past that. A tree drawn straight into the window grows a row per module, and the
    /// windows that ask for something under the tree - a name, a preview, a list of injectables -
    /// pushed all of it further down with every module the project gained.
    ///
    /// The height is the rows' own rather than a fixed box, so a project with four modules is not
    /// shown four rows and a field of nothing. It is estimated from the row count for the first
    /// pass and read off the drawn rows from then on, because the Layout pass has no rects to
    /// measure and the two passes have to agree on the height within a frame.
    /// </summary>
    internal class ModuleTreeScroll
    {
        internal const int MAX_ROWS = 12;

        /// <summary>The air EditorGUILayout puts between two control rects, and above the first.</summary>
        private const float SPACING = 2f;

        private Vector2 _position;
        private float _measured;
        private int _measuredRows = -1;
        private int _rows;

        /// <summary><paramref name="rows"/> is how many rows the tree is about to draw, the folder row included.</summary>
        internal void Begin(int rows)
        {
            _rows = rows;

            float estimate = rows * (FlowRowPainter.ROW_HEIGHT + SPACING) + SPACING;
            float content = rows == _measuredRows && _measured > 0f ? _measured : estimate;
            float height = Mathf.Min(content, MAX_ROWS * (FlowRowPainter.ROW_HEIGHT + SPACING) + SPACING);

            _position = EditorGUILayout.BeginScrollView(_position, GUILayout.Height(height));
            EditorGUILayout.BeginVertical();
        }

        internal void End()
        {
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                _measured = GUILayoutUtility.GetLastRect().height;
                _measuredRows = _rows;
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
