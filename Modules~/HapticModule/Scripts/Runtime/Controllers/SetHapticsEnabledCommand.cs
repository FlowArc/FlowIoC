using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Constants;
using Modules.HapticModule.Models;
using Modules.HapticModule.Services;
using UnityEngine;

namespace Modules.HapticModule.Controllers
{
    /// <summary>Stores the choice, applies it, and cuts a vibration short when turning off.</summary>
    internal class SetHapticsEnabledCommand : Command
    {
        [SignalParam] private bool _on { get; set; }

        [Inject] private IHapticModel _model { get; set; }
        [Inject] private IHapticPlayer _player { get; set; }

        public override void Execute()
        {
            _model.SetEnabled(_on);
            PlayerPrefs.SetInt(HapticConstants.PrefsKey, _on ? 1 : 0);
            PlayerPrefs.Save();

            if (!_on)
                _player.Stop();
        }
    }
}
