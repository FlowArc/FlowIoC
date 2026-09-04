#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Root;
using FlowIoC.ScreenModule.Data;

namespace FlowIoC.Editor.ScreenScanner
{
    /// <summary>
    /// Turns the Roots it is handed into the panel's rows. It does not find its own Roots: the
    /// window asks the open scenes for them, which keeps this class free of what happens to be
    /// open and lets a test describe exactly the Roots it means.
    /// </summary>
    internal class ScreenScannerRunner
    {
        private readonly ScreenSubContextDeclarations _declarations;

        internal ScreenScannerRunner(ScreenSubContextDeclarations declarations)
        {
            _declarations = declarations;
        }

        internal List<ScreenRowEVO> Rows(IEnumerable<RootBase> roots)
        {
            List<ScreenRowEVO> rows = new List<ScreenRowEVO>();

            if (roots == null)
                return rows;

            foreach (RootBase root in roots)
            {
                if (root == null || root.SubContextTypes == null)
                    continue;

                for (int index = 0; index < root.SubContextTypes.Count; index++)
                {
                    SubContextData entry = root.SubContextTypes[index];
                    Type contextType = _declarations.ResolveType(entry.ContextFullName);

                    if (!_declarations.IsScreenContext(contextType))
                        continue;

                    rows.Add(RowFor(root, index, entry, contextType));
                }
            }

            return rows;
        }

        private ScreenRowEVO RowFor(RootBase root, int index, SubContextData entry, Type contextType)
        {
            _declarations.TryRead(contextType, out ScreenCVO declaration, out string error);

            return new ScreenRowEVO
            {
                Root = root,
                EntryIndex = index,
                ContextFullName = entry.ContextFullName,
                ContextName = entry.ContextName,
                SceneName = root.gameObject.scene.name,
                Declaration = declaration,
                Effective = Effective(entry, declaration),
                IsOverridden = entry.OverrideScreen,
                DeclarationError = error
            };
        }

        /// <summary>
        /// The editor's answer to what ScreenSubContextBase.Resolved computes at runtime. Load
        /// always comes from the declaration, which is why an override on an unreadable
        /// declaration has no load key.
        /// </summary>
        private ScreenCVO Effective(SubContextData entry, ScreenCVO declaration)
        {
            if (!entry.OverrideScreen)
                return declaration;

            return new ScreenCVO
            {
                Load = declaration?.Load ?? default,
                ManagerId = entry.ScreenManagerId,
                Layer = entry.ScreenLayer,
                Tag = entry.ScreenTag,
                HasShowAnimation = entry.ScreenHasShowAnimation,
                HasHideAnimation = entry.ScreenHasHideAnimation
            };
        }
    }
}
#endif
