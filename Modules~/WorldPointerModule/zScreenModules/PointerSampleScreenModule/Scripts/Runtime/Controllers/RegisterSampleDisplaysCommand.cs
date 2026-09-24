#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.PoolModule.Services;
using Modules.WorldPointerModule.Data.UnityObjects;
using Modules.WorldPointerModule.Entities;
using Modules.WorldPointerModule.PointerSampleScreenModule.Constants;
using Modules.WorldPointerModule.PointerSampleScreenModule.Shared.Constants;
using Modules.WorldPointerModule.PointerSampleScreenModule.Shared.Data.ValueObjects;
using Modules.WorldPointerModule.PointerSampleScreenModule.ViewsMediators;
using Modules.WorldPointerModule.Services;
using UnityEngine;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.Controllers
{
    /// <summary>
    /// Makes the screen's three panels the displays of the sample's three channels, each handing
    /// out labels from the pool. Every target already registered on them is given a label at
    /// once, with its last content.
    /// </summary>
    internal class RegisterSampleDisplaysCommand : Command
    {
        [Inject] private IWorldPointerService _worldPointerService { get; set; }
        [Inject] private IPoolService _poolService { get; set; }

        [SignalParam] private PointerSampleScreenView _view { get; set; }

        public override void Execute()
        {
            Register(WorldPointerSampleIds.Hide, _view.HideParent, _view.HideOptions);
            Register(WorldPointerSampleIds.Clamp, _view.ClampParent, _view.ClampOptions);
            Register(WorldPointerSampleIds.Ignore, _view.IgnoreParent, _view.IgnoreOptions);
        }

        private void Register(string channel, RectTransform parent, CD_WorldPointerOptions options)
        {
            var display = new WorldPointerPoolDisplay<WorldPointerSampleVO>(_poolService, PointerSamplePoolKeys.Label, parent,
                options != null ? options.Options : null);

            _worldPointerService.RegisterDisplay(channel, display);
        }
    }
}
#endif