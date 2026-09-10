#if UNITY_EDITOR

using FlowIoC.BaseModule.Root;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Models;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.RootsContexts
{
    public class AbTestFlowTestRoot : Root<AbTestFlowTestContext>
    {
        /// <summary>
        /// The Raise button writes into CD_AbTests, and the Editor keeps that after play mode ends.
        /// The model is bound locally and reached through this context's own binder - a test module
        /// has no service to ask, and nothing outside the context needs the model. Called directly
        /// rather than through a dispatched signal, which may not finish before the context is torn
        /// down.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (Context == null)
                return;

            var model = (IAbTestFlowTestModel) Context.InjectionBinder.GetInstance(typeof(IAbTestFlowTestModel));

            model?.RestoreEditorAssets();
        }
    }
}

#endif