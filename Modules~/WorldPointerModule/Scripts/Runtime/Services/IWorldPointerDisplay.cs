using Modules.WorldPointerModule.Data.ValueObjects;

namespace Modules.WorldPointerModule.Services
{
    /// <summary>
    /// What a screen registers to draw one id. The non-generic half is what UnregisterDisplay
    /// takes, so a screen closing needs no content type to say it is going.
    /// </summary>
    public interface IWorldPointerDisplay
    {
        /// <summary>
        /// How this display's pointers behave - offsets, what happens off screen, margins, arrow,
        /// smoothing. Null means a fresh WorldPointerOptionsCVO. The screen owns them because the
        /// screen knows its layout and its safe area; the world side says only where and what.
        /// </summary>
        WorldPointerOptionsCVO Options { get; }
    }

    /// <summary>
    /// A display that hands out indicators for one content type. WorldPointerLayer is the ready
    /// implementation: a prefab, a pool and a preset on a component in the screen's prefab.
    /// </summary>
    public interface IWorldPointerDisplay<TContent> : IWorldPointerDisplay
    {
        /// <summary>An indicator for one target, under a Canvas. Null is reported and the target waits.</summary>
        IWorldPointerIndicator<TContent> Acquire();

        /// <summary>
        /// The indicator is no longer the service's: its target left, the targets were cleared, or
        /// this display was unregistered. The service does not tell it Hidden first - the display
        /// decides how it goes, at once back to the pool or after a fade of its own.
        /// </summary>
        void Release(IWorldPointerIndicator<TContent> indicator);
    }
}
