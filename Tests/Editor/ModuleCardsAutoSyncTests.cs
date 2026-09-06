using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleCards;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleCardsAutoSyncTests
    {
        private const string PROJECT_A = "C:/projects/one";
        private const string PROJECT_B = "C:/projects/two";

        private readonly ModuleCardsAutoSync _switch = new ModuleCardsAutoSync();

        [TearDown]
        public void TearDown()
        {
            _switch.TurnOn(PROJECT_A);
            _switch.TurnOn(PROJECT_B);
        }

        [Test]
        public void It_is_on_until_it_is_turned_off()
        {
            Assert.IsFalse(_switch.IsOff(PROJECT_A));
        }

        [Test]
        public void Turning_it_off_is_remembered()
        {
            _switch.TurnOff(PROJECT_A);

            Assert.IsTrue(_switch.IsOff(PROJECT_A));
        }

        [Test]
        public void Turning_it_off_in_one_project_leaves_another_alone()
        {
            _switch.TurnOff(PROJECT_A);

            Assert.IsFalse(_switch.IsOff(PROJECT_B));
        }

        [Test]
        public void The_same_path_written_two_ways_is_the_same_project()
        {
            _switch.TurnOff("C:/projects/one");

            Assert.IsTrue(_switch.IsOff("C:\\Projects\\One\\"));
        }

        /// <summary>
        /// The two switches share an EditorPrefs store, so a project that turned the rules off
        /// must not find its cards turned off with them.
        /// </summary>
        [Test]
        public void Its_key_is_not_the_agent_rules_key()
        {
            Assert.AreNotEqual(new AgentRulesAutoSync().KeyFor(PROJECT_A), _switch.KeyFor(PROJECT_A));
        }
    }
}
