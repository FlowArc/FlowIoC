using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Builder
{
    /// <summary>
    /// Starts an open. It reads the screen's declaration from the registry and hands back a builder
    /// that carries that one screen and nothing else; the pool is asked at Show, by the builder.
    /// </summary>
    public interface IScreenBuilderSubService
    {
        internal IScreenBuilder Open<T>(int managerId = 0) where T : IScreenBody;
    }
}
