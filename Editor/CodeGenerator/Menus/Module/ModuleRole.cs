#if UNITY_EDITOR
namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// What a main module's Root roots. The Root inspector takes its colour from the Root's own
    /// name, so this is what puts the word into it: a System Root reads as a System, a Service
    /// Root as a Service, and Core writes the plain Root the generator has always written.
    ///
    /// System comes first because it is what a module written for the game at hand usually is.
    /// The module's own name never carries the word - it says what the module does.
    /// </summary>
    internal enum ModuleRole
    {
        System,
        Service,
        Core
    }
}
#endif
