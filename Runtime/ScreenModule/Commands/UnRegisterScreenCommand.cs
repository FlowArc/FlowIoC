using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Service.Sub;

namespace FlowIoC.ScreenModule.Commands
{
    /// <summary>
    /// A screen context going away takes its screen with it. The instance is unloaded first,
    /// while the entry still knows how it was loaded, and the entry is dropped after. The manager
    /// id is part of the address: the same view type may be registered at another manager by
    /// another Root, and that registration is none of this command's business.
    /// </summary>
    internal class UnRegisterScreenCommand : Command
    {
        [Inject] private IScreenRegistryModel _registry { get; set; }
        [Inject] private UnloadSubService _unload { get; set; }

        [SignalParam] private int _managerId { get; set; }
        [SignalParam] private Type _viewType { get; set; }

        public override void Execute()
        {
            if (!_registry.TryGetEntry(_managerId, _viewType, out ScreenEntry entry))
                return;

            if (entry.Loaded != null)
                _unload.Unregistered(entry.Loaded);

            _registry.RemoveEntry(entry);
        }
    }
}