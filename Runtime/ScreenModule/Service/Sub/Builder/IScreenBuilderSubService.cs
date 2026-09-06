using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Builder
{
    /// <summary>
    /// Starts an open. It reads where the screen comes from - the pool, or the registry - and hands
    /// back a builder that carries that one screen and nothing else.
    /// </summary>
    public interface IScreenBuilderSubService
    {
        internal IScreenBuilder Open<T>(int managerId = 0) where T : IScreenBody;
    }
}