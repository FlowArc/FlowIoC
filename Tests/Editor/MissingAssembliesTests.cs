using System.Collections.Generic;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class MissingAssembliesTests
    {
        [Test]
        public void A_module_that_requires_nothing_is_missing_nothing()
        {
            IReadOnlyList<string> missing = new MissingAssemblies().In(
                new[] {"Sirenix.OdinInspector.Attributes"}, new string[0]);

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void Requirements_the_project_already_has_are_not_reported()
        {
            IReadOnlyList<string> missing = new MissingAssemblies().In(
                new[] {"Sirenix.OdinInspector.Attributes", "DOTween"},
                new[] {"DOTween"});

            CollectionAssert.IsEmpty(missing);
        }

        /// <summary>
        /// The order is the order they were asked for, because it is what the page reads out to
        /// whoever has to go and import the asset.
        /// </summary>
        [Test]
        public void Only_the_absent_requirements_come_back_and_in_the_order_asked()
        {
            IReadOnlyList<string> missing = new MissingAssemblies().In(
                new[] {"DOTween"},
                new[] {"Sirenix.OdinInspector.Attributes", "DOTween", "Shapes"});

            CollectionAssert.AreEqual(
                new[] {"Sirenix.OdinInspector.Attributes", "Shapes"}, missing);
        }

        /// <summary>
        /// An unknown set of loaded assemblies is not an empty one. Nothing known to be present
        /// means nothing can be ruled out, so every requirement is reported.
        /// </summary>
        [Test]
        public void A_project_whose_assemblies_are_unknown_is_missing_all_of_them()
        {
            IReadOnlyList<string> missing = new MissingAssemblies().In(
                null, new[] {"DOTween"});

            CollectionAssert.AreEqual(new[] {"DOTween"}, missing);
        }

        [Test]
        public void The_same_requirement_asked_for_twice_is_reported_once()
        {
            IReadOnlyList<string> missing = new MissingAssemblies().In(
                new string[0], new[] {"DOTween", "DOTween"});

            CollectionAssert.AreEqual(new[] {"DOTween"}, missing);
        }

        /// <summary>
        /// The Editor's own assembly is loaded, so the check answers truthfully about a name
        /// nobody had to hand in. This is what LoadedAssemblies is for.
        /// </summary>
        [Test]
        public void The_loaded_assemblies_include_the_one_this_test_lives_in()
        {
            CollectionAssert.Contains(new LoadedAssemblies().Names(), "FlowIoC.Tests");
        }
    }
}
