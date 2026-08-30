using FlowIoC.Editor.SetupModules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class BuildSceneListTests
    {
        private BuildSceneList _list;

        [SetUp]
        public void SetUp()
        {
            _list = new BuildSceneList();
        }

        [Test]
        public void The_scene_goes_to_the_front_of_an_empty_list()
        {
            string[] result = _list.WithSceneFirst(new string[0], "Assets/Main.unity");

            CollectionAssert.AreEqual(new[] {"Assets/Main.unity"}, result);
        }

        [Test]
        public void The_scenes_already_there_are_kept_and_pushed_down()
        {
            string[] result = _list.WithSceneFirst(new[] {"Assets/Scenes/SampleScene.unity"}, "Assets/Main.unity");

            CollectionAssert.AreEqual(
                new[] {"Assets/Main.unity", "Assets/Scenes/SampleScene.unity"},
                result);
        }

        [Test]
        public void A_scene_already_in_the_list_is_not_added_twice_or_moved()
        {
            string[] existing = {"Assets/Scenes/SampleScene.unity", "Assets/Main.unity"};

            string[] result = _list.WithSceneFirst(existing, "Assets/Main.unity");

            CollectionAssert.AreEqual(existing, result);
        }

        [Test]
        public void A_missing_scene_path_leaves_the_list_alone()
        {
            string[] existing = {"Assets/Scenes/SampleScene.unity"};

            CollectionAssert.AreEqual(existing, _list.WithSceneFirst(existing, null));
        }
    }
}
