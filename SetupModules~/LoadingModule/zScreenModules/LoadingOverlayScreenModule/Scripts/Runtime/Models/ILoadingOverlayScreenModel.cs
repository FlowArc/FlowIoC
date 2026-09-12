using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.Models
{
    /// <summary>
    /// The last snapshot and close seen for each set, so the opening Command can fill an overlay
    /// that finished loading after the news arrived. A Running snapshot clears the close.
    /// </summary>
    public interface ILoadingOverlayScreenModel
    {
        void Remember(LoadingSetStatusRVO status);
        void RememberClosed(string set);
        bool TryGetLatest(string set, out LoadingSetStatusRVO status);
        bool IsClosed(string set);
    }
}
