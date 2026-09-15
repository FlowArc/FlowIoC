using FlowIoC.BaseModule.Root;
using Modules.LocalSaveModule.Models;

namespace Modules.LocalSaveModule.RootsContexts
{
    public class LocalSaveServiceRoot : Root<LocalSaveServiceContext>
    {
        /// <summary>
        /// Unity marks only root level objects as do not destroy, so a Root authored under
        /// something else has to detach itself before it can survive a scene load.
        /// </summary>
        protected override void BeforeCreateContext()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// The order matters: the session has to reach disk before the assets are rolled back, or
        /// the play session's data is thrown away with the dirt.
        ///
        /// SaveAll is called on the model rather than dispatched as a signal, because a command
        /// dispatched during shutdown may not complete before the context is torn down.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (Context == null || _rootsManager == null)
                return;

            var model = (ILocalSaveModel) _rootsManager.InjectionBinderCrossContext
                .GetInstance(typeof(ILocalSaveModel));

            if (model == null)
                return;

            model.SaveAll();

#if UNITY_EDITOR
            model.RestoreEditorAssets();
#endif
        }
    }
}