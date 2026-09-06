using FlowIoC.BaseModule.Pooling;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The one pool the framework's four pooled things now share.
    /// </summary>
    public class TypePoolTests
    {
        [Test]
        public void An_empty_pool_hands_out_nothing()
        {
            TypePool<Parked> pool = new TypePool<Parked>();

            Assert.That(pool.TryTake(typeof(Parked), out Parked taken), Is.False);
            Assert.That(taken, Is.Null);
        }

        [Test]
        public void What_is_returned_is_what_comes_back()
        {
            TypePool<Parked> pool = new TypePool<Parked>();
            Parked item = new Parked();

            pool.Return(typeof(Parked), item);

            Assert.That(pool.TryTake(typeof(Parked), out Parked taken), Is.True);
            Assert.That(taken, Is.SameAs(item));
        }

        [Test]
        public void An_instance_is_handed_out_once()
        {
            TypePool<Parked> pool = new TypePool<Parked>();
            pool.Return(typeof(Parked), new Parked());

            pool.TryTake(typeof(Parked), out Parked _);

            Assert.That(pool.TryTake(typeof(Parked), out Parked second), Is.False);
            Assert.That(second, Is.Null);
        }

        [Test]
        public void Each_type_keeps_its_own()
        {
            TypePool<Parked> pool = new TypePool<Parked>();
            Parked other = new OtherParked();

            pool.Return(typeof(OtherParked), other);

            Assert.That(pool.TryTake(typeof(Parked), out Parked _), Is.False, "a different type's pool is empty");
            Assert.That(pool.TryTake(typeof(OtherParked), out Parked taken), Is.True);
            Assert.That(taken, Is.SameAs(other));
        }

        [Test]
        public void An_unguarded_pool_takes_a_second_return_at_its_word()
        {
            TypePool<Parked> pool = new TypePool<Parked>();
            Parked item = new Parked();

            Assert.That(pool.Return(typeof(Parked), item), Is.True);
            Assert.That(pool.Return(typeof(Parked), item), Is.True,
                "nothing is checked, which is what keeps the command path free of a hash per return");
        }

        [Test]
        public void A_guarded_pool_refuses_the_same_instance_twice()
        {
            TypePool<Parked> pool = new TypePool<Parked>(guardDoubleReturn: true);
            Parked item = new Parked();

            Assert.That(pool.Return(typeof(Parked), item), Is.True);
            Assert.That(pool.Return(typeof(Parked), item), Is.False);

            pool.TryTake(typeof(Parked), out Parked _);

            Assert.That(pool.TryTake(typeof(Parked), out Parked second), Is.False,
                "it was parked once, so it comes out once");
        }

        [Test]
        public void A_guarded_pool_takes_an_instance_back_after_it_has_been_handed_out()
        {
            TypePool<Parked> pool = new TypePool<Parked>(guardDoubleReturn: true);
            Parked item = new Parked();

            pool.Return(typeof(Parked), item);
            pool.TryTake(typeof(Parked), out Parked _);

            Assert.That(pool.Return(typeof(Parked), item), Is.True, "it is in use again, so returning it is honest");
        }

        [Test]
        public void A_null_is_never_parked()
        {
            TypePool<Parked> pool = new TypePool<Parked>();

            Assert.That(pool.Return(typeof(Parked), null), Is.False);
            Assert.That(pool.TryTake(typeof(Parked), out Parked _), Is.False);
        }

        [Test]
        public void Clearing_lets_go_of_everything()
        {
            TypePool<Parked> pool = new TypePool<Parked>(guardDoubleReturn: true);
            Parked item = new Parked();
            pool.Return(typeof(Parked), item);

            pool.Clear();

            Assert.That(pool.TryTake(typeof(Parked), out Parked _), Is.False);
            Assert.That(pool.Return(typeof(Parked), item), Is.True, "and forgets that it ever held it");
        }

        private class Parked
        {
        }

        private class OtherParked : Parked
        {
        }
    }
}
