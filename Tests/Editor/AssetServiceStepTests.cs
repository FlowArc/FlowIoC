using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Injectable.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The three steps a game binds on the asset service. What each promises the sequence is the
    /// retain: held until the load or the download is done, released on success, stopped when the
    /// download says no or the call throws - so a step after it never runs on assets that are not
    /// there.
    /// </summary>
    public class AssetServiceStepTests
    {
        /// <summary>Records what it was asked and answers with whatever the test put in.</summary>
        private class RecordingAssetService : IAssetService
        {
            public readonly List<(string label, string group, Type type)> GroupLoads = new();
            public readonly List<string> ReleasedGroups = new();
            public readonly List<string> Downloads = new();
            public bool DownloadAnswer = true;
            public Exception Throws;

            public Task<T> LoadAssetAsync<T>(object key, string groupId = null) => Task.FromResult(default(T));
            public T LoadAsset<T>(object key, string groupId = null) => default;
            public bool TryGetAsset<T>(object key, out T asset) { asset = default; return false; }
            public void Release(object key, string groupId = null) { }

            public Task LoadGroupByLabelAsync<T>(string label, string groupId = null, AssetLoadOptions options = default)
            {
                if (Throws != null) throw Throws;
                GroupLoads.Add((label, groupId, typeof(T)));
                return Task.CompletedTask;
            }

            public Task LoadAssetsAsync<T>(string groupId, IEnumerable<object> keys, AssetLoadOptions options = default) => Task.CompletedTask;
            public void AddToGroup(string groupId, object key) { }
            public void ReleaseGroup(string groupId) => ReleasedGroups.Add(groupId);
            public bool IsGroupLoaded(string groupId) => false;
            public IReadOnlyCollection<string> GetGroupKeys(string groupId) => Array.Empty<string>();
            public Task<long> GetDownloadSizeAsync(object keyOrLabel) => Task.FromResult(0L);

            public Task<bool> DownloadDependenciesAsync(object keyOrLabel, AssetLoadOptions options = default)
            {
                if (Throws != null) throw Throws;
                Downloads.Add(keyOrLabel.ToString());
                return Task.FromResult(DownloadAnswer);
            }
        }

        private class RecordingLoad : IAssetService.Commands.LoadGroupByLabel<Sprite>
        {
            public int Released, Stopped;
            public override void Release(params object[] commandGroupData) => Released++;
            public override void Stop() => Stopped++;
        }

        private class RecordingDownload : IAssetService.Commands.DownloadDependencies
        {
            public int Released, Stopped;
            public override void Release(params object[] commandGroupData) => Released++;
            public override void Stop() => Stopped++;
        }

        private StandInContext _context;
        private RecordingAssetService _assets;

        [SetUp]
        public void SetUp()
        {
            _context = new StandInContext();
            _assets = new RecordingAssetService();
            _context.InjectionBinder.BindInstance<IAssetService>(_assets);
        }

        private T Step<T>() where T : new()
        {
            var step = new T();
            _context.TryToInjectObject(step);
            return step;
        }

        [UnityTest]
        public IEnumerator LoadGroupByLabel_loads_the_label_into_the_group_as_the_type_and_releases()
        {
            RecordingLoad step = Step<RecordingLoad>();

            step.Execute("splash", "boot");
            yield return null;

            Assert.That(_assets.GroupLoads, Is.EqualTo(new[] {("splash", "boot", typeof(Sprite))}));
            Assert.AreEqual(1, step.Released);
            Assert.AreEqual(0, step.Stopped);
        }

        [UnityTest]
        public IEnumerator LoadGroupByLabel_stops_the_sequence_when_the_load_throws()
        {
            _assets.Throws = new InvalidOperationException("no catalogue");
            LogAssert.ignoreFailingMessages = true;
            RecordingLoad step = Step<RecordingLoad>();

            step.Execute("splash", "boot");
            yield return null;

            LogAssert.ignoreFailingMessages = false;
            Assert.AreEqual(0, step.Released);
            Assert.AreEqual(1, step.Stopped);
        }

        [Test]
        public void ReleaseGroup_lets_go_of_the_group_it_was_bound_with()
        {
            Step<IAssetService.Commands.ReleaseGroup>().Execute("match");

            Assert.That(_assets.ReleasedGroups, Is.EqualTo(new[] {"match"}));
        }

        [UnityTest]
        public IEnumerator DownloadDependencies_releases_once_the_bundles_are_cached()
        {
            RecordingDownload step = Step<RecordingDownload>();

            step.Execute("remote-art");
            yield return null;

            Assert.That(_assets.Downloads, Is.EqualTo(new[] {"remote-art"}));
            Assert.AreEqual(1, step.Released);
        }

        [UnityTest]
        public IEnumerator DownloadDependencies_stops_the_sequence_when_the_download_says_no()
        {
            _assets.DownloadAnswer = false;
            RecordingDownload step = Step<RecordingDownload>();

            step.Execute("remote-art");
            yield return null;

            Assert.AreEqual(0, step.Released);
            Assert.AreEqual(1, step.Stopped);
        }
    }
}
