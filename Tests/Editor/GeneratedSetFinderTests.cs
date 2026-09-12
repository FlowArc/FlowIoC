using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The generated set is found on disk, not derived: the files in the folders Create Module
    /// writes a module's named classes into, whose names start with the module's stem. The
    /// identifiers declared inside follow the same rule, so a signal holder's nested Incoming and
    /// Outgoing classes go with it.
    /// </summary>
    public class GeneratedSetFinderTests
    {
        private const string ROOTS = "D:/p/CounterModule/Scripts/Runtime/RootsContexts";
        private const string SIGNALS = "D:/p/CounterModule/Scripts/Signals";

        private static readonly Dictionary<string, string> Files = new Dictionary<string, string>
        {
            [ROOTS + "/CounterServiceRoot.cs"] = "namespace X { public class CounterServiceRoot : Root<CounterServiceContext> { } }",
            [ROOTS + "/CounterServiceContext.cs"] = "namespace X { public class CounterServiceContext : Context { } }",
            [ROOTS + "/TickRoot.cs"] = "namespace X { public class TickRoot : Root<CounterServiceContext> { } }",
            [SIGNALS + "/CounterServiceSignals.cs"] =
                "namespace X {\n public class CounterServiceSignals : ISignalHolder {\n"
                + " public CounterServiceSignalsIncoming Incoming = new();\n }\n"
                + " public class CounterServiceSignalsIncoming { }\n public class CounterServiceSignalsOutgoing { }\n"
                + " internal enum CounterMode { A = 0 }\n public struct Payload { }\n}"
        };

        private static GeneratedSetFinder Finder() => new GeneratedSetFinder(
            folder => Files.Keys.Where(path => path.StartsWith(folder + "/") && !path.Substring(folder.Length + 1).Contains("/")),
            path => Files[path]);

        [Test]
        public void A_file_that_carries_the_stem_is_renamed_with_every_identifier_in_it_that_does()
        {
            var kept = new List<string>();

            List<ClassRenameEVO> found = Finder().In(new[] {SIGNALS}, "Counter", "Timer", kept);

            ClassRenameEVO holder = found.Single();
            Assert.AreEqual(SIGNALS + "/TimerServiceSignals.cs", holder.NewPath.Replace('\\', '/'));
            CollectionAssert.AreEquivalent(
                new[] {"CounterServiceSignals", "CounterServiceSignalsIncoming", "CounterServiceSignalsOutgoing", "CounterMode"},
                holder.Identifiers.Select(i => i.Old));
            Assert.AreEqual("TimerServiceSignalsIncoming", holder.Identifiers.Single(i => i.Old == "CounterServiceSignalsIncoming").New);
            Assert.IsFalse(holder.Identifiers.Any(i => i.Old == "Payload"), "an identifier without the stem is not renamed");
        }

        [Test]
        public void A_file_that_does_not_carry_the_stem_is_kept_and_says_so()
        {
            var kept = new List<string>();

            List<ClassRenameEVO> found = Finder().In(new[] {ROOTS}, "Counter", "Timer", kept);

            CollectionAssert.AreEquivalent(
                new[] {ROOTS + "/CounterServiceRoot.cs", ROOTS + "/CounterServiceContext.cs"},
                found.Select(f => f.Path));
            Assert.AreEqual(1, kept.Count);
            StringAssert.Contains("TickRoot.cs", kept[0]);
        }

        [Test]
        public void A_missing_or_null_folder_is_skipped()
        {
            var kept = new List<string>();

            List<ClassRenameEVO> found = Finder().In(new[] {null, "D:/p/CounterModule/Nowhere"}, "Counter", "Timer", kept);

            Assert.IsEmpty(found);
            Assert.IsEmpty(kept);
        }

        [Test]
        public void An_identifier_declared_twice_is_listed_once()
        {
            var finder = new GeneratedSetFinder(
                _ => new[] {"D:/p/X/CounterThing.cs"},
                _ => "partial class CounterThing { } partial class CounterThing { }");

            List<ClassRenameEVO> found = finder.In(new[] {"D:/p/X"}, "Counter", "Timer", new List<string>());

            Assert.AreEqual(1, found.Single().Identifiers.Count);
        }
    }
}
