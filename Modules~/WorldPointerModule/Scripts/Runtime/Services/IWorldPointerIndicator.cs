using Modules.WorldPointerModule.Enums;
using UnityEngine;

namespace Modules.WorldPointerModule.Services
{
    /// <summary>
    /// What the service needs from the thing it moves. Three members, so any UI element can be a
    /// pointer; the WorldPointerIndicator component in Entities is the ordinary implementation.
    /// </summary>
    public interface IWorldPointerIndicator
    {
        /// <summary>The element that is moved. It must sit under a Canvas.</summary>
        RectTransform Rect { get; }

        /// <summary>Null unless the indicator has an arrow. Its up is aimed at the target on the edge.</summary>
        RectTransform ArrowPivot { get; }

        /// <summary>Told on the first tick after it is handed out, and afterwards only when the state changes.</summary>
        void SetState(WorldPointerState state);
    }

    /// <summary>An indicator that shows content: what the world side sent with SetContent.</summary>
    public interface IWorldPointerIndicator<in TContent> : IWorldPointerIndicator
    {
        /// <summary>Called when it is handed out with content already sent, and on every SetContent after.</summary>
        void SetContent(TContent content);
    }
}