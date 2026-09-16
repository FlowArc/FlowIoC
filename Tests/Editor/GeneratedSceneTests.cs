using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// After the reload Create Module places the Root in the scene it made before the reload,
    /// and in no other. The scene it made is known by its path and by nothing else - not by
    /// being the active scene, which is whatever happened to be open when the scripts came
    /// back.
    /// </summary>
    public class GeneratedSceneTests
    {
        private const string Recorded = "Assets/Modules/HeroModule/Scenes/HeroScene.unity";

        [Test]
        public void The_scene_at_the_recorded_path_is_the_generated_one()
        {
            Assert.IsTrue(new GeneratedScene(Recorded).IsAt(Recorded));
        }

        /// <summary>
        /// The owner's open scene, on the day the bug was found: LoadingOverlayScreenTestScene
        /// was active when a main module finished compiling, and the old code saved it.
        /// </summary>
        [Test]
        public void A_scene_at_another_path_is_not()
        {
            Assert.IsFalse(new GeneratedScene(Recorded)
                .IsAt("Assets/Modules/LoadingModule/zScreenModules/LoadingOverlayScreenModule/zTestModules/LoadingOverlayScreenTestModule/Scenes/LoadingOverlayScreenTestScene.unity"));
        }

        /// <summary>
        /// A run that asked for no scene recorded no path, and then no scene at all is its
        /// scene - least of all whatever is open.
        /// </summary>
        [Test]
        public void No_recorded_path_means_no_scene_is_the_generated_one()
        {
            Assert.IsFalse(new GeneratedScene(null).IsAt(Recorded));
            Assert.IsFalse(new GeneratedScene("").IsAt(Recorded));
        }

        /// <summary>
        /// An untitled scene has no path. It is never the generated one, because the generated
        /// one was saved before the reload and has its path from that save.
        /// </summary>
        [Test]
        public void An_unsaved_scene_is_not_the_generated_one()
        {
            Assert.IsFalse(new GeneratedScene(Recorded).IsAt(""));
            Assert.IsFalse(new GeneratedScene(Recorded).IsAt(null));
        }

        /// <summary>
        /// The recorded path is built from folder paths that came off Path.Combine, and Unity
        /// reports a scene's path with forward slashes. The two spellings name one file.
        /// </summary>
        [Test]
        public void Slashes_do_not_decide()
        {
            Assert.IsTrue(new GeneratedScene(Recorded.Replace('/', '\\')).IsAt(Recorded));
        }
    }
}
