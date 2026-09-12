using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.LoadingModule.Data.UnityObjects;
using Modules.LoadingModule.Data.ValueObjects;
using Modules.LoadingModule.Shared.Data.ValueObjects;

namespace Modules.LoadingModule.Models
{
    /// <summary>
    /// The sets and their steps as the config declared them and as the reports have left them. It
    /// knows nothing of screens or signals: a Command reads a report into it and asks it for the
    /// snapshot to publish.
    /// </summary>
    public interface ILoadingModel
    {
        IReadOnlyList<LoadingSetRVO> Sets { get; }
        CD_LoadingSets Config { get; }

        void Load(CD_LoadingSets config);

        bool TryGetSet(string set, out LoadingSetRVO runtime);
        bool TryGetStep(string step, out LoadingStepRVO runtime);
        IReadOnlyList<LoadingStepRVO> ParentStepsOf(string childSet);

        void Begin(LoadingSetRVO set, float now);

        /// <summary>
        /// A failed set whose failed step reports again is running once more; the steps that
        /// succeeded keep their end, and no second Began is announced.
        /// </summary>
        void Reopen(LoadingSetRVO set, float now);

        void Apply(LoadingStepRVO step, LoadingStepReportVO report);
        bool IsEnded(LoadingSetRVO set);
        void End(LoadingSetRVO set, float now);

        float Progress(LoadingSetRVO set);
        float Fraction(LoadingStepRVO step);
        LoadingSetStatusRVO BuildStatus(LoadingSetRVO set);

        TaskCompletionSource<bool> Awaiter(LoadingSetRVO set);
        bool TryTakeStallWarning(LoadingSetRVO set, float now, out string warning);
    }
}
