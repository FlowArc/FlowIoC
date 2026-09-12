#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    /// <summary>
    /// The frame the four single-file generators stand in: everything the window asks for scrolls,
    /// and the button that writes the file does not.
    ///
    /// Each window used to lay its sections out one under the other with no scroll of its own, and
    /// two of those sections grow - a row per injectable, a row per parameter. Adding enough of
    /// them pushed the module list and the action button past the bottom of the window with nothing
    /// to scroll them back, so the window could be filled in and then not used.
    ///
    /// The scroll position lives here rather than in each window, so a window has one scrollbar
    /// rather than one per list - a list with its own scroll inside a scrolling page traps the
    /// wheel over whichever one the pointer happens to be on.
    /// </summary>
    internal class GeneratorWindowBody
    {
        /// <summary>
        /// The action button's height, which is the 40 Create Module's button has carried all
        /// along. The four windows here are the same tool at a smaller scale and read as one set.
        /// </summary>
        private const float BUTTON_HEIGHT = 40f;

        /// <summary>
        /// What the action button is held back from the left, the right and the bottom by - the
        /// same number on all three, which is what Create Module's button gets from the box it sits
        /// in and what makes a button look placed rather than stuck to the edge.
        /// </summary>
        private const float BUTTON_INSET = 8f;

        /// <summary>
        /// What the body leaves for the footer: the button, its inset below, and the same gap again
        /// above it. It is a constant rather than a measurement because the footer is drawn after
        /// the body, and the body has to be given its height before that.
        /// </summary>
        private const float FOOTER_HEIGHT = BUTTON_HEIGHT + BUTTON_INSET * 2f;

        /// <summary>
        /// The floor the body stops shrinking at. Past it the window is smaller than anything can
        /// usefully be drawn in, and the body scrolls rather than squeezing the footer out.
        /// </summary>
        private const float MIN_BODY_HEIGHT = 80f;

        private Vector2 _scrollPosition;

        /// <summary>
        /// Where the header bar ended, remembered from the last Repaint. GUILayoutUtility answers
        /// with a placeholder during the Layout pass, so reading it there would give the two passes
        /// different heights and make the window flicker; the header is a fixed height, so last
        /// frame's answer is this frame's.
        /// </summary>
        private float _headerBottom = 52f;

        /// <summary>
        /// The height is explicit rather than <c>ExpandHeight</c>, which is what the first attempt
        /// at this used. A scroll view reports its content's height as its own minimum, so a list
        /// long enough pushed the layout past the bottom of the window and took the button with it
        /// - the very thing the scroll was added to prevent. Told exactly how tall to be, it
        /// scrolls instead of growing.
        ///
        /// The height the window draws into is <c>position.height</c>, and nothing has to be taken
        /// off it for the tab: a window's <c>rootVisualElement.contentRect</c> is exactly that tall
        /// and sits at an offset inside its container. <c>Screen.height</c> is that container, which
        /// is 26 points taller on a floating window - reading it here made the body overshoot and
        /// pushed the button off the bottom, and taking a guess at the tab instead left it hanging
        /// short of the edge.
        /// </summary>
        internal void Begin(EditorWindow window)
        {
            if (Event.current.type == EventType.Repaint)
                _headerBottom = GUILayoutUtility.GetLastRect().yMax;

            float available = window.position.height - _headerBottom - FOOTER_HEIGHT;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(Mathf.Max(available, MIN_BODY_HEIGHT)));
            EditorGUILayout.BeginVertical("box");
        }

        internal void End()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// The window's action button, given its rectangle outright rather than laid out after the
        /// body. Laid out, where it lands is whatever margins the button style and the groups
        /// around it happen to carry - which is how it first came to sit short of the bottom edge
        /// by a gap nobody chose. Here the same inset is written on the left, the right and the
        /// bottom, and that is what the reader sees.
        ///
        /// <c>GUI.Button</c> rather than <c>GUILayout.Button</c>, because a layout button would add
        /// its own margin inside whatever rect it was given and put the arithmetic back.
        /// </summary>
        internal bool FooterButton(EditorWindow window, string label, bool disabled, Color background) =>
            FooterButton(window, new GUIContent(label), disabled, background);

        /// <summary>The same button with an icon before its label - Rename Module's warning sign.</summary>
        internal bool FooterButton(EditorWindow window, GUIContent label, bool disabled, Color background)
        {
            var rect = new Rect(BUTTON_INSET,
                window.position.height - BUTTON_INSET - BUTTON_HEIGHT,
                window.position.width - BUTTON_INSET * 2f,
                BUTTON_HEIGHT);

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = background;
            EditorGUI.BeginDisabledGroup(disabled);

            bool pressed = GUI.Button(rect, label);

            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = previous;

            return pressed;
        }
    }
}
#endif