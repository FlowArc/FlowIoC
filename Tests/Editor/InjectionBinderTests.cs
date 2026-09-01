using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Asking the binder for something it does not have. A Connector gets signal holders rather
    /// than binding them, so this is the path a missing module takes, and it has to say which
    /// type is missing instead of throwing a dictionary's key error at whoever asked.
    /// </summary>
    public class InjectionBinderTests
    {
        private class Holder
        {
        }

        private InjectionBinder _binder;

        [SetUp]
        public void SetUp()
        {
            _binder = new InjectionBinder();
            _binder.SetBindedContext(new Context());
        }

        [Test]
        public void Asking_for_a_type_nobody_bound_names_the_type()
        {
            LogAssert.Expect(LogType.Error, new Regex("Nothing is bound to Holder"));

            Assert.IsNull(_binder.GetInstance<Holder>());
        }

        [Test]
        public void Asking_under_a_name_nobody_bound_says_the_type_is_there_and_the_name_is_not()
        {
            _binder.Bind<Holder>();

            LogAssert.Expect(LogType.Error, new Regex("Holder is bound, but not under the name 'second'"));

            Assert.IsNull(_binder.GetInstance<Holder>("second"));
        }

        [Test]
        public void What_was_bound_comes_back()
        {
            Holder bound = _binder.Bind<Holder>();

            Assert.AreSame(bound, _binder.GetInstance<Holder>());
        }
    }
}
