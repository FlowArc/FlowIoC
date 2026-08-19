using System.Threading.Tasks;
using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service
{
    public interface IScreenService
    {
        LoadSubService Load { get; set; }
        
        /// <summary><i><b>&gt;FlowIoC&lt;</b></i>
        /// <br></br>You have to use the "Show" method at the end of the line
        /// <br></br><b>Example:</b>
        /// <br></br>_screenService.Open&lt;NewScreenViewn&gt;().<b>Show()</b>;
        /// <br></br>var scrn = await _service.Open&lt;New...<b>Show&lt;NewScreenView&gt;()</b>;
        /// </summary>
        /// <typeparam name="T">IScreenBody (Example:NewScreenView)</typeparam>
        /// <returns></returns>
        IScreenBuilderSubService Open<T>(int managerId = 0) where T : IScreenBody;
        // internal Task<T> ShowNewScreen<T>() where T : IScreenBody;
        // internal Task<T> ShowPooledScreen<T>() where T : IScreenBody;
        
        CheckSubService Check { get; set; }
        TryGetSubService TryGet { get; set; }
        HideSubService Hide { get; set; }
        UnloadSubService Unload { get; set; }
        /*
        
        // History Methods
        Task BackToHistory(int managerIndex = 0);
        void ResetHistory(int managerIndex = 0);
        void ResetAllHistory();
        List<ScreenState> GetScreenStateHistory(IScreenBody screenBody);

        // Lifecycle Callbacks
        void RegisterLifecycleCallbacks(IScreenBody screenBody, Action onShow = null, Action onHide = null, Action onDestroy = null);
        void UnregisterLifecycleCallbacks(IScreenBody screenBody);
        void InvokeDestroyCallback(IScreenBody screenBody);

        // Screen State Methods
        ScreenState GetScreenState(IScreenBody screenBody);
        
        */
    }
}