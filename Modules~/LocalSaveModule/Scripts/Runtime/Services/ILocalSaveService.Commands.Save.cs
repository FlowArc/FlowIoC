using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LocalSaveModule.Models;

namespace Modules.LocalSaveModule.Services
{
    public partial interface ILocalSaveService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Writes one persisted asset, named the way it was filed on the Root's adapter -
            /// <c>.ToSequence&lt;ILocalSaveService.Commands.Save&gt;("PD_Profile")</c>, not a key
            /// of the sender's own choosing.
            /// </summary>
            public class Save : Command<string>
            {
                [Inject] private ILocalSaveModel _model { get; set; }

                public override void Execute(string assetName) => _model.Save(assetName);
            }
        }
    }
}
