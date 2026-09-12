#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// The first line of a module's card, renamed with the module. Two shapes are in the wild: the
    /// cards written by hand head with the folder name - "# CounterModule" - and the stub Create
    /// Module writes heads with the name without Module - "# WorldPointer" - so both are looked
    /// for, and only as the whole line. Nothing under the heading is read: the rest of the card is
    /// the author's.
    /// </summary>
    internal class ModuleCardHeading
    {
        private const string MARK = "# ";

        internal string Renamed(
            string text, string oldName, string newName, string oldStem, string newStem, out bool changed)
        {
            changed = false;

            if (string.IsNullOrEmpty(text)) return text;

            foreach ((string old, string @new) in new[] {(oldName, newName), (oldStem, newStem)})
            {
                string heading = MARK + old;

                if (!text.StartsWith(heading, StringComparison.Ordinal)) continue;

                string rest = text.Substring(heading.Length);

                if (rest.Length != 0 && rest[0] != '\r' && rest[0] != '\n') continue;

                changed = true;

                return MARK + @new + rest;
            }

            return text;
        }
    }
}
#endif
