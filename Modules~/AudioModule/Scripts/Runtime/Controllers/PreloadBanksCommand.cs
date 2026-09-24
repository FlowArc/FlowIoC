using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared.Data.UnityObjects;
using Modules.AudioModule.Signals;

namespace Modules.AudioModule.Controllers
{
    /// <summary>Starts loading every bank that asks to be ready from the start. It does not wait for them.</summary>
    internal class PreloadBanksCommand : Command
    {
        [Inject] private IAudioBankModel _banks { get; set; }
        [InjectSignal] private AudioInternalSignals _signals { get; set; }

        public override void Execute()
        {
            foreach (CD_AudioBank bank in _banks.Banks)
            {
                if (bank.PreloadAtBoot)
                    _signals.LoadBank.Dispatch(bank.Module, _ => { });
            }
        }
    }
}
