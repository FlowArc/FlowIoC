#if UNITY_EDITOR
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// Which existing module kinds can host a newly-created module of a given kind. A new Main
    /// module cannot nest under a Screen or Test module; a new Screen module belongs to the module
    /// whose feature it shows, so it goes under a Main or Sub module and never under a Screen or a
    /// Test one; a new Test module cannot nest under another Test module - it attaches to the
    /// module it tests, not to a peer test module. A new Sub module has no restriction at all.
    /// </summary>
    internal class ModuleSelectionRules
    {
        public bool CanHost(ModuleKind created, ModuleKind parent)
        {
            switch (created)
            {
                case ModuleKind.Test when parent == ModuleKind.Test:
                case ModuleKind.Screen when parent == ModuleKind.Screen || parent == ModuleKind.Test:
                case ModuleKind.Main when parent == ModuleKind.Screen || parent == ModuleKind.Test:
                    return false;
                default:
                    return true;
            }
        }
    }
}
#endif