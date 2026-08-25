#if UNITY_EDITOR

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// Where a module sits in the tree. This used to travel as the strings "Main", "Sub",
    /// "Screen" and "Test" written into _module_info.txt and parsed back out with
    /// Enum.TryParse, which silently fell back to Main on a typo.
    /// </summary>
    internal enum ModuleKind
    {
        Main,
        Sub,
        Screen,
        Test
    }
}

#endif
