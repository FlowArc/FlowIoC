#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The word a module type puts on the end of a module's name before "Module": a Test module
    /// named "Player" is PlayerTestModule, a Screen module MainScreenModule, a Main module just
    /// PlayerModule.
    ///
    /// The rule used to live in the Create Module window, which appended the suffix before it
    /// called the generator - so the generator itself never knew it, and a call that skipped the
    /// window and named a test module "Ads" wrote zTestModules/AdsModule with the parent's own
    /// assembly name. The generator applies it now, and the window only shows it.
    ///
    /// A name that already ends in the suffix is left as it is: the window's preview shows the
    /// suffixed name and a caller may well pass what it read there. A name ending in "Test" is
    /// a test module's name in this framework whatever the type says - ModuleAssemblyName reads
    /// the same suffix off the folder - so nothing is lost by not doubling it.
    /// </summary>
    internal class ModuleTypeSuffix
    {
        internal string For(ModuleType type)
        {
            switch (type)
            {
                case ModuleType.Main:
                    return string.Empty;
                case ModuleType.Test:
                    return "Test";
                case ModuleType.Screen:
                    return "Screen";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        internal string Apply(string moduleName, ModuleType type)
        {
            string name = moduleName?.Trim() ?? string.Empty;
            string suffix = For(type);

            if (suffix.Length == 0 || name.EndsWith(suffix, StringComparison.Ordinal))
                return name;

            return name + suffix;
        }
    }
}
#endif
