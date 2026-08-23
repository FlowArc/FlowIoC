using FlowIoC.Editor.AgentRules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AgentRulesDismissalTests
    {
        private const string ProjectA = @"D:\work\ProjectA";
        private const string ProjectB = @"D:\work\ProjectB";
        private const string Hash = "1eac1650";

        private AgentRulesDismissal _dismissal;

        [SetUp]
        public void SetUp() => _dismissal = new AgentRulesDismissal();

        [TearDown]
        public void TearDown()
        {
            _dismissal.Clear(ProjectA);
            _dismissal.Clear(ProjectB);
        }

        /// <summary>
        /// The failure this type exists for: the dismissal used to live under one machine-wide
        /// EditorPrefs key, so saying "do not ask again" in one project silenced the notice in
        /// every other project on the machine.
        /// </summary>
        [Test]
        public void Dismissing_one_project_leaves_another_project_asking()
        {
            _dismissal.Dismiss(ProjectA, Hash);

            Assert.IsTrue(_dismissal.IsDismissed(ProjectA, Hash));
            Assert.IsFalse(_dismissal.IsDismissed(ProjectB, Hash));
        }

        [Test]
        public void A_dismissal_only_covers_the_rules_it_was_made_for()
        {
            _dismissal.Dismiss(ProjectA, Hash);

            Assert.IsFalse(_dismissal.IsDismissed(ProjectA, "deadbeef"));
        }

        [Test]
        public void Clear_makes_the_notice_ask_again()
        {
            _dismissal.Dismiss(ProjectA, Hash);

            _dismissal.Clear(ProjectA);

            Assert.IsFalse(_dismissal.IsDismissed(ProjectA, Hash));
            Assert.IsFalse(_dismissal.HasDismissal(ProjectA));
        }

        [Test]
        public void HasDismissal_reports_whether_anything_was_recorded()
        {
            Assert.IsFalse(_dismissal.HasDismissal(ProjectA));

            _dismissal.Dismiss(ProjectA, Hash);

            Assert.IsTrue(_dismissal.HasDismissal(ProjectA));
        }

        [Test]
        public void Nothing_is_dismissed_before_anything_is_recorded()
        {
            Assert.IsFalse(_dismissal.IsDismissed(ProjectA, Hash));
        }

        [Test]
        public void Two_project_roots_produce_two_keys()
        {
            Assert.AreNotEqual(_dismissal.KeyFor(ProjectA), _dismissal.KeyFor(ProjectB));
        }

        [Test]
        public void One_project_root_produces_a_stable_key()
        {
            Assert.AreEqual(_dismissal.KeyFor(ProjectA), _dismissal.KeyFor(ProjectA));
        }

        /// <summary>
        /// Unity hands the project root back with either separator and with whatever casing the
        /// drive reports, so the same project must not end up with two different keys.
        /// </summary>
        [Test]
        public void Separators_casing_and_a_trailing_slash_do_not_change_the_key()
        {
            string expected = _dismissal.KeyFor(@"D:\work\ProjectA");

            Assert.AreEqual(expected, _dismissal.KeyFor("D:/work/ProjectA"));
            Assert.AreEqual(expected, _dismissal.KeyFor(@"d:\WORK\projecta"));
            Assert.AreEqual(expected, _dismissal.KeyFor(@"D:\work\ProjectA\"));
        }
    }
}
