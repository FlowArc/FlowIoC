#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// How the Create Module window is to draw the toggle for one of a layout's optional folders -
    /// the Signals holder, the Shared assembly - given the layout and the module type in hand.
    ///
    /// It is here rather than inline in the window because it is the part of that toggle worth
    /// being sure of: three states, arrived at from two inputs, with no IMGUI anywhere near it.
    /// </summary>
    internal class OptionalFolderToggleRule
    {
        /// <summary>
        /// <paramref name="folder"/> is the layout's entry for the folder, or null when the layout
        /// has none. <paramref name="withheldFrom"/> names the module types that are not offered
        /// this folder whatever the layout says - a test module wires other modules' signals
        /// rather than owning a public surface of its own, so it is withheld the Signals holder.
        /// </summary>
        public OptionalFolderToggleState For(
            FolderConfig folder,
            ModuleType moduleType,
            IReadOnlyCollection<ModuleType> withheldFrom)
        {
            if (folder == null)
                return OptionalFolderToggleState.Hidden;

            // Before the mandatory check, deliberately: a layout marking the folder mandatory is
            // describing the folder, not deciding which module types are offered a toggle for it.
            if (withheldFrom != null && withheldFrom.Contains(moduleType))
                return OptionalFolderToggleState.Hidden;

            return folder.IsOptional
                ? OptionalFolderToggleState.Selectable
                : OptionalFolderToggleState.ForcedOn;
        }
    }

    /// <summary>What the window does with the toggle.</summary>
    internal enum OptionalFolderToggleState
    {
        /// <summary>Not drawn at all, and the folder is not created.</summary>
        Hidden,

        /// <summary>Drawn ticked and disabled: the layout does not allow the folder to be left out.</summary>
        ForcedOn,

        /// <summary>Drawn as an ordinary toggle the reader owns.</summary>
        Selectable
    }
}
#endif