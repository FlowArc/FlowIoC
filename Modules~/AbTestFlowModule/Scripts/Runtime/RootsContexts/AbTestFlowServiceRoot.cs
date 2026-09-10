using FlowIoC.BaseModule.Root;
using Modules.AbTestFlowModule.Services;

namespace Modules.AbTestFlowModule.RootsContexts
{
    public class AbTestFlowServiceRoot : Root<AbTestFlowServiceContext>
    {
        /// <summary>
        /// Unity marks only root level objects as do not destroy, so a Root authored under
        /// something else has to detach itself before it can survive a scene load. Surviving is
        /// what keeps a scene change from re-rolling a group or re-applying an override.
        /// </summary>
        protected override void BeforeCreateContext()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

#if UNITY_EDITOR
        /// <summary>
        /// The Editor keeps whatever a play session wrote into a ScriptableObject, so the originals
        /// are put back before the process ends. The service is the module's one counterpart, so it
        /// is what the Root asks - directly rather than through a dispatched signal, which may not
        /// finish before the context is torn down.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (Context == null || _rootsManager == null)
                return;

            var service = (IAbTestFlowService) _rootsManager.InjectionBinderCrossContext.GetInstance(typeof(IAbTestFlowService));

            service?.RestoreEditorAssets();
        }
#endif
    }
}