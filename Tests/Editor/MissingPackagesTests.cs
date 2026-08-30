using System.Collections.Generic;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class MissingPackagesTests
    {
        [Test]
        public void A_module_that_requires_nothing_is_missing_nothing()
        {
            IReadOnlyList<string> missing = new MissingPackages().In(
                new[] {"com.unity.cinemachine"}, new string[0]);

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void Requirements_the_project_already_has_are_not_reported()
        {
            IReadOnlyList<string> missing = new MissingPackages().In(
                new[] {"com.unity.cinemachine", "com.unity.render-pipelines.core"},
                new[] {"com.unity.cinemachine"});

            CollectionAssert.IsEmpty(missing);
        }

        /// <summary>
        /// The order is the order they were asked for, because it is what the install dialog reads
        /// out to whoever has to approve adding them.
        /// </summary>
        [Test]
        public void Only_the_absent_requirements_come_back_and_in_the_order_asked()
        {
            IReadOnlyList<string> missing = new MissingPackages().In(
                new[] {"com.unity.render-pipelines.core"},
                new[] {"com.unity.cinemachine", "com.unity.render-pipelines.core", "com.unity.splines"});

            CollectionAssert.AreEqual(new[] {"com.unity.cinemachine", "com.unity.splines"}, missing);
        }

        /// <summary>
        /// The Package Manager answers asynchronously, so the list of what is installed can be
        /// absent rather than empty. Nothing known to be present means nothing can be ruled out.
        /// </summary>
        [Test]
        public void A_project_whose_packages_are_unknown_is_missing_all_of_them()
        {
            IReadOnlyList<string> missing = new MissingPackages().In(
                null, new[] {"com.unity.cinemachine"});

            CollectionAssert.AreEqual(new[] {"com.unity.cinemachine"}, missing);
        }

        [Test]
        public void The_same_requirement_asked_for_twice_is_reported_once()
        {
            IReadOnlyList<string> missing = new MissingPackages().In(
                new string[0],
                new[] {"com.unity.cinemachine", "com.unity.cinemachine"});

            CollectionAssert.AreEqual(new[] {"com.unity.cinemachine"}, missing);
        }
    }
}
