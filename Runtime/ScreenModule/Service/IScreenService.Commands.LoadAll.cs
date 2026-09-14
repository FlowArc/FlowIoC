using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.ScreenModule.Service
{
    public partial interface IScreenService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Loads every registered screen into the pool and holds the sequence until the last
            /// of them is in - the boot's preload as one step, for a game that shows no bar while
            /// it happens. A screen already loaded is passed over. A game that reports the load's
            /// progress on a loading step writes its own Command around <c>Load.All</c> instead,
            /// the way the setup set's <c>PreloadScreensCommand</c> does.
            /// </summary>
            public class LoadAll : Command
            {
                [Inject] private IScreenService _screenService { get; set; }

                public override void Execute()
                {
                    Retain();
                    _screenService.Load.All(completeCallback: () => Release());
                }
            }
        }
    }
}
