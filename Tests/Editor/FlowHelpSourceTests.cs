using System;
using System.Collections.Generic;
using FlowIoC.Editor.Inspector;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowHelpSourceTests
    {
        private class FakeScriptText : IFlowScriptText
        {
            private readonly Dictionary<Type, string> _sources = new Dictionary<Type, string>();

            public int Reads { get; private set; }

            public void Add(Type type, string source) => _sources[type] = source;

            public string Read(Type type)
            {
                Reads++;

                return _sources.TryGetValue(type, out string source) ? source : null;
            }
        }

        private class Base
        {
            public bool AutoSetup;
        }

        private class Derived : Base
        {
            public int Speed;
        }

        [Test]
        public void For_reads_a_member_declared_on_the_type_itself()
        {
            var texts = new FakeScriptText();
            texts.Add(typeof(Derived), "class Derived {\n/// <summary>How fast.</summary>\npublic int Speed;\n}");

            string help = new FlowHelpSource(texts).For(typeof(Derived), "Speed");

            Assert.AreEqual("How fast.", help);
        }

        [Test]
        public void For_walks_up_to_the_base_type()
        {
            var texts = new FakeScriptText();
            texts.Add(typeof(Derived), "class Derived {\npublic int Speed;\n}");
            texts.Add(typeof(Base), "class Base {\n/// <summary>Runs Setup on its own.</summary>\npublic bool AutoSetup;\n}");

            string help = new FlowHelpSource(texts).For(typeof(Derived), "AutoSetup");

            Assert.AreEqual("Runs Setup on its own.", help);
        }

        [Test]
        public void For_answers_null_when_nothing_documents_the_member()
        {
            var texts = new FakeScriptText();
            texts.Add(typeof(Derived), "class Derived {\npublic int Speed;\n}");

            string help = new FlowHelpSource(texts).For(typeof(Derived), "Speed");

            Assert.IsNull(help);
        }

        [Test]
        public void For_reads_each_type_once()
        {
            var texts = new FakeScriptText();
            texts.Add(typeof(Derived), "class Derived {\n/// <summary>How fast.</summary>\npublic int Speed;\n}");

            var source = new FlowHelpSource(texts);
            source.For(typeof(Derived), "Speed");
            source.For(typeof(Derived), "Speed");

            Assert.AreEqual(1, texts.Reads);
        }

        [Test]
        public void Summary_returns_the_types_own_documentation()
        {
            var texts = new FakeScriptText();
            texts.Add(typeof(Derived), "/// <summary>A thing that moves.</summary>\nclass Derived {\n}");

            string summary = new FlowHelpSource(texts).Summary(typeof(Derived));

            Assert.AreEqual("A thing that moves.", summary);
        }
    }
}
