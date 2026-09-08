using System.Collections.Generic;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleSoloTests
    {
        private readonly FlowConsoleSolo _solo = new FlowConsoleSolo();

        private static List<bool> Channels(params bool[] visible)
        {
            return new List<bool>(visible);
        }

        [Test]
        public void One_channel_visible_and_the_rest_dark_is_a_solo()
        {
            Assert.IsTrue(_solo.IsSoloed(Channels(false, true, false), 1));
        }

        [Test]
        public void A_channel_visible_beside_others_is_not_a_solo()
        {
            Assert.IsFalse(_solo.IsSoloed(Channels(true, true, false), 1));
        }

        [Test]
        public void A_channel_that_is_not_even_visible_is_not_a_solo()
        {
            Assert.IsFalse(_solo.IsSoloed(Channels(true, false, true), 1));
        }

        [Test]
        public void Soloing_leaves_one_channel_lit()
        {
            List<bool> channels = Channels(true, true, true);

            _solo.Apply(channels, 1);

            Assert.AreEqual(Channels(false, true, false), channels);
        }

        /// <summary>
        /// The way back matters as much as the way in. Soloing the channel that is already alone
        /// restores everything, so one key and one button both narrows the console to a single
        /// channel and puts it back.
        /// </summary>
        [Test]
        public void Soloing_the_channel_that_is_already_alone_lights_them_all()
        {
            List<bool> channels = Channels(false, true, false);

            _solo.Apply(channels, 1);

            Assert.AreEqual(Channels(true, true, true), channels);
        }

        [Test]
        public void Soloing_a_channel_that_was_hidden_lights_it_and_darkens_the_rest()
        {
            List<bool> channels = Channels(true, false, true);

            _solo.Apply(channels, 1);

            Assert.AreEqual(Channels(false, true, false), channels);
        }

        [Test]
        public void An_index_outside_the_list_changes_nothing()
        {
            List<bool> channels = Channels(true, false);

            _solo.Apply(channels, 5);
            Assert.AreEqual(Channels(true, false), channels);

            Assert.IsFalse(_solo.IsSoloed(channels, 5));
            Assert.IsFalse(_solo.IsSoloed(null, 0));
        }
    }
}
