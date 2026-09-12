using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The order of the text rules is the correctness of the pass: a nested namespace before the
    /// one it starts with, a longer identifier before a shorter one it contains, and the screen
    /// address only inside the module's own files.
    /// </summary>
    public class RenameRulesTests
    {
        private static ModuleRenamePlanEVO Plan()
        {
            var plan = new ModuleRenamePlanEVO();

            plan.Modules.Add(new ModuleRenameEVO
            {
                OldNamespace = "Modules.CounterModule", NewNamespace = "Modules.TimerModule", IsPicked = true
            });
            plan.Modules.Add(new ModuleRenameEVO
            {
                OldNamespace = "Modules.CounterModule.CounterTestModule", NewNamespace = "Modules.TimerModule.TimerTestModule"
            });
            plan.Channels.Add(new ChannelRenameEVO {OldName = "CounterModule", NewName = "TimerModule"});

            var holder = new ClassRenameEVO();
            holder.Identifiers.Add(new IdentifierRenameEVO {Old = "CounterServiceSignals", New = "TimerServiceSignals"});
            holder.Identifiers.Add(new IdentifierRenameEVO {Old = "CounterServiceSignalsIncoming", New = "TimerServiceSignalsIncoming"});
            plan.Classes.Add(holder);

            plan.ScreenAddresses.Add(new ScreenAddressRenameEVO {OldAddress = "CounterScreen", NewAddress = "TimerScreen"});

            return plan;
        }

        [Test]
        public void Namespaces_come_first_deepest_first_then_channels_then_identifiers_longest_first()
        {
            List<TextRule> rules = new RenameRules().Global(Plan());

            CollectionAssert.AreEqual(
                new[]
                {
                    "Modules.CounterModule.CounterTestModule",
                    "Modules.CounterModule",
                    "FlowLogType.CounterModule",
                    "CounterServiceSignalsIncoming",
                    "CounterServiceSignals"
                },
                rules.Select(rule => rule.Old));
        }

        [Test]
        public void The_global_rules_carry_no_screen_address_and_the_local_rules_add_it_last()
        {
            var rename = new RenameRules();
            ModuleRenamePlanEVO plan = Plan();

            Assert.IsFalse(rename.Global(plan).Any(rule => rule.Old == "CounterScreen"));

            List<TextRule> local = rename.Local(plan);
            Assert.AreEqual("CounterScreen", local.Last().Old);
            Assert.AreEqual(rename.Global(plan).Count + 1, local.Count);
        }
    }
}
