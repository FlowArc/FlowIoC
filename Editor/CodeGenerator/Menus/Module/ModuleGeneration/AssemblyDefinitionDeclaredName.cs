#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The name an asmdef declares for itself, renamed in place. It is a field of its own rather
    /// than another reference: the references block lists what the assembly sees, and this is
    /// what it is called, which is what every other asmdef's entry has to match.
    /// </summary>
    internal class AssemblyDefinitionDeclaredName
    {
        internal string Rename(string asmdefContent, string oldName, string newName, out bool renamed)
        {
            renamed = false;

            if (string.IsNullOrEmpty(asmdefContent) || string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                return asmdefContent;

            var pattern = new Regex("(\"name\"\\s*:\\s*\")" + Regex.Escape(oldName) + "(\")");

            if (!pattern.IsMatch(asmdefContent)) return asmdefContent;

            renamed = true;

            return pattern.Replace(asmdefContent, "${1}" + newName.Replace("$", "$$") + "${2}", 1);
        }
    }
}
#endif
