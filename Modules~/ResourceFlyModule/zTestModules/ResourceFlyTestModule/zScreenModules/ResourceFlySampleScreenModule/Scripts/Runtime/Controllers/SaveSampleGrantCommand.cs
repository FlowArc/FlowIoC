#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Models;
using Modules.ResourceFlyModule.Services;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Controllers
{
    /// <summary>The save a game's grant would make, after the Reserve step: the counter learns the new saved value and still shows the old one.</summary>
    internal class SaveSampleGrantCommand : Command<string>
    {
        [Inject] private IResourceFlySampleModel _model { get; set; }
        [Inject] private IResourceFlyService _resourceFly { get; set; }

        [SignalParam] private int _amount { get; set; }

        public override void Execute(string key) => _resourceFly.SetValue(key, _model.Grant(key, _amount));
    }
}
#endif
