#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// The rename pass over one text: every rule in the order it was given, and whether any of
    /// them found something. Order is the caller's - RenameRules puts the deeper namespace before
    /// the one it starts with - and this only runs them.
    /// </summary>
    internal class SourceTextRewriter
    {
        internal string Rewrite(string text, IReadOnlyList<TextRule> rules, out bool changed)
        {
            string result = text;

            foreach (TextRule rule in rules)
                result = rule.Pattern.Replace(result, rule.Replacement);

            changed = result != text;

            return result;
        }
    }
}
#endif
