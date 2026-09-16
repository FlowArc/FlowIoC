#if UNITY_EDITOR
namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// Numbered because the value is written into the handoff a Create Module run leaves for
    /// itself across the domain reload, and a serialized enum reads back by its number.
    /// </summary>
    internal enum ModuleType
    {
        Main = 0,
        Test = 1,
        Screen = 2
    }
}
#endif