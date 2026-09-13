using System.Collections.Generic;
using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowChannelPaletteTests
    {
        private readonly FlowChannelPalette _palette = new FlowChannelPalette();

        /// <summary>
        /// The same module is the same colour on every machine and after every regeneration, or a
        /// part would rewrite itself on each load and the console would change colour under the
        /// reader. Case-insensitive, because a channel is found that way everywhere else.
        /// </summary>
        [Test]
        public void A_name_always_lands_on_the_same_tone_whatever_its_case()
        {
            Assert.AreEqual(_palette.Pick("PlayerModule"), new FlowChannelPalette().Pick("PlayerModule"));
            Assert.AreEqual(_palette.IndexOf("PlayerModule"), _palette.IndexOf("playermodule"));
        }

        [Test]
        public void There_are_twelve_tones_and_none_of_them_is_white_or_black()
        {
            Assert.AreEqual(12, _palette.Count);

            var seen = new HashSet<Color32>();

            for (int index = 0; index < _palette.Count; index++)
            {
                Color32 tone = _palette.ToneAt(index);

                Assert.IsTrue(seen.Add(tone), "Two tones are the same colour.");
                Assert.AreNotEqual((Color) tone, Color.white);
                Assert.AreNotEqual((Color) tone, Color.black);
                Assert.AreEqual(255, tone.a);
            }
        }

        [Test]
        public void Every_pick_is_one_of_the_tones()
        {
            foreach (string name in new[] {"PlayerModule", "HapticModule", "LoadingScreenModule", "", null})
            {
                int index = _palette.IndexOf(name);

                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, _palette.Count);
                Assert.AreEqual(_palette.ToneAt(index), _palette.Pick(name));
            }
        }

        /// <summary>
        /// The hash is FNV-1a, pinned here so that a change to it is a decision rather than an
        /// accident: every module in every project would change colour.
        /// </summary>
        [Test]
        public void The_hash_is_pinned()
        {
            // FNV-1a of "playermodule" is 533427784, and 533427784 % 12 is 4.
            Assert.AreEqual(4, _palette.IndexOf("PlayerModule"));
            Assert.AreEqual(2, _palette.IndexOf("HapticModule"));
        }
    }
}
