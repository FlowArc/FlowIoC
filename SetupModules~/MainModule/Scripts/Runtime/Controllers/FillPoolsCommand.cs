using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Services;
using Modules.LoadingModule.Services;
using Modules.MainModule.Constants;

namespace Modules.MainModule.Controllers
{
    /// <summary>
    /// Fills the pool groups MainConstants.BootPoolGroups names and reports the Pools step group by
    /// group. No groups means nothing to fill: the step is skipped, counts its weight, and the bar
    /// reaches the end - the worked example of a step that legitimately does not run.
    /// </summary>
    internal class FillPoolsCommand : Command
    {
        [Inject] private IPoolService _poolService { get; set; }
        [Inject] private ILoadingService _loadingService { get; set; }

        public override async void Execute()
        {
            Retain();

            ILoadingStep step = _loadingService.Report(MainConstants.POOLS_STEP);
            string[] groups = MainConstants.BootPoolGroups;

            if (groups.Length == 0)
            {
                step.Skip();
                Release();
                return;
            }

            step.Start();

            try
            {
                for (int index = 0; index < groups.Length; index++)
                {
                    if (!_poolService.Check.IsGroupConfigExist(groups[index]))
                    {
                        FlowLogger.LogError(FlowLogType.MainModule,
                            $"FillPoolsCommand - MainConstants.BootPoolGroups names '{groups[index]}', which the pool config does not have.");
                        step.Fail($"pool group '{groups[index]}' is not configured");
                        Stop();
                        return;
                    }

                    step.Detail(groups[index]);
                    await _poolService.InitializeGroupAsync(groups[index]);
                    step.Progress((index + 1f) / groups.Length);
                }

                step.Complete();
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError(FlowLogType.MainModule, $"FillPoolsCommand threw while filling a pool group: {exception}");
                step.Fail(exception.Message);
                Stop();
            }
        }
    }
}
