using System;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace FlowIoC.AssetModule.Commands
{
    internal sealed class LoadGroupCommand : Command
    {
        [Inject] private IAssetService _assetService { get; set; }
        [SignalParam] private string _label { get; set; }

        /// <summary>
        /// Three ways out, and every one of them resolves the retain. The load finishing is the one
        /// that used to be written on its own: an empty label loads nothing and announces nothing,
        /// and an exception in the load left Execute at the await, so the step waited for a Release
        /// that was never coming and the group hung with no timeout and nothing logged.
        ///
        /// Both failures stop the flow rather than releasing it, because the steps behind this one
        /// were bound on the understanding that the group is in memory.
        /// </summary>
        public override async void Execute()
        {
            Retain();

            FlowLogger.Log(SystemLogType.Asset,
                $"[LoadGroupCommand.Execute][label({_label})]");

            // Asked here rather than left to the service: the service answers an empty label with a
            // finished task and no group, so the caller would carry on believing the group loaded.
            if (string.IsNullOrEmpty(_label))
            {
                FlowLogger.LogError(SystemLogType.Asset,
                    "LoadGroupCommand - the signal carried no label, so there is no group to load.");
                Stop();
                return;
            }

            try
            {
                await _assetService.LoadGroupByLabelAsync<UnityEngine.Object>(_label);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError(SystemLogType.Asset,
                    $"LoadGroupCommand threw while loading the group '{_label}': {exception}");
                Stop();
            }
        }
    }
}
