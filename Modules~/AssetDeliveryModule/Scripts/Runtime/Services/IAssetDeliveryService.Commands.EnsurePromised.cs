using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Constants;
using Modules.AssetDeliveryModule.Data.ValueObjects;
using Modules.AssetDeliveryModule.Enums;
using Modules.LoadingModule.Services;

namespace Modules.AssetDeliveryModule.Services
{
    public partial interface IAssetDeliveryService
    {
        public static partial class Commands
        {
            /// <summary>
            /// The boot's step: every pack the store promised before the first open - all but the
            /// on-demand ones - fetched if it has not landed, drawn on the Content step as bytes.
            /// Nothing missing skips the step; a pack that will not come fails it and stops the
            /// sequence, and the loading screen's retry runs the boot again.
            /// Bound as <c>.ToSequence&lt;IAssetDeliveryService.Commands.EnsurePromised&gt;()</c>
            /// after Begin and before anything loads.
            /// </summary>
            public class EnsurePromised : Command
            {
                [Inject] private IAssetDeliveryService _delivery { get; set; }
                [Inject] private ILoadingService _loading { get; set; }

                public override async void Execute()
                {
                    Retain();

                    ILoadingStep step = _loading.Report(AssetDeliveryConstants.CONTENT_STEP);

                    try
                    {
                        IReadOnlyList<AssetPackRVO> packs = await _delivery.GetPacksAsync();
                        var missing = new List<string>();

                        foreach (AssetPackRVO pack in packs)
                            if (pack.Policy != AssetPackPolicy.OnDemand && !pack.OnDevice)
                                missing.Add(pack.Pack);

                        if (missing.Count == 0)
                        {
                            step.Skip();
                            Release();
                            return;
                        }

                        step.Start();

                        // A store that reports no size - Play in local testing - leaves the detail alone.
                        var progress = new ImmediateProgress<AssetPackProgressRVO>(report =>
                        {
                            step.Progress(report.Fraction);
                            if (report.TotalBytes > 0) step.Detail(AssetPackProgressText.Of(report));
                        });

                        bool landed = await _delivery.EnsureAsync(missing, progress);

                        if (!landed)
                        {
                            step.Fail("content did not download");
                            Stop();
                            return;
                        }

                        step.Complete();
                        Release();
                    }
                    catch (Exception exception)
                    {
                        FlowLogger.LogError($"IAssetDeliveryService.Commands.EnsurePromised threw: {exception}");
                        step.Fail(exception.Message);
                        Stop();
                    }
                }
            }
        }
    }
}
