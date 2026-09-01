using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.Model.Config;

namespace FlowIoC.ScreenModule.Commands
{
    public sealed class UnRegisterScreenConfigCommand : Command
    {
        [SignalParam] private int _managerId { get; set; }
        [SignalParam] private List<CD_Screen> _configs { get; set; }
        [Inject] private IScreenConfigModel _screenConfigModel { get; set; }

        public override void Execute()
        {
            if (_configs == null)
            {
                FlowLogger.LogError(SystemLogType.Screen, "[ScreenService][UnRegisterScreenConfigCommand] Cannot unregister null screen configs!");
                return;
            }

            for (int i = 0; i < _configs.Count; i++)
            {
                var config = _configs[i];

                if (config == null)
                {
                    FlowLogger.LogWarning(SystemLogType.Screen, $"[ScreenService][UnRegisterScreenConfigCommand] Null config found in screen configs list! index:{i}");
                    continue;
                }

                _screenConfigModel.UnRegisterScreenConfig(_managerId, config.ConvertStringToType(), config);
            }
        }
    }
}