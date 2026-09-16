using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Models;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// Launch's one decision: initialize now, or wait for the game's IAdsService.Initialize.
    /// Off in CD_Ads means a consent flow comes first, and the sequence stops here.
    /// </summary>
    internal class RequestInitializeOnLaunchCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }

        public override void Execute()
        {
            Retain();

            if (!_model.Settings.InitializeOnLaunch)
            {
                FlowLogger.Log("Initialize - waiting for IAdsService.Initialize (CD_Ads.InitializeOnLaunch is off).");
                Stop();
                return;
            }

            _model.RequestInitialize();
            Release();
        }
    }
}
