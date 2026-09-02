using System.Collections.Generic;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The same screen may be registered at two managers, so the passive pool has to keep the two
    /// instances apart: one pooled after closing at a manager belongs to that manager's layers and
    /// must not be handed to an Open at the other.
    /// </summary>
    public class ScreenRuntimeModelPoolTests
    {
        private class PooledScreenView : ScreenView
        {
        }

        private readonly List<GameObject> _created = new();

        private ScreenRuntimeModel _runtimeModel;

        // PostConstruct is deliberately not called: it creates a DontDestroyOnLoad pool parent,
        // which edit mode has no use for. Without it AddToPassivePool re-parents to null, which
        // is what a root object already is.
        [SetUp]
        public void SetUp() => _runtimeModel = new ScreenRuntimeModel();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _created)
                Object.DestroyImmediate(created);

            _created.Clear();
        }

        private PooledScreenView Screen(int managerId)
        {
            GameObject host = new GameObject("PooledScreen");
            _created.Add(host);

            PooledScreenView view = host.AddComponent<PooledScreenView>();
            view.Data = new ScreenVO {ScreenType = typeof(PooledScreenView), ManagerId = managerId};
            return view;
        }

        [Test]
        public void A_screen_pooled_at_one_manager_is_not_handed_to_another()
        {
            _runtimeModel.AddToPassivePool(Screen(0));

            Assert.IsFalse(_runtimeModel.GetScreen<PooledScreenView>(1, out PooledScreenView screen));
            Assert.IsNull(screen);
        }

        [Test]
        public void A_screen_pooled_at_a_manager_is_handed_back_to_that_manager()
        {
            PooledScreenView pooled = Screen(0);
            _runtimeModel.AddToPassivePool(pooled);

            Assert.IsTrue(_runtimeModel.GetScreen<PooledScreenView>(0, out PooledScreenView screen));
            Assert.AreSame(pooled, screen);
        }

        [Test]
        public void Each_manager_keeps_its_own_pooled_instance()
        {
            PooledScreenView atZero = Screen(0);
            PooledScreenView atOne = Screen(1);
            _runtimeModel.AddToPassivePool(atZero);
            _runtimeModel.AddToPassivePool(atOne);

            _runtimeModel.GetScreen<PooledScreenView>(1, out PooledScreenView fromOne);
            _runtimeModel.GetScreen<PooledScreenView>(0, out PooledScreenView fromZero);

            Assert.AreSame(atOne, fromOne);
            Assert.AreSame(atZero, fromZero);
        }

        [Test]
        public void Removing_from_the_pool_removes_only_that_manager_entry()
        {
            PooledScreenView atZero = Screen(0);
            PooledScreenView atOne = Screen(1);
            _runtimeModel.AddToPassivePool(atZero);
            _runtimeModel.AddToPassivePool(atOne);

            _runtimeModel.RemoveFromPassivePool(atZero);

            Assert.IsFalse(_runtimeModel.GetScreen<PooledScreenView>(0, out PooledScreenView _));
            Assert.IsTrue(_runtimeModel.GetScreen<PooledScreenView>(1, out PooledScreenView remaining));
            Assert.AreSame(atOne, remaining);
        }
    }
}
