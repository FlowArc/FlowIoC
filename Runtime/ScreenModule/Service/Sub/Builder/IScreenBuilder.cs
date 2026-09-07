using System.Threading.Tasks;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Builder
{
    /// <summary>
    /// One screen being opened. <c>Open&lt;T&gt;()</c> hands one of these back and nothing happens
    /// until <c>Show()</c>, so the calls in between say how this screen is to be opened and no other.
    ///
    /// It is a thing per call rather than a thing per service. The sub service used to keep the
    /// screen being opened in its own fields, and a service is one object for the whole run: two
    /// commands opening a screen in the same frame wrote over each other, and the second Show
    /// opened whatever the first had asked for.
    /// </summary>
    public interface IScreenBuilder
    {
        IScreenBuilder OpenInLayer(int layerIndex);

        /// <summary>
        /// if there is another screen opened on the same layer,
        /// <br></br>This will force the other screen to HIDE!.
        /// <br></br>Use "true" for other screen to hide with animation.
        /// </summary>
        /// <param name="forceOpenForLayerWithHideAnim">Boolean</param>
        /// <returns></returns>
        IScreenBuilder ForceOpenAtFullLayer(bool forceOpenForLayerWithHideAnim = false);

        /// <summary>
        /// if there is a same screen opened before,
        /// <br></br>This will force the same screen to HIDE!.
        /// <br></br>Use "true" for same screen to hide with animation.
        /// </summary>
        /// <param name="forceOpenForDuplicationWithHideAnim">Boolean</param>
        /// <returns></returns>
        IScreenBuilder ForceOpenAtDuplication(bool forceOpenForDuplicationWithHideAnim = false);

        IScreenBuilder SetParameters(params object[] parameters);
        IScreenBuilder SkipShowAnimation();
        IScreenBuilder SkipHideAnimation();

        Task<IScreenBody> Show();
        Task<T> Show<T>() where T : IScreenBody;
    }
}
