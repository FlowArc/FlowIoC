#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The camera Unity put in a scene a Create Module run made, found in that scene rather than
    /// through Camera.main, which answers for whichever scene is loaded. Unity has it clear to
    /// the skybox; a scene the run made clears to a solid colour instead, because what the run
    /// hands over - a screen on its layer, a module's Root - is looked at against a flat colour
    /// and the skybox's horizon behind it is nobody's. A game that wants the skybox back sets it
    /// on the camera, the way it sets anything else in a scene it was handed.
    /// </summary>
    internal class GeneratedSceneCamera
    {
        private readonly Scene _scene;

        public GeneratedSceneCamera(Scene scene)
        {
            _scene = scene;
        }

        /// <summary>
        /// Has the camera clear to a solid colour. The background colour is left as Unity set it,
        /// which is the colour that was under the skybox. A scene with no camera is left alone.
        /// </summary>
        public void ClearToSolidColour()
        {
            foreach (GameObject rootObject in _scene.GetRootGameObjects())
            {
                Camera camera = rootObject.GetComponentInChildren<Camera>(true);
                if (camera == null)
                    continue;

                camera.clearFlags = CameraClearFlags.SolidColor;
            }
        }
    }
}
#endif
