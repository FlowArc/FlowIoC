using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.BotBarModule.Models;

namespace Modules.BotBarModule.Controllers
{
    /// <summary>
    /// Every Open puts the bar on its StartTab, silently: the screen reads the state when it
    /// opens, and the game's boot opens its home page itself.
    /// </summary>
    internal class SelectStartTabCommand : Command
    {
        [Inject] private IBotBarModel _model { get; set; }

        public override void Execute()
        {
            _model.Select(_model.StartTab);
            FlowLogger.Log($"Execute - SelectStartTabCommand | {_model.Selected}");
        }
    }
}
