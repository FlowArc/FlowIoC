using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A background load holds the priority down for as long as any background load is in
    /// flight, and hands back what it found rather than a Low it set itself.
    /// </summary>
    public class AssetPriorityTests
    {
        private AssetTestKit _kit;

        [SetUp]
        public void SetUp()
        {
            _kit = new AssetTestKit();
            _kit.Gateway.BackgroundLoadingPriority = ThreadPriority.BelowNormal;
        }

        [UnityTest]
        public IEnumerator Two_overlapping_background_loads_lower_once_and_restore_after_the_last()
        {
            _kit.Gateway.Locations["l1"] = new List<string> {"a"};
            _kit.Gateway.Locations["l2"] = new List<string> {"b"};
            var background = new AssetLoadOptions {Background = true};

            Task first = _kit.Group.LoadGroupByLabelAsync<string>("l1", null, background);
            yield return AssetTestKit.Frames(1);
            Task second = _kit.Group.LoadGroupByLabelAsync<string>("l2", null, background);
            yield return AssetTestKit.Frames(1);

            Assert.AreEqual(ThreadPriority.Low, _kit.Gateway.BackgroundLoadingPriority);

            _kit.Gateway.Complete("a", "a-asset");
            yield return AssetTestKit.Until(first);

            Assert.AreEqual(ThreadPriority.Low, _kit.Gateway.BackgroundLoadingPriority);

            _kit.Gateway.Complete("b", "b-asset");
            yield return AssetTestKit.Until(second);

            Assert.AreEqual(ThreadPriority.BelowNormal, _kit.Gateway.BackgroundLoadingPriority);
        }

        [UnityTest]
        public IEnumerator A_foreground_load_leaves_the_priority_alone()
        {
            _kit.Gateway.Locations["l"] = new List<string> {"a"};

            Task task = _kit.Group.LoadGroupByLabelAsync<string>("l");
            yield return AssetTestKit.Frames(1);

            Assert.AreEqual(ThreadPriority.BelowNormal, _kit.Gateway.BackgroundLoadingPriority);

            _kit.Gateway.Complete("a", "a-asset");
            yield return AssetTestKit.Until(task);

            Assert.AreEqual(ThreadPriority.BelowNormal, _kit.Gateway.BackgroundLoadingPriority);
        }

        [Test]
        public void Exit_without_enter_changes_nothing()
        {
            _kit.Priority.Exit();

            Assert.AreEqual(ThreadPriority.BelowNormal, _kit.Gateway.BackgroundLoadingPriority);
        }
    }
}
