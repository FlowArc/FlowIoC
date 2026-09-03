using System.Reflection;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The injector's list is filled by its own inspector, so an object assembled from code carries
    /// a ViewInjector with nothing in it and every view on that object stays unregistered without a
    /// word. Finding that out cost an hour in a test module's scene; what these tests hold is that
    /// the injector says so instead.
    /// </summary>
    public class ViewInjectorUnfilledEntriesTests
    {
        private class ProbeView : MonoBehaviour, IView
        {
            public bool IsRegistered { get; set; }
        }

        private GameObject _viewHost;

        [TearDown]
        public void TearDown()
        {
            if (_viewHost != null)
                Object.DestroyImmediate(_viewHost);
        }

        [Test]
        public void An_empty_list_on_an_object_that_carries_a_view_is_reported()
        {
            _viewHost = new GameObject("ProbeViewHost");
            ViewInjector injector = _viewHost.AddComponent<ViewInjector>();
            _viewHost.AddComponent<ProbeView>();

            LogAssert.Expect(LogType.Error, new Regex("ProbeViewHost(.*)ProbeView"));

            StartInjector(injector);
        }

        /// <summary>
        /// An injector on an object with no view is idle rather than broken - the inspector says as
        /// much in a help box - so it must not be reported as a failure.
        /// </summary>
        [Test]
        public void An_object_with_no_view_at_all_is_not_reported()
        {
            _viewHost = new GameObject("EmptyHost");
            ViewInjector injector = _viewHost.AddComponent<ViewInjector>();

            StartInjector(injector);
        }

        /// <summary>
        /// Start is a Unity message, and an edit mode test is not a running scene. Calling it is the
        /// only way to exercise what the injector does when the scene begins.
        /// </summary>
        private static void StartInjector(ViewInjector injector)
        {
            MethodInfo start = typeof(ViewInjector).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);

            start.Invoke(injector, null);
        }
    }
}
