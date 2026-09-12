using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A group load claims every key under the group id, marks it loaded and announces it, and
    /// its progress is the mean of what the handles report.
    /// </summary>
    public class AssetGroupSubServiceTests
    {
        private sealed class Reports : IProgress<float>
        {
            public readonly List<float> Values = new();
            public void Report(float value) => Values.Add(value);
        }

        private AssetTestKit _kit;

        [SetUp]
        public void SetUp() => _kit = new AssetTestKit();

        [UnityTest]
        public IEnumerator A_label_load_claims_every_location_under_the_group_and_marks_it_loaded()
        {
            string loaded = null;
            _kit.Signals.Outgoing.GroupLoaded.AddListener(group => loaded = group);
            _kit.Gateway.Locations["l"] = new List<string> {"a", "b"};

            Task task = _kit.Group.LoadGroupByLabelAsync<string>("l");
            yield return AssetTestKit.Frames(1);

            Assert.AreEqual(2, _kit.Gateway.Loads);
            Assert.IsFalse(_kit.Group.IsGroupLoaded("l"));

            _kit.Gateway.Complete("a", "a-asset");
            _kit.Gateway.Complete("b", "b-asset");
            yield return AssetTestKit.Until(task);

            Assert.IsTrue(_kit.Group.IsGroupLoaded("l"));
            CollectionAssert.AreEquivalent(new[] {"a", "b"}, _kit.Group.GetGroupKeys("l"));
            Assert.AreEqual("l", loaded);
        }

        [UnityTest]
        public IEnumerator Progress_rises_and_ends_with_one()
        {
            _kit.Gateway.Locations["l"] = new List<string> {"a", "b"};
            var reports = new Reports();

            Task task = _kit.Group.LoadGroupByLabelAsync<string>("l", null, new AssetLoadOptions {Progress = reports});
            yield return AssetTestKit.Frames(2);

            // A sample lands every frame or two - the continuation posts to the synchronization
            // context, which the editor pumps once per update - so each state is held for several.
            _kit.Gateway.Handles["a"].PercentComplete = 0.5f;
            yield return AssetTestKit.Frames(6);

            _kit.Gateway.Complete("a", "a-asset");
            yield return AssetTestKit.Frames(6);

            _kit.Gateway.Complete("b", "b-asset");
            yield return AssetTestKit.Until(task);

            Assert.Greater(reports.Values.Count, 2);
            Assert.AreEqual(1f, reports.Values[reports.Values.Count - 1]);
            CollectionAssert.Contains(reports.Values, 0.25f);
            CollectionAssert.Contains(reports.Values, 0.5f);

            for (int i = 1; i < reports.Values.Count; i++)
                Assert.GreaterOrEqual(reports.Values[i], reports.Values[i - 1]);
        }

        [UnityTest]
        public IEnumerator A_group_already_loaded_reports_one_once()
        {
            _kit.Gateway.Locations["l"] = new List<string> {"a"};
            Task first = _kit.Group.LoadGroupByLabelAsync<string>("l");
            _kit.Gateway.Complete("a", "a-asset");
            yield return AssetTestKit.Until(first);

            var reports = new Reports();
            Task second = _kit.Group.LoadGroupByLabelAsync<string>("l", null, new AssetLoadOptions {Progress = reports});
            yield return AssetTestKit.Until(second);

            Assert.AreEqual(1, _kit.Gateway.Loads);
            Assert.AreEqual(new[] {1f}, reports.Values);
        }

        [UnityTest]
        public IEnumerator An_empty_label_reports_one_once_and_marks_the_group_loaded()
        {
            var reports = new Reports();

            Task task = _kit.Group.LoadGroupByLabelAsync<string>("nothing", null, new AssetLoadOptions {Progress = reports});
            yield return AssetTestKit.Until(task);

            Assert.AreEqual(0, _kit.Gateway.Loads);
            Assert.IsTrue(_kit.Group.IsGroupLoaded("nothing"));
            Assert.AreEqual(new[] {1f}, reports.Values);
        }

        [UnityTest]
        public IEnumerator LoadAssetsAsync_claims_the_keys_it_was_given()
        {
            Task task = _kit.Group.LoadAssetsAsync<string>("g", new object[] {"a", "b"});
            _kit.Gateway.Complete("a", "a-asset");
            _kit.Gateway.Complete("b", "b-asset");
            yield return AssetTestKit.Until(task);

            CollectionAssert.AreEquivalent(new[] {"a", "b"}, _kit.Group.GetGroupKeys("g"));
            Assert.IsTrue(_kit.Group.IsGroupLoaded("g"));
        }
    }
}
