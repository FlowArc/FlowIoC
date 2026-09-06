using System;
using System.Collections.Generic;
using FlowIoC.PoolModule.Entities;
using FlowIoC.PoolModule.Models.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The pool's bookkeeping, exercised without a scene. PostConstruct is deliberately not called:
    /// it builds the [Pools] object, and with no parent to move items under the model never touches
    /// a Transform, so a plain fake stands in for a pooled MonoBehaviour.
    /// </summary>
    public class PoolRuntimeModelTests
    {
        private const string Group = "group";
        private const string ItemKey = "item";

        private IPoolRuntimeModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new PoolRuntimeModel();
            _model.RegisterPool(ItemKey, Group);
        }

        [Test]
        public void A_registered_pool_is_found_and_an_unregistered_one_is_not()
        {
            Assert.That(_model.PoolExists(ItemKey, Group), Is.True);
            Assert.That(_model.PoolExists("other", Group), Is.False);
            Assert.That(_model.IsGroupCreated(Group), Is.True);
            Assert.That(_model.IsGroupCreated("other"), Is.False);
        }

        [Test]
        public void A_parked_item_comes_back_out_once()
        {
            FakePoolItem item = new FakePoolItem();
            _model.AddToPassivePool(item, ItemKey, Group);

            Assert.That(_model.TryGetFromPassivePool(ItemKey, Group, out FakePoolItem first), Is.True);
            Assert.That(first, Is.SameAs(item));
            Assert.That(_model.TryGetFromPassivePool(ItemKey, Group, out FakePoolItem second), Is.False);
            Assert.That(second, Is.Null);
        }

        [Test]
        public void Parking_an_item_deactivates_it()
        {
            FakePoolItem item = new FakePoolItem {Active = true};

            _model.AddToPassivePool(item, ItemKey, Group);

            Assert.That(item.Active, Is.False);
        }

        [Test]
        public void The_same_item_cannot_be_parked_twice()
        {
            FakePoolItem item = new FakePoolItem();

            _model.AddToPassivePool(item, ItemKey, Group);
            _model.AddToPassivePool(item, ItemKey, Group);

            Assert.That(_model.GetPassiveItemCount(ItemKey, Group), Is.EqualTo(1),
                "a double return would hand the same instance to two callers");
        }

        [Test]
        public void An_item_whose_type_does_not_hold_is_put_back_rather_than_lost()
        {
            FakePoolItem item = new FakePoolItem();
            _model.AddToPassivePool(item, ItemKey, Group);

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("TryGetFromPassivePool"));

            Assert.That(_model.TryGetFromPassivePool(ItemKey, Group, out OtherPoolItem wrongType), Is.False);
            Assert.That(wrongType, Is.Null);
            Assert.That(_model.GetPassiveItemCount(ItemKey, Group), Is.EqualTo(1),
                "the item left the passive half for the cast and has to go back into it");
        }

        [Test]
        public void An_active_item_is_removed_by_identity()
        {
            FakePoolItem first = new FakePoolItem();
            FakePoolItem second = new FakePoolItem();
            FakePoolItem third = new FakePoolItem();

            _model.AddToActivePool(first, ItemKey, Group);
            _model.AddToActivePool(second, ItemKey, Group);
            _model.AddToActivePool(third, ItemKey, Group);

            _model.RemoveFromActivePool(second, ItemKey, Group);

            List<IPoolableItem> left = new List<IPoolableItem>();
            foreach (IPoolableItem item in _model.GetAllActiveItemsByGroupKey(Group))
                left.Add(item);

            Assert.That(_model.GetActiveItemCount(ItemKey, Group), Is.EqualTo(2));
            Assert.That(left, Has.Member(first));
            Assert.That(left, Has.Member(third));
            Assert.That(left, Has.No.Member(second));
        }

        [Test]
        public void Removing_an_item_that_is_not_in_the_pool_changes_nothing()
        {
            FakePoolItem parked = new FakePoolItem();
            _model.AddToActivePool(parked, ItemKey, Group);

            _model.RemoveFromActivePool(new FakePoolItem(), ItemKey, Group);

            Assert.That(_model.GetActiveItemCount(ItemKey, Group), Is.EqualTo(1));
        }

        [Test]
        public void Unregistering_a_pool_leaves_the_rest_of_its_group_standing()
        {
            _model.RegisterPool("second", Group);

            _model.UnregisterPool(ItemKey, Group);

            Assert.That(_model.PoolExists(ItemKey, Group), Is.False, "the named pool is gone");
            Assert.That(_model.PoolExists("second", Group), Is.True, "its neighbour is not");
            Assert.That(_model.IsGroupCreated(Group), Is.True, "and neither is the group");
        }

        [Test]
        public void Clearing_a_group_empties_both_halves_of_every_pool_in_it()
        {
            _model.AddToActivePool(new FakePoolItem(), ItemKey, Group);
            _model.AddToPassivePool(new FakePoolItem(), ItemKey, Group);

            _model.ClearPoolByGroupKey(Group);

            Assert.That(_model.GetActiveItemCount(ItemKey, Group), Is.Zero);
            Assert.That(_model.GetPassiveItemCount(ItemKey, Group), Is.Zero);
        }

        private class FakePoolItem : IPoolableItem
        {
            public string ItemKey { get; set; }
            public Action<IPoolableItem> ReturnToPoolAction { get; set; }
            public Transform transform => null;
            public bool Active;

            public void SetActive(bool value = true) => Active = value;
            public void OnInitialized() { }
            public void OnGetFromPool() { }
            public void OnReturnToPool() { }
            public void Dismiss() => ReturnToPoolAction?.Invoke(this);
        }

        private class OtherPoolItem : FakePoolItem
        {
        }
    }
}
