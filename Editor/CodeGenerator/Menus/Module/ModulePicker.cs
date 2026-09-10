#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The list a generator picks a parent module from: the project's modules as a tree, a module
    /// under the module it lives in, drawn the way Module Scanner draws them so the two read as
    /// one thing. Every row is always shown - what the reader is here to see is which modules sit
    /// inside which, and a foldout would hide exactly that behind a click.
    ///
    /// The whole row is the button. A row the rules say cannot host what is being created is
    /// drawn dim, takes no click and does not light up under the pointer: a highlight promises a
    /// click that does something, and this one would not.
    /// </summary>
    internal class ModulePicker
    {
        private const string MODULE_SUFFIX = "Module";

        /// <summary>
        /// The row over the whole tree: the modules folder itself, which every top level module
        /// hangs from. Its path is what ModuleGenerator compares a pick against to tell a top
        /// level module from a nested one, so it is built the way the generator builds it.
        /// </summary>
        private const string ROOT_LABEL = "Modules";

        private readonly object _rootKey = new object();
        private const float BADGE_WIDTH = 78f;

        /// <summary>The dot at the row's edge that says this is the pick.</summary>
        private const string PICK_MARK = "●";

        /// <summary>
        /// The column the pick mark sits in, before the tree, on every row alike - so a row without
        /// the mark has no empty slot between its guide line and its name. As narrow as the dot
        /// needs: wider read as a gap on every row that carries no dot.
        /// </summary>
        private const float LEAD_WIDTH = 8f;

        /// <summary>The dot at a size that fits the column: the label glyph is twice what the column has room for.</summary>
        private const int PICK_MARK_SIZE = 7;

        /// <summary>
        /// A row that cannot be picked, taken down rather than tinted - it is out of the question
        /// rather than in a state.
        /// </summary>
        private const float DIM_ALPHA = 0.12f;

        private readonly FlowRowPainter _rows = new FlowRowPainter();
        private readonly FlowTreePainter _tree;
        private readonly ModuleRoleBadge _badge = new ModuleRoleBadge();
        private readonly FlowPalette _palette = new FlowPalette();

        private readonly List<ModuleTreeRowEVO<ModulePickEVO>> _entries;

        private GUIStyle _mark;

        /// <summary>
        /// What kind of module the pick is - what decides the folder layout the file is written
        /// into, and whether it is wrapped in UNITY_EDITOR. Main while the pick is the modules
        /// folder or nothing yet. Read back after Draw, which is where the pick is known.
        /// </summary>
        internal ModuleKind PickedKind { get; private set; } = ModuleKind.Main;

        /// <summary>See ROOT_LABEL. Read on use: Application.dataPath is not there when the fields are.</summary>
        private string RootPath => System.IO.Path.Combine(Application.dataPath, ROOT_LABEL);

        internal ModulePicker(ModuleRegistry registry)
        {
            _tree = new FlowTreePainter(_rows, LEAD_WIDTH);
            _entries = new ModuleTree().Build(new ModulePickFactory().From(registry));
        }

        /// <summary>
        /// Draws the tree and takes the click. The pick lands in the two strings the generators
        /// already work from: the module folder, and the module's name without its suffix.
        /// </summary>
        /// <param name="parentModulePath">The folder of the pick, in the shape ModulePickEVO.Path describes.</param>
        /// <param name="selectedModuleName">The pick's name without its Module suffix; empty for the root.</param>
        /// <param name="canHost">Whether a module of this kind may hold what is being created.</param>
        /// <param name="rootSelectable">
        /// Whether the modules folder itself, the row the whole tree hangs from, is a pick. It is
        /// for the one generator that can put what it writes there - a top level module - and for
        /// the rest the row is there to be hung from and nothing else.
        /// </param>
        internal void Draw(
            ref string parentModulePath, ref string selectedModuleName, Func<ModuleKind, bool> canHost,
            bool rootSelectable)
        {
            _tree.Begin();

            PickedKind = ModuleKind.Main;

            Rect rootRect = DrawRow(ROOT_LABEL, null, 0, rootSelectable, parentModulePath == RootPath, out bool rootPicked);

            _tree.Hang(_rootKey, rootRect, 0, null);

            if (rootPicked)
            {
                parentModulePath = RootPath;
                selectedModuleName = string.Empty;
            }

            foreach (ModuleTreeRowEVO<ModulePickEVO> entry in _entries)
            {
                ModulePickEVO pick = entry.Row;
                int depth = entry.Depth + 1;

                bool selected = parentModulePath == pick.Path;

                if (selected) PickedKind = pick.Kind;

                Rect rect = DrawRow(pick.Name, pick, depth, canHost(pick.Kind), selected, out bool picked);

                _tree.Hang(entry, rect, depth, entry.Parent ?? _rootKey);

                if (!picked) continue;

                parentModulePath = pick.Path;
                selectedModuleName = TrimModuleSuffix(pick.Name);
                PickedKind = pick.Kind;
            }
        }

        /// <summary>
        /// One row: the pick mark at the edge, the name, and what kind of module it is. The tint is the
        /// window's own chrome rather than a status colour - nothing here is right or wrong, one
        /// row is simply the pick - and the pick is filled hardest so it reads as such from across
        /// the room.
        /// </summary>
        private Rect DrawRow(
            string label, ModulePickEVO pick, int depth, bool selectable, bool selected, out bool picked)
        {
            Rect rect = _rows.RowInset();
            Color accent = _palette.Chrome(EditorGUIUtility.isProSkin);
            bool hovered = selectable && _rows.IsHovered(rect);

            if (!selectable) _rows.Darken(rect, DIM_ALPHA);
            else if (selected) _rows.Paint(rect, accent, FlowRowPainter.HEADING_ALPHA);
            else _rows.Paint(rect, accent, hovered ? FlowRowPainter.FILL_ALPHA : FlowRowPainter.QUIET_ALPHA);

            if (selected)
            {
                Color previous = GUI.color;
                GUI.color = accent;
                GUI.Label(new Rect(_tree.LeadX(rect), rect.y, LEAD_WIDTH, rect.height), PICK_MARK, Mark);
                GUI.color = previous;
            }

            float x = _tree.TextX(rect, depth);

            float room = rect.xMax - BADGE_WIDTH - 10f - x;

            GUI.Label(new Rect(x, rect.y, room, rect.height), label,
                selectable ? _rows.Name(hovered || selected) : _rows.Mini(false));

            if (pick != null)
            {
                GUI.Label(new Rect(rect.xMax - BADGE_WIDTH - 6f, rect.y, BADGE_WIDTH, rect.height),
                    _badge.Text(pick), _badge.Style(pick, _rows, hovered));
            }

            picked = false;

            if (!selectable) return rect;

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            // Drawn last and painting nothing, so it takes the click without covering the row.
            picked = GUI.Button(rect, GUIContent.none, GUIStyle.none);

            return rect;
        }

        /// <summary>Built on use: EditorStyles is not loaded when the picker's fields are.</summary>
        private GUIStyle Mark => _mark ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = PICK_MARK_SIZE
        };

        private string TrimModuleSuffix(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName) || !moduleName.EndsWith(MODULE_SUFFIX)) return moduleName;

            return moduleName.Substring(0, moduleName.Length - MODULE_SUFFIX.Length).Trim();
        }
    }
}

#endif