using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.AssetModule.Service
{
    public partial interface IAssetService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Releases every asset a group holds, the group named where the step is bound:
            /// <c>.ToSequence&lt;IAssetService.Commands.ReleaseGroup&gt;("match")</c> - the match's
            /// assets let go of when the match is left.
            /// </summary>
            public class ReleaseGroup : Command<string>
            {
                [Inject] private IAssetService _assetService { get; set; }

                public override void Execute(string groupId) => _assetService.ReleaseGroup(groupId);
            }
        }
    }
}
