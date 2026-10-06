#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.ResourceFlyModule.Data.ValueObjects;
using Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Constants;
using Modules.ResourceFlyModule.Services;
using UnityEngine;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Controllers
{
    /// <summary>
    /// Flies the item just won into the box, its picture from the game's data - the way a reward
    /// screen flies a weapon's own icon: the named look Item gives the rest and makes it visual only.
    /// </summary>
    internal class FlySampleItemCommand : Command
    {
        [Inject] private IResourceFlyService _resourceFly { get; set; }

        [SignalParam] private Sprite _sprite { get; set; }

        public override void Execute()
        {
            Retain();

            try
            {
                _resourceFly.Fly(new ResourceFlyRouteVO(ResourceFlySampleKeys.BOX_SOURCE, ResourceFlySampleKeys.BOX), 1,
                    new ResourceFlyLookVO {Name = ResourceFlySampleKeys.LOOK_ITEM, Sprite = _sprite}, () => Release());
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"FlySampleItemCommand threw: {exception}");
                Release();
            }
        }
    }
}
#endif
