using System.Threading.Tasks;

namespace Modules.LoadingModule.Services
{
    /// <summary>
    /// Shows, waits for and times the game's loading; loads nothing itself. A set is begun by the
    /// module that owns the moment, its steps are reported by whoever does the work, and it ends
    /// when every step CD_LoadingSets lists for it has completed, skipped or failed.
    /// </summary>
    public interface ILoadingService
    {
        /// <summary>Opens the set's presentation. A Silent set needs no Begin; its first report begins it.</summary>
        void Begin(string set);

        /// <summary>The handle a Command reports through. The step's set comes from the config.</summary>
        ILoadingStep Report(string step);

        /// <summary>True when the set completed, false when it failed or the config does not know it.</summary>
        Task<bool> Await(string set);
    }
}
