using System.Threading.Tasks;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Builder
{
    public interface IScreenBuilderSubService
    {
        internal IScreenBuilderSubService Open<T>(int managerId = 0) where T : IScreenBody;
        IScreenBuilderSubService OpenInLayer(int layerIndex);

        /// <summary>
        /// if there is another screen opened on the same layer,
        /// <br></br>This will force the other screen to HIDE!.
        /// <br></br>Use "true" for other screen to hide with animation.
        /// </summary>
        /// <param name="forceOpenForLayerWithHideAnim">Boolean</param>
        /// <returns></returns>
        IScreenBuilderSubService ForceOpenAtFullLayer(bool forceOpenForLayerWithHideAnim = false);
        /// <summary>
        /// if there is a same screen opened before,
        /// <br></br>This will force the same screen to HIDE!.
        /// <br></br>Use "true" for same screen to hide with animation.
        /// </summary>
        /// <param name="forceOpenForDuplicationWithHideAnim">Boolean</param>
        /// <returns></returns>
        IScreenBuilderSubService ForceOpenAtDuplication(bool forceOpenForDuplicationWithHideAnim = false);
        IScreenBuilderSubService SetParameters(params object[] parameters);
        IScreenBuilderSubService SkipShowAnimation();
        IScreenBuilderSubService SkipHideAnimation();
        IScreenBuilderSubService AddToHistory();
        
        Task<IScreenBody> Show();
        Task<T> Show<T>() where T : IScreenBody;
        internal ScreenVO GetAndFlushScreenData();
        internal T GetAndFlushScreenBody<T>() where T : IScreenBody;
    }
}