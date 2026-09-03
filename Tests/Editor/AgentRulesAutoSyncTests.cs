using FlowIoC.Editor.AgentRules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class AgentRulesAutoSyncTests
    {
        private const string ProjectA = @"D:\work\ProjectA";
        private const string ProjectB = @"D:\work\ProjectB";

        private AgentRulesAutoSync _autoSync;

        [SetUp]
        public void SetUp() => _autoSync = new AgentRulesAutoSync();

        [TearDown]
        public void TearDown()
        {
            _autoSync.TurnOn(ProjectA);
            _autoSync.TurnOn(ProjectB);
        }

        /// <summary>
        /// The default is what a project that has just installed FlowIoC gets, and it is on: the
        /// rule block describes the version the project is on, and a stale one helps nobody.
        /// </summary>
        [Test]
        public void A_project_that_has_said_nothing_is_synced_automatically()
        {
            Assert.IsFalse(_autoSync.IsOff(ProjectA));
        }

        [Test]
        public void Turning_it_off_stops_the_automatic_sync()
        {
            _autoSync.TurnOff(ProjectA);

            Assert.IsTrue(_autoSync.IsOff(ProjectA));
        }

        [Test]
        public void Turning_it_on_again_resumes_the_automatic_sync()
        {
            _autoSync.TurnOff(ProjectA);

            _autoSync.TurnOn(ProjectA);

            Assert.IsFalse(_autoSync.IsOff(ProjectA));
        }

        /// <summary>
        /// The failure this type exists for: the switch used to live under one machine-wide
        /// EditorPrefs key, so turning it off in one project turned it off in every other project
        /// on the machine.
        /// </summary>
        [Test]
        public void Turning_it_off_in_one_project_leaves_another_project_synced()
        {
            _autoSync.TurnOff(ProjectA);

            Assert.IsTrue(_autoSync.IsOff(ProjectA));
            Assert.IsFalse(_autoSync.IsOff(ProjectB));
        }

        [Test]
        public void Two_project_roots_produce_two_keys()
        {
            Assert.AreNotEqual(_autoSync.KeyFor(ProjectA), _autoSync.KeyFor(ProjectB));
        }

        [Test]
        public void One_project_root_produces_a_stable_key()
        {
            Assert.AreEqual(_autoSync.KeyFor(ProjectA), _autoSync.KeyFor(ProjectA));
        }

        /// <summary>
        /// Unity hands the project root back with either separator and with whatever casing the
        /// drive reports, so the same project must not end up with two different keys.
        /// </summary>
        [Test]
        public void Separators_casing_and_a_trailing_slash_do_not_change_the_key()
        {
            string expected = _autoSync.KeyFor(@"D:\work\ProjectA");

            Assert.AreEqual(expected, _autoSync.KeyFor("D:/work/ProjectA"));
            Assert.AreEqual(expected, _autoSync.KeyFor(@"d:\WORK\projecta"));
            Assert.AreEqual(expected, _autoSync.KeyFor(@"D:\work\ProjectA\"));
        }
    }
}