using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LocalSaveModule.Models;

namespace Modules.LocalSaveModule.Services
{
    public partial interface ILocalSaveService
    {
        public static partial class Commands
        {
            /// <summary>Writes every persisted asset, flushing once at the end.</summary>
            public class SaveAll : Command
            {
                [Inject] private ILocalSaveModel _model { get; set; }

                public override void Execute() => _model.SaveAll();
            }
        }
    }
}
