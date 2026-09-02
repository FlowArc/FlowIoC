using System;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.Editor.Root;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The Root's inspector shows what a screen context declares in code, which means
    /// instantiating the context to read a property. A context whose declaration reaches for an
    /// injected member cannot survive that, so the failure is turned into a message instead of an
    /// exception that would break the whole inspector.
    /// </summary>
    public class ScreenSubContextDeclarationsTests
    {
        internal class ReadableScreenView : ScreenView
        {
        }

        internal class ReadableScreenMediator : IMediator
        {
            public void OnRegister()
            {
            }

            public void OnRemove()
            {
            }
        }

        internal class ReadableScreenContext : ScreenSubContext<ReadableScreenView, ReadableScreenMediator>
        {
            protected override ScreenCVO Screen => new()
            {
                ManagerId = 1,
                Layer = 4,
                Tag = ScreenTag.GroupC,
                HasShowAnimation = true,
                Load = ScreenLoadCVO.Addressable("Readable")
            };
        }

        internal class BrokenScreenContext : ScreenSubContext<ReadableScreenView, ReadableScreenMediator>
        {
            protected override ScreenCVO Screen => throw new InvalidOperationException("reaches for an injected member");
        }

        private ScreenSubContextDeclarations _declarations;

        [SetUp]
        public void SetUp() => _declarations = new ScreenSubContextDeclarations();

        [Test]
        public void A_context_type_is_resolved_from_its_full_name()
        {
            Type resolved = _declarations.ResolveType(typeof(ReadableScreenContext).FullName);

            Assert.AreEqual(typeof(ReadableScreenContext), resolved);
        }

        [Test]
        public void A_name_that_resolves_to_nothing_is_not_a_screen_context()
        {
            Assert.IsNull(_declarations.ResolveType("Nowhere.NoSuchContext"));
            Assert.IsFalse(_declarations.IsScreenContext(null));
        }

        [Test]
        public void A_plain_context_is_not_a_screen_context()
        {
            Assert.IsFalse(_declarations.IsScreenContext(typeof(Context)));
        }

        [Test]
        public void A_declaration_is_read_from_the_context_itself()
        {
            bool read = _declarations.TryRead(typeof(ReadableScreenContext), out ScreenCVO declaration, out string error);

            Assert.IsTrue(read);
            Assert.IsNull(error);
            Assert.AreEqual(1, declaration.ManagerId);
            Assert.AreEqual(4, declaration.Layer);
            Assert.AreEqual(ScreenTag.GroupC, declaration.Tag);
            Assert.IsTrue(declaration.HasShowAnimation);
            Assert.AreEqual("Readable", declaration.Load.Key);
        }

        [Test]
        public void A_declaration_that_throws_is_reported_rather_than_raised()
        {
            bool read = _declarations.TryRead(typeof(BrokenScreenContext), out ScreenCVO declaration, out string error);

            Assert.IsFalse(read);
            Assert.IsNull(declaration);
            Assert.IsTrue(error.Contains(nameof(BrokenScreenContext)));
        }

        [Test]
        public void A_declaration_is_read_once_and_cached()
        {
            _declarations.TryRead(typeof(ReadableScreenContext), out ScreenCVO first, out string _);
            _declarations.TryRead(typeof(ReadableScreenContext), out ScreenCVO second, out string _);

            Assert.AreSame(first, second);
        }
    }
}
