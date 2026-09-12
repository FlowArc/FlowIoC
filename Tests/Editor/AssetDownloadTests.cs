using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>A download fetches bundles and holds nothing: no registry entry, a released handle.</summary>
    public class AssetDownloadTests
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
        public IEnumerator The_size_is_what_the_gateway_answers()
        {
            _kit.Gateway.DownloadSize = 4096;

            Task<long> task = _kit.Download.GetDownloadSizeAsync("l");
            yield return AssetTestKit.Until(task);

            Assert.AreEqual(4096, task.Result);
        }

        [UnityTest]
        public IEnumerator A_download_that_succeeds_answers_true_reports_one_last_and_releases_its_handle()
        {
            var reports = new Reports();

            Task<bool> task = _kit.Download.DownloadDependenciesAsync("l", new AssetLoadOptions {Progress = reports});
            yield return AssetTestKit.Frames(1);

            _kit.Gateway.Downloads["l"].PercentComplete = 0.4f;
            yield return AssetTestKit.Frames(6);

            _kit.Gateway.Downloads["l"].Complete(null);
            yield return AssetTestKit.Until(task);

            Assert.IsTrue(task.Result);
            Assert.AreEqual(1f, reports.Values[reports.Values.Count - 1]);
            CollectionAssert.Contains(reports.Values, 0.4f);
            Assert.AreEqual(1, _kit.Gateway.Downloads["l"].Releases);
            Assert.AreEqual(0, _kit.Registry.Entries.Count);
        }

        [UnityTest]
        public IEnumerator A_download_that_fails_answers_false_and_dispatches()
        {
            string failed = null;
            _kit.Signals.Outgoing.AssetLoadFailed.AddListener(key => failed = key);
            LogAssert.Expect(LogType.Error, new Regex("Download failed: l"));

            Task<bool> task = _kit.Download.DownloadDependenciesAsync("l");
            yield return AssetTestKit.Frames(1);

            _kit.Gateway.Downloads["l"].Fail();
            yield return AssetTestKit.Until(task);

            Assert.IsFalse(task.Result);
            Assert.AreEqual("l", failed);
            Assert.AreEqual(1, _kit.Gateway.Downloads["l"].Releases);
        }

        [UnityTest]
        public IEnumerator A_background_download_holds_the_priority_down_until_it_ends()
        {
            _kit.Gateway.BackgroundLoadingPriority = ThreadPriority.Normal;

            Task<bool> task = _kit.Download.DownloadDependenciesAsync("l", new AssetLoadOptions {Background = true});
            yield return AssetTestKit.Frames(1);

            Assert.AreEqual(ThreadPriority.Low, _kit.Gateway.BackgroundLoadingPriority);

            _kit.Gateway.Downloads["l"].Complete(null);
            yield return AssetTestKit.Until(task);

            Assert.AreEqual(ThreadPriority.Normal, _kit.Gateway.BackgroundLoadingPriority);
        }

        [Test]
        public void An_invalid_key_answers_nothing_without_touching_the_gateway()
        {
            LogAssert.Expect(LogType.Error, new Regex("GetDownloadSizeAsync: invalid key"));
            LogAssert.Expect(LogType.Error, new Regex("DownloadDependenciesAsync: invalid key"));

            Task<long> size = _kit.Download.GetDownloadSizeAsync(null);
            Task<bool> download = _kit.Download.DownloadDependenciesAsync("");

            Assert.AreEqual(0L, size.Result);
            Assert.IsFalse(download.Result);
            Assert.AreEqual(0, _kit.Gateway.Downloads.Count);
        }
    }
}
