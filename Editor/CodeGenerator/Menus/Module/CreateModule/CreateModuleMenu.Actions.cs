#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule
{
    internal partial class CreateModuleMenu
    {
        private void DisplayActionsSection()
        {
            if (_actionsBar.Draw(ACTIONS_LABEL, ADD_ACTION))
                _actionNames.Add(NEW_ACTION);

            MeasureActionListHeight();

            // The list is the one part of the form that grows with the window. Everything above it
            // is fixed - the panels have a height of their own and the screen settings are as tall
            // as their fields - so the room a taller window adds goes to the actions, which is the
            // section that lengthens as the module is described.
            _actionScrollPosition = EditorGUILayout.BeginScrollView(
                _actionScrollPosition,
                GUILayout.MinHeight(_actionListHeight),
                GUILayout.MaxHeight(_actionListHeight));

            // The row that is pressed is noted and dropped once the list has been drawn. Removing
            // it where the button is - and leaving the loop from inside its row, as this did -
            // ends the frame with a horizontal group still open, which IMGUI reports as a
            // mismatched layout group for every repaint that follows.
            int removeAt = -1;

            for (int ii = 0; ii < _actionNames.Count; ii++)
            {
                EditorGUILayout.BeginHorizontal();

                _actionNames[ii] = EditorGUILayout.TextField(_actionNames[ii]);

                if (_actionsBar.DrawRemove())
                    removeAt = ii;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (removeAt >= 0)
                _actionNames.RemoveAt(removeAt);
        }

        /// <summary>
        /// How much of the form's viewport is left under the Add Action button, which is what the
        /// list is drawn at. Both halves of the sum are read on a repaint and used on the layout
        /// pass that follows: GUILayout fixes an element's height while it is laying the form out,
        /// so a height measured mid-repaint would be a frame too late to be obeyed. The window is
        /// asked to repaint whenever the answer changes, so dragging its edge settles at once
        /// rather than on the next mouse move.
        /// </summary>
        private void MeasureActionListHeight()
        {
            if (Event.current.type != EventType.Repaint) return;

            float top = GUILayoutUtility.GetLastRect().yMax;

            float measured = Mathf.Max(
                ACTION_LIST_MIN_HEIGHT, _formViewportHeight - top - ACTION_LIST_BOTTOM_PADDING);

            if (Mathf.Approximately(measured, _actionListHeight)) return;

            _actionListHeight = measured;
            Repaint();
        }
    }
}
#endif