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
            /// Fetches the bundles a key or label needs and holds the sequence until they are
            /// cached, the key given where the step is bound:
            /// <c>.ToSequence&lt;IAssetService.Commands.DownloadDependencies&gt;("remote-art")</c>.
            /// Nothing is held afterwards - it is a download, not a load. A download that fails
            /// stops the sequence, and so does one that throws; the service reports the reason.
            /// </summary>
            public class DownloadDependencies : Command<string>
            {
                [Inject] private IAssetService _assetService { get; set; }

                public override async void Execute(string keyOrLabel)
                {
                    Retain();

                    try
                    {
                        bool downloaded = await _assetService.DownloadDependenciesAsync(keyOrLabel);

                        if (!downloaded)
                        {
                            FlowLogger.Log($"IAssetService.Commands.DownloadDependencies - '{keyOrLabel}' did not download, so the sequence stops here.");
                            Stop();
                            return;
                        }

                        Release();
                    }
                    catch (Exception exception)
                    {
                        FlowLogger.LogError($"IAssetService.Commands.DownloadDependencies threw while fetching '{keyOrLabel}': {exception}");
                        Stop();
                    }
                }
            }
        }
    }
}
