#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// The text rules a plan comes to, in the order they have to run.
    ///
    /// Namespaces go deepest first: once Modules.CounterModule has become Modules.TimerModule, the
    /// token Modules.CounterModule.CounterTestModule is no longer in the text to be found, so the
    /// nested one is replaced while its parent is still spelled the old way. Identifiers go longest
    /// first for the same reason, although the word boundary already keeps CounterServiceSignals
    /// out of CounterServiceSignalsIncoming. The log constants sit between: they are tokens of their
    /// own and overlap with neither.
    ///
    /// The screen address is a literal inside the screen's own context, and a name a game may well
    /// use as a string elsewhere for something else - so it is only in the rules for the files
    /// under the module folder.
    /// </summary>
    internal class RenameRules
    {
        /// <summary>The rules for every source file in the project.</summary>
        internal List<TextRule> Global(ModuleRenamePlanEVO plan)
        {
            var rules = new List<TextRule>();

            foreach (ModuleRenameEVO module in plan.Modules.OrderByDescending(m => m.OldNamespace.Length))
                rules.Add(TextRule.Token(module.OldNamespace, module.NewNamespace));

            foreach (ChannelRenameEVO channel in plan.Channels)
                rules.Add(TextRule.Token("FlowLogType." + channel.OldName, "FlowLogType." + channel.NewName));

            foreach (IdentifierRenameEVO identifier in plan.Classes
                         .SelectMany(file => file.Identifiers)
                         .OrderByDescending(identifier => identifier.Old.Length))
                rules.Add(TextRule.Token(identifier.Old, identifier.New));

            return rules;
        }

        /// <summary>The rules for the files inside the module folder: the global ones and the screen address.</summary>
        internal List<TextRule> Local(ModuleRenamePlanEVO plan)
        {
            List<TextRule> rules = Global(plan);

            foreach (ScreenAddressRenameEVO screen in plan.ScreenAddresses)
                rules.Add(TextRule.ScreenAddress(screen.OldAddress, screen.NewAddress));

            return rules;
        }
    }
}
#endif
