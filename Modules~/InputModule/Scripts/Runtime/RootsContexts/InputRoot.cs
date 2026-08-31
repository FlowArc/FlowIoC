using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;

namespace Modules.InputModule.RootsContexts
{
    /// <summary>
    /// The module's presence in the scene, and the parent of the EventSystem it brings with it.
    ///
    /// Input outlives a scene: a game that loads a second scene still needs its buttons to
    /// answer, so the root detaches itself and survives. Unity only marks root level objects as
    /// do not destroy, which is why the SetParent comes first, and BeforeCreateContext is where
    /// it happens because the context is built right after it.
    /// </summary>
     [CustomClassHeader("ROOTs", 0.8f, 0.2f, 0.2f, 0.2f, 0.2f, 0.8f, 14)]
    public class InputRoot : Root<InputContext>
    {
        protected override void BeforeCreateContext()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }
}
