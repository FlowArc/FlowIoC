using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.WorldPointerModule.Services
{
    public partial interface IWorldPointerService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Unregisters every target - the step a flow binds when the scene the targets stood in
            /// is left. Every indicator goes back to its display's Release; the displays stay.
            /// </summary>
            public class UnregisterAll : Command
            {
                [Inject] private IWorldPointerService _worldPointerService { get; set; }

                public override void Execute() => _worldPointerService.UnregisterAll();
            }
        }
    }
}