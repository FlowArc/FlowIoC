namespace FlowIoC.BaseModule.ViewsMediators.View
{
    public interface IView : ITransform
    {
        bool IsRegistered { get; set; }
    }
}