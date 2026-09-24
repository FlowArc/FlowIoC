using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Signals;

namespace Modules.AudioModule.ViewsMediators
{
    public class ButtonClickSoundMediator : IMediator
    {
        [Inject] private ButtonClickSoundView _view { get; set; }

        [InjectSignal] private AudioInternalSignals _signals { get; set; }

        public void OnRegister() => _view.OnButtonPressed += Play;

        public void OnRemove() => _view.OnButtonPressed -= Play;

        private void Play(AudioKey key) => _signals.Play.Dispatch(key);
    }
}
