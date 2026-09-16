using FlowIoC.BaseModule.Root;

namespace Modules.DeviceDebuggerModule.RootsContexts
{
    public class DeviceDebuggerServiceRoot : Root<DeviceDebuggerServiceContext>
    {
        /// <summary>
        /// The ring outlives a scene load, so the error that happened during one can still be
        /// read. Unity marks only root level objects as do not destroy, so a Root authored under
        /// something else has to detach itself before it can survive the load.
        /// </summary>
        protected override void BeforeCreateContext()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }
}
