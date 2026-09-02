using System.Collections.Generic;
using FlowIoC.Editor.Screens;
using FlowIoC.ScreenModule.Data;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Two screens on one layer of one manager is legal and sometimes deliberate - the second
    /// closes the first, which is how a stack of full screen views behaves - so this is advice
    /// rather than an error. It states the rule the runtime enforces in IsLayerFull, which is
    /// keyed by manager and layer together.
    /// </summary>
    public class ScreenLayerCollisionsTests
    {
        private ScreenLayerCollisions _collisions;

        [SetUp]
        public void SetUp() => _collisions = new ScreenLayerCollisions();

        private static ScreenRowEVO Row(string name, int managerId, int layer)
        {
            return new ScreenRowEVO
            {
                ContextName = name,
                Effective = new ScreenCVO {ManagerId = managerId, Layer = layer}
            };
        }

        [Test]
        public void Two_screens_on_one_layer_of_one_manager_collide()
        {
            ScreenRowEVO first = Row("First", 0, 1);
            ScreenRowEVO second = Row("Second", 0, 1);

            HashSet<ScreenRowEVO> collided = _collisions.Find(new[] {first, second});

            Assert.AreEqual(2, collided.Count);
            Assert.IsTrue(collided.Contains(first));
            Assert.IsTrue(collided.Contains(second));
        }

        [Test]
        public void The_same_layer_at_different_managers_does_not_collide()
        {
            HashSet<ScreenRowEVO> collided = _collisions.Find(new[] {Row("First", 0, 1), Row("Second", 1, 1)});

            Assert.AreEqual(0, collided.Count);
        }

        [Test]
        public void Different_layers_at_one_manager_do_not_collide()
        {
            HashSet<ScreenRowEVO> collided = _collisions.Find(new[] {Row("First", 0, 1), Row("Second", 0, 2)});

            Assert.AreEqual(0, collided.Count);
        }

        [Test]
        public void Three_screens_on_one_layer_all_collide()
        {
            HashSet<ScreenRowEVO> collided =
                _collisions.Find(new[] {Row("First", 0, 1), Row("Second", 0, 1), Row("Third", 0, 1)});

            Assert.AreEqual(3, collided.Count);
        }

        [Test]
        public void A_row_without_effective_values_takes_no_part()
        {
            ScreenRowEVO unreadable = new ScreenRowEVO {ContextName = "Unreadable"};

            HashSet<ScreenRowEVO> collided = _collisions.Find(new[] {unreadable, Row("First", 0, 0)});

            Assert.AreEqual(0, collided.Count);
        }

        [Test]
        public void An_empty_list_collides_with_nothing()
        {
            Assert.AreEqual(0, _collisions.Find(new ScreenRowEVO[0]).Count);
        }
    }
}
