using FlowIoC.ScreenModule.Service.Sub;
using FlowIoC.ScreenModule.Service.Sub.Builder;
using FlowIoC.ScreenModule.Service.Sub.Load;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service
{
    public interface IScreenService
    {
        LoadSubService Load { get; }

        /// <summary><i><b>&gt;FlowIoC&lt;</b></i>
        /// <br></br>You have to use the "Show" method at the end of the line
        /// <br></br><b>Example:</b>
        /// <br></br>_screenService.Open&lt;NewScreenViewn&gt;().<b>Show()</b>;
        /// <br></br>var scrn = await _service.Open&lt;New...<b>Show&lt;NewScreenView&gt;()</b>;
        /// </summary>
        /// <typeparam name="T">IScreenBody (Example:NewScreenView)</typeparam>
        /// <returns></returns>
        IScreenBuilder Open<T>(int managerId = 0) where T : IScreenBody;

        CheckSubService Check { get; }
        TryGetSubService TryGet { get; }
        HideSubService Hide { get; }
        UnloadSubService Unload { get; }
    }
}
