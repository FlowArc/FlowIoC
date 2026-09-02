using System.Collections.Generic;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A Root hands each sub-context the entry it was listed with, and knows nothing about what a
    /// given kind of sub-context does with it. The seam is therefore tested with a context that
    /// only records what it was given, rather than with a screen context.
    /// </summary>
    public class RootBaseSubContextOverrideTests
    {
        internal class RecordingContext : Context, ISubContextOverridable
        {
            internal SubContextData Received;
            internal bool WasCalled;

            void ISubContextOverridable.ApplyOverride(in SubContextData data)
            {
                Received = data;
                WasCalled = true;
            }
        }

        private class ProbeRoot : Root<Context>
        {
            // _rootsManager is normally set in Awake, which edit mode does not run.
            internal void UseSharedRootsManager() => _rootsManager = (RootsManager) RootsManagerFactory.GetRootsManager();
        }

        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
                Object.DestroyImmediate(_host);
        }

        private ProbeRoot RootListing(SubContextData entry)
        {
            _host = new GameObject("ProbeRoot");
            ProbeRoot root = _host.AddComponent<ProbeRoot>();
            root.UseSharedRootsManager();
            root.SubContextTypes = new List<SubContextData> {entry};
            return root;
        }

        private static RecordingContext Created(ProbeRoot root)
        {
            foreach (IContext context in root.GetSubContexts())
                if (context is RecordingContext recording)
                    return recording;

            return null;
        }

        [Test]
        public void The_root_hands_each_sub_context_the_entry_it_was_listed_with()
        {
            ProbeRoot root = RootListing(new SubContextData
            {
                ContextFullName = typeof(RecordingContext).FullName,
                ContextName = nameof(RecordingContext),
                OverrideScreen = true,
                ScreenManagerId = 2,
                ScreenLayer = 5
            });

            root.InitializeSubContexts();

            RecordingContext created = Created(root);
            Assert.IsNotNull(created);
            Assert.IsTrue(created.WasCalled);
            Assert.AreEqual(2, created.Received.ScreenManagerId);
            Assert.AreEqual(5, created.Received.ScreenLayer);
        }

        [Test]
        public void An_entry_with_no_override_still_reaches_the_sub_context()
        {
            ProbeRoot root = RootListing(new SubContextData
            {
                ContextFullName = typeof(RecordingContext).FullName,
                ContextName = nameof(RecordingContext)
            });

            root.InitializeSubContexts();

            RecordingContext created = Created(root);
            Assert.IsNotNull(created);
            Assert.IsTrue(created.WasCalled);
            Assert.IsFalse(created.Received.OverrideScreen);
        }
    }
}
