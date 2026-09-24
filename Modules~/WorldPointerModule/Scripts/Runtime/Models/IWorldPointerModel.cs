using System.Collections.Generic;
using Modules.WorldPointerModule.Data.ValueObjects;
using Modules.WorldPointerModule.Enums;
using Modules.WorldPointerModule.Services;
using Modules.WorldPointerModule.Services.Sub;
using UnityEngine;

namespace Modules.WorldPointerModule.Models
{
    /// <summary>
    /// The registry: every channel and every target, found by key in one step. Its rows are
    /// RD_WorldPointer's, written in place. Bound locally: only the service crosses.
    /// </summary>
    internal interface IWorldPointerModel
    {
        /// <summary>Every channel with a display or a target. Walk it backwards to remove while walking.</summary>
        IReadOnlyList<WorldPointerChannelRVO> Channels { get; }

        int TargetCount { get; }

        WorldPointerChannelRVO GetChannel(string channel);

        bool TryGetTarget(string channel, Transform target, out WorldPointerTargetRVO entry);

        /// <summary>Adds the pair, and its channel if it is the first. The caller has checked it is not there.</summary>
        WorldPointerTargetRVO AddTarget(string channel, Transform target);

        /// <summary>Takes the target out, and its channel with it when that leaves the channel empty and undrawn.</summary>
        void RemoveTarget(WorldPointerTargetRVO entry);

        /// <summary>Takes every target out. Channels with a display stay.</summary>
        void ClearTargets();

        /// <summary>Gives the channel its display, adding the channel if needed; null takes it away.</summary>
        WorldPointerChannelRVO SetDisplay(string channel, WorldPointerDisplaySlot slot);

        void SetContent(WorldPointerTargetRVO entry, object content);

        void SetVisible(WorldPointerTargetRVO entry, bool visible);

        /// <summary>Hands the target an indicator, or takes it away with null. Either way its state starts again.</summary>
        void SetIndicator(WorldPointerTargetRVO entry, IWorldPointerIndicator indicator, Camera canvasCamera);

        /// <summary>Records the state. False when it is the one already recorded.</summary>
        bool SetState(WorldPointerTargetRVO entry, WorldPointerState state);
    }
}