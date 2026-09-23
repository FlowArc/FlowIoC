#if UNITY_EDITOR
using UnityEngine.SceneManagement;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// An open scene that was never saved, and so has no path. Create Module makes its scene
    /// beside the open ones and Unity makes no new scene beside an untitled one, so a run that
    /// makes a scene asks this before it writes anything.
    /// </summary>
    internal class UntitledScene
    {
        public bool IsOpen
        {
            get
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                        return true;
                }

                return false;
            }
        }
    }
}
#endif
