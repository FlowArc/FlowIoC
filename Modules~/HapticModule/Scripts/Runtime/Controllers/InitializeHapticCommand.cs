using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Constants;
using Modules.HapticModule.Models;
using Modules.HapticModule.Services;
using UnityEngine;

namespace Modules.HapticModule.Controllers
{
    /// <summary>Reads the stored choice into the model and readies the platform. Runs from Setup.</summary>
    internal class InitializeHapticCommand : Command
    {
        [Inject] private IHapticModel _model { get; set; }
        [Inject] private IHapticPlayer _player { get; set; }

        public override void Execute()
        {
            _model.SetEnabled(PlayerPrefs.GetInt(HapticConstants.PrefsKey, 1) == 1);
            _player.Initialize();
        }
    }
}
