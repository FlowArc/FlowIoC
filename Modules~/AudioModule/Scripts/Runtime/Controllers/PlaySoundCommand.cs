using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Shared;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    internal class PlaySoundCommand : Command
    {
        [SignalParam] private AudioKey _key { get; set; }

        [Inject] private IFunctionProvider _functions { get; set; }

        public override void Execute() =>
            _functions.Call<StartSoundFunction>().AddParams(_key, false, Vector3.zero).ExecuteAndGetResult<bool>();
    }
}
