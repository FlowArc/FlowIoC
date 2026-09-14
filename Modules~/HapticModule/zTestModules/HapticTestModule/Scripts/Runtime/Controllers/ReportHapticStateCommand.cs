#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.HapticTestModule.Data.ValueObjects;
using Modules.HapticModule.HapticTestModule.Signals;
using Modules.HapticModule.Services;
using UnityEngine;

namespace Modules.HapticModule.HapticTestModule.Controllers
{
    internal class ReportHapticStateCommand : Command
    {
        [Inject] private IHapticService _haptics { get; set; }

        [InjectSignal] private HapticTestInternalSignals _signals { get; set; }

        public override void Execute()
        {
            _signals.StateChanged.Dispatch(new HapticTestStateVO
            {
                Platform = Application.platform.ToString(),
                IsEnabled = _haptics.IsEnabled()
            });
        }
    }
}

#endif
