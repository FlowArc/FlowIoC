using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.WorldPointerModule.Services
{
    public partial interface IWorldPointerService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Stops every pointer - the step a flow binds when the scene the pointers were
            /// following is left. Each indicator is told Hidden first.
            /// </summary>
            public class UnregisterAll : Command
            {
                [Inject] private IWorldPointerService _worldPointerService { get; set; }

                public override void Execute() => _worldPointerService.UnregisterAll();
            }
        }
    }
}
