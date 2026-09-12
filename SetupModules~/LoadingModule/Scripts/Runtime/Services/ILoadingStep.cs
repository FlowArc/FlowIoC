namespace Modules.LoadingModule.Services
{
    /// <summary>
    /// Start and Complete are the pair; Skip and Fail are the other two ends. A Complete, Skip or
    /// Fail on a step that never started is accepted - instantaneous work need not write two lines.
    /// </summary>
    public interface ILoadingStep
    {
        void Start();
        void Progress(float fraction);
        void Detail(string text);
        void Complete();
        void Skip();
        void Fail(string reason = null);
    }
}
