using System.Reflection;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What is worked out about a type once, before it is ever injected. A property the attribute
    /// cannot be honoured on is reported there, once, rather than on every injection of every
    /// instance.
    /// </summary>
    public class InjectEntryTests
    {
        private class Dependency
        {
        }

        private class Target
        {
            [Inject] private Dependency _writable { get; set; }
            [Inject] private Dependency _readOnly { get; }

            public Dependency Writable => _writable;
        }

        [SetUp]
        public void SetUp()
        {
            // The entries are remembered per type for the run, and this test is about the report
            // made while they are built - so they are forgotten first, as a new run forgets them.
            typeof(InjectionExtensions)
                .GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic)
                ?.Invoke(null, null);
        }

        [Test]
        public void A_property_without_a_setter_is_reported_once_and_the_others_are_still_injected()
        {
            StandInContext context = new StandInContext();
            Dependency dependency = context.InjectionBinder.Bind<Dependency>();

            LogAssert.Expect(LogType.Error, new Regex("has no setter"));

            Target first = new Target();
            Target second = new Target();
            context.TryToInjectObject(first);
            context.TryToInjectObject(second);

            Assert.That(first.Writable, Is.SameAs(dependency));
            Assert.That(second.Writable, Is.SameAs(dependency));
        }
    }
}
