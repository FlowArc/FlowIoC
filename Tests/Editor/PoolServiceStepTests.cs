using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Services;
using FlowIoC.PoolModule.Services.Sub;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The two steps a game binds on the pool service. Each holds the sequence until the fill it
    /// asked for is done - every group, or the one named at the binding - and stops it when the
    /// fill throws, so nothing runs on an empty pool.
    /// </summary>
    public class PoolServiceStepTests
    {
        private class RecordingPoolService : IPoolService
        {
            public readonly List<string> GroupsFilled = new();
            public int AllFilled;
            public Exception Throws;

            public void AutoInitializeAll() { }
            public void InitializeAll() { }

            public Task InitializeAllAsync()
            {
                if (Throws != null) throw Throws;
                AllFilled++;
                return Task.CompletedTask;
            }

            public void InitializeGroup(string groupKey) { }

            public Task InitializeGroupAsync(string groupKey)
            {
                if (Throws != null) throw Throws;
                GroupsFilled.Add(groupKey);
                return Task.CompletedTask;
            }

            public CreateSubService Create => null;
            public ReturnSubService Return => null;
            public CheckSubService Check => null;
            public DestroySubService Destroy => null;
            public IPoolableItem Get(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) => null;
            public T Get<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem => null;
            public Task<IPoolableItem> GetAsync(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) => Task.FromResult<IPoolableItem>(null);
            public Task<T> GetAsync<T>(string itemKey, Transform parent = null, Action<IPoolableItem> callback = null) where T : class, IPoolableItem => Task.FromResult<T>(null);
        }

        private class RecordingAll : IPoolService.Commands.InitializeAll
        {
            public int Released, Stopped;
            public override void Release(params object[] commandGroupData) => Released++;
            public override void Stop() => Stopped++;
        }

        private class RecordingGroup : IPoolService.Commands.InitializeGroup
        {
            public int Released, Stopped;
            public override void Release(params object[] commandGroupData) => Released++;
            public override void Stop() => Stopped++;
        }

        private StandInContext _context;
        private RecordingPoolService _pools;

        [SetUp]
        public void SetUp()
        {
            _context = new StandInContext();
            _pools = new RecordingPoolService();
            _context.InjectionBinder.BindInstance<IPoolService>(_pools);
        }

        private T Step<T>() where T : new()
        {
            var step = new T();
            _context.TryToInjectObject(step);
            return step;
        }

        [UnityTest]
        public IEnumerator InitializeAll_fills_every_group_and_releases()
        {
            RecordingAll step = Step<RecordingAll>();

            step.Execute();
            yield return null;

            Assert.AreEqual(1, _pools.AllFilled);
            Assert.AreEqual(1, step.Released);
            Assert.AreEqual(0, step.Stopped);
        }

        [UnityTest]
        public IEnumerator InitializeGroup_fills_the_group_it_was_bound_with_and_releases()
        {
            RecordingGroup step = Step<RecordingGroup>();

            step.Execute("Match");
            yield return null;

            Assert.That(_pools.GroupsFilled, Is.EqualTo(new[] {"Match"}));
            Assert.AreEqual(1, step.Released);
        }

        [UnityTest]
        public IEnumerator A_fill_that_throws_stops_the_sequence()
        {
            _pools.Throws = new InvalidOperationException("prefab missing");
            LogAssert.ignoreFailingMessages = true;
            RecordingGroup step = Step<RecordingGroup>();

            step.Execute("Match");
            yield return null;

            LogAssert.ignoreFailingMessages = false;
            Assert.AreEqual(0, step.Released);
            Assert.AreEqual(1, step.Stopped);
        }
    }
}
