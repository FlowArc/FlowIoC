using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingScreenModule.Models
{
    /// <summary>
    /// The last snapshot, close and failure seen for each set, so the opening Command can fill a
    /// screen that finished loading after the news arrived. A Running snapshot clears the close
    /// and the failure - that is a re-run.
    /// </summary>
    public interface ILoadingScreenModel
    {
        void Remember(LoadingSetStatusRVO status);
        void RememberClosed(string set);
        void RememberFailed(string set, string step);
        bool TryGetLatest(string set, out LoadingSetStatusRVO status);
        bool IsClosed(string set);
        string FailedStepOf(string set);
    }
}
