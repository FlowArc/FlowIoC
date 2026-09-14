using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace FlowIoC.AssetModule.Service
{
    public partial interface IAssetService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Loads every asset under an Addressables label into a group and holds the sequence
            /// until the last of them is in, the label and the group named where the step is bound:
            /// <c>.ToSequence&lt;IAssetService.Commands.LoadGroupByLabel&lt;Sprite&gt;&gt;("splash", "boot")</c>.
            /// The type is what the assets load as. A load that throws stops the sequence; what
            /// went wrong is reported where it happened.
            /// </summary>
            public class LoadGroupByLabel<T> : Command<string, string>
            {
                [Inject] private IAssetService _assetService { get; set; }

                public override async void Execute(string label, string groupId)
                {
                    Retain();

                    try
                    {
                        await _assetService.LoadGroupByLabelAsync<T>(label, groupId);
                        Release();
                    }
                    catch (Exception exception)
                    {
                        FlowLogger.LogError($"IAssetService.Commands.LoadGroupByLabel threw while loading '{label}' into '{groupId}': {exception}");
                        Stop();
                    }
                }
            }
        }
    }
}
