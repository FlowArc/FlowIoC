using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A screen's own state, without the service. What is under test is what Hide does to a screen
    /// that is still animating in: the service cannot tell, because it asks for InUse and a screen
    /// animating in is InUse as well, so the screen itself has to hold the hide until the show is
    /// over rather than drop it.
    /// </summary>
    public class ScreenBodyTests
    {
        private class AnimatedScreen : ScreenBody
        {
            public int ShowsStarted;

            // Completes only when the test says so, the way a tween reports when it is done.
            protected override void PlayShowAnimation() => ShowsStarted++;

            public void FinishShow() => ShowCompleted?.Invoke(this);
        }

        private GameObject _host;
        private AnimatedScreen _screen;
        private int _hidden;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("AnimatedScreen");
            _screen = _host.AddComponent<AnimatedScreen>();
            _screen.Data.HasShowAnimation = true;
            _screen.Data.AddState(ScreenState.InUse);
            _hidden = 0;
            _screen.HideCompleted += _ => _hidden++;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_host);

        [Test]
        public void A_hide_asked_for_during_the_show_animation_happens_once_the_show_has_finished()
        {
            _screen.Show();
            _screen.Hide();

            Assert.That(_hidden, Is.Zero, "the screen is still animating in");

            _screen.FinishShow();

            Assert.That(_hidden, Is.EqualTo(1));
            Assert.That(_screen.Data.HasState(ScreenState.InShowAnimation), Is.False);
        }

        [Test]
        public void A_hide_that_was_waiting_is_forgotten_when_the_screen_is_shown_again()
        {
            _screen.Show();
            _screen.Hide();

            // The service force-hid it meanwhile and is opening it again; the old hide is stale.
            _screen.Show();
            _screen.FinishShow();

            Assert.That(_hidden, Is.Zero);
        }

        [Test]
        public void A_screen_that_is_not_in_use_ignores_hide()
        {
            _screen.Data.RemoveState(ScreenState.InUse);

            _screen.Hide();

            Assert.That(_hidden, Is.Zero);
        }
    }
}
