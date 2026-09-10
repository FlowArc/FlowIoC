#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// One row of a module list in the place the tree puts it: the module itself, how deep
    /// inside other modules it sits, the entry it hangs from and everything drawn under it.
    /// </summary>
    internal class ModuleTreeRowEVO<T> where T : IModuleTreeItem
    {
        internal T Row { get; set; }

        /// <summary>How many modules this one sits inside. A top level module is at zero.</summary>
        internal int Depth { get; set; }

        /// <summary>
        /// The entry of the module this one lives in, or null at the top. The guide line a row is
        /// drawn with runs from that entry's row down to this one, so a window needs the entry
        /// rather than the name - it is the entry's rect it draws from.
        /// </summary>
        internal ModuleTreeRowEVO<T> Parent { get; set; }

        /// <summary>
        /// Every entry inside this one, in the order they are drawn. Module Scanner reads it to
        /// keep a green parent over a red child under "Only issues", and Delete Module to keep a
        /// module whose child matched the search and to name what a deletion takes with it.
        /// </summary>
        internal List<ModuleTreeRowEVO<T>> Descendants { get; } = new List<ModuleTreeRowEVO<T>>();
    }
}

#endif
