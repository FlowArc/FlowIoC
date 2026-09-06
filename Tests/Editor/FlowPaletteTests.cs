using System;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowPaletteTests
    {
        private static readonly Color DarkSkin = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color LightSkin = new Color(0.784f, 0.784f, 0.784f);

        [Test]
        public void Deep_carries_white_title_text_on_every_role()
        {
            var palette = new FlowPalette();

            foreach (FlowRole role in Enum.GetValues(typeof(FlowRole)))
            {
                float ratio = Contrast(palette.Deep(role), Color.white);
                Assert.GreaterOrEqual(ratio, 4.5f, $"{role} deep fill fails white title contrast");
            }
        }

        [Test]
        public void Vivid_stays_visible_on_the_dark_skin()
        {
            var palette = new FlowPalette();

            foreach (FlowRole role in Enum.GetValues(typeof(FlowRole)))
            {
                float ratio = Contrast(palette.Vivid(role), DarkSkin);
                Assert.GreaterOrEqual(ratio, 3f, $"{role} vivid stripe disappears on the dark skin");
            }
        }

        [Test]
        public void Deep_stays_visible_on_the_light_skin()
        {
            var palette = new FlowPalette();

            foreach (FlowRole role in Enum.GetValues(typeof(FlowRole)))
            {
                float ratio = Contrast(palette.Deep(role), LightSkin);
                Assert.GreaterOrEqual(ratio, 3f, $"{role} deep stripe disappears on the light skin");
            }
        }

        /// <summary>
        /// The chrome is owned by no role, so the three loops above never reach it. It carries a
        /// window title and sits on both skins exactly as a role's fill does, and it has to clear
        /// the same bars.
        /// </summary>
        [Test]
        public void The_chrome_reads_everywhere_a_roles_fill_does()
        {
            var palette = new FlowPalette();

            Assert.GreaterOrEqual(Contrast(palette.ChromeDeep, Color.white), 4.5f,
                "chrome deep fails white title contrast");
            Assert.GreaterOrEqual(Contrast(palette.ChromeVivid, DarkSkin), 3f,
                "chrome vivid disappears on the dark skin");
            Assert.GreaterOrEqual(Contrast(palette.ChromeDeep, LightSkin), 3f,
                "chrome deep disappears on the light skin");
        }

        /// <summary>
        /// The generator windows wear the chrome because they are tools rather than roles. Reading
        /// it off a role is what made every one of them change colour the day the plain Root's fill
        /// did, so the two are asserted to be different things.
        /// </summary>
        [Test]
        public void The_chrome_is_no_roles_colour()
        {
            var palette = new FlowPalette();

            foreach (FlowRole role in Enum.GetValues(typeof(FlowRole)))
                Assert.AreNotEqual(palette.Deep(role), palette.ChromeDeep, $"the chrome is {role}'s fill");
        }

        [Test]
        public void Accent_follows_the_skin()
        {
            var palette = new FlowPalette();

            Assert.AreEqual(palette.Vivid(FlowRole.Root), palette.Accent(FlowRole.Root, true));
            Assert.AreEqual(palette.Deep(FlowRole.Root), palette.Accent(FlowRole.Root, false));
        }

        private static float Contrast(Color a, Color b)
        {
            float la = Luminance(a);
            float lb = Luminance(b);
            float hi = Mathf.Max(la, lb);
            float lo = Mathf.Min(la, lb);

            return (hi + 0.05f) / (lo + 0.05f);
        }

        private static float Luminance(Color c)
        {
            return 0.2126f * Channel(c.r) + 0.7152f * Channel(c.g) + 0.0722f * Channel(c.b);
        }

        private static float Channel(float v)
        {
            return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
        }
    }
}