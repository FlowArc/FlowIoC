using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Gateway;
using FlowIoC.AssetModule.Model;
using FlowIoC.AssetModule.Service.Sub;
using FlowIoC.AssetModule.Signals;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>A handle that completes when the test says, and counts its releases.</summary>
    internal sealed class FakeAssetHandle : IAssetHandle
    {
        private readonly TaskCompletionSource<bool> _done = new();

        public bool IsValid { get; private set; } = true;
        public bool IsDone => _done.Task.IsCompleted;
        public bool Succeeded { get; private set; }
        public float PercentComplete { get; set; }
        public object Result { get; private set; }
        public Task Task => _done.Task;
        public int Releases { get; private set; }

        public object WaitForCompletion() => Result;

        public void Complete(object result)
        {
            Result = result;
            Succeeded = true;
            PercentComplete = 1f;
            _done.TrySetResult(true);
        }

        public void Fail()
        {
            Succeeded = false;
            PercentComplete = 1f;
            _done.TrySetResult(false);
        }

        public void Release()
        {
            Releases++;
            IsValid = false;
        }
    }

    internal sealed class FakeAddressablesGateway : IAddressablesGateway
    {
        public readonly Dictionary<string, FakeAssetHandle> Handles = new();
        public readonly Dictionary<string, List<string>> Locations = new();
        public readonly Dictionary<string, FakeAssetHandle> Downloads = new();

        public int Loads;
        public long DownloadSize;

        public ThreadPriority BackgroundLoadingPriority { get; set; } = ThreadPriority.Normal;

        public IAssetHandle LoadAsset<T>(object runtimeKey)
        {
            Loads++;
            var handle = new FakeAssetHandle();
            Handles[runtimeKey.ToString()] = handle;
            return handle;
        }

        public Task<IReadOnlyList<string>> LoadResourceLocationsAsync(string label, Type type) =>
            Task.FromResult<IReadOnlyList<string>>(Locations.TryGetValue(label, out var keys) ? keys : new List<string>());

        public Task<long> GetDownloadSizeAsync(object keyOrLabel) => Task.FromResult(DownloadSize);

        public IAssetHandle DownloadDependencies(object keyOrLabel)
        {
            var handle = new FakeAssetHandle();
            Downloads[keyOrLabel.ToString()] = handle;
            return handle;
        }

        public void Complete(string key, object result) => Handles[key].Complete(result);

        public void Fail(string key) => Handles[key].Fail();
    }

    /// <summary>
    /// The asset sub services wired the way AssetServiceContext wires them, over the fake gateway.
    /// The [Inject] properties are set by reflection, the way the dev suite's LoadingTestKit does
    /// it, so no Context has to exist.
    /// </summary>
    internal sealed class AssetTestKit
    {
        public readonly FakeAddressablesGateway Gateway = new();
        public readonly AssetSignals Signals = new();
        public readonly IAssetRegistryModel Registry = new AssetRegistryModel();
        public readonly AssetLoadSubService Load = new();
        public readonly AssetGroupSubService Group = new();
        public readonly AssetReleaseSubService Release = new();
        public readonly AssetPrioritySubService Priority = new();
        public readonly AssetDownloadSubService Download = new();

        public AssetTestKit()
        {
            Inject(Load, "_registry", Registry);
            Inject(Load, "_release", Release);
            Inject(Load, "_gateway", Gateway);
            Inject(Load, "_signals", Signals);

            Inject(Group, "_registry", Registry);
            Inject(Group, "_load", Load);
            Inject(Group, "_priority", Priority);
            Inject(Group, "_gateway", Gateway);
            Inject(Group, "_signals", Signals);

            Inject(Release, "_registry", Registry);
            Inject(Release, "_signals", Signals);

            Inject(Priority, "_gateway", Gateway);

            Inject(Download, "_gateway", Gateway);
            Inject(Download, "_priority", Priority);
            Inject(Download, "_signals", Signals);
        }

        public static void Inject(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType()
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(property, $"{target.GetType().Name} has no property {propertyName}");
            property.SetValue(target, value);
        }

        /// <summary>
        /// Pumps the editor loop until the task is done. The service's awaits continue on Unity's
        /// synchronization context, which only a frame advances, so a test that awaits is a
        /// [UnityTest] coroutine yielding through this.
        /// </summary>
        public static IEnumerator Until(Task task, int frames = 50)
        {
            for (int i = 0; i < frames && !task.IsCompleted; i++)
                yield return null;

            Assert.IsTrue(task.IsCompleted, "the task did not complete within the frame budget");
        }

        /// <summary>A frame, for the sampling loops that report once per frame.</summary>
        public static IEnumerator Frames(int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;
        }
    }
}
