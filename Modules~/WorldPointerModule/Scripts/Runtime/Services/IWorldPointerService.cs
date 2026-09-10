using System.Runtime.CompilerServices;
using Modules.WorldPointerModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.WorldPointerModule.Services
{
    /// <summary>
    /// Moves a UI element to a 3D object's screen position every frame and decides what happens
    /// when the object leaves the frame - hide, clamp to the edge, or ignore. The whole surface
    /// of the module: there are no signals, because nothing outside it needs telling.
    /// </summary>
    public interface IWorldPointerService
    {
        /// <summary>
        /// The camera pointers are projected through. Camera.main until a game sets it, and read
        /// again whenever it comes back null - a scene load that replaced the main camera is picked
        /// up without anybody telling the service.
        /// </summary>
        Camera Camera { get; set; }

        /// <summary>
        /// Starts moving <paramref name="indicator"/> to <paramref name="target"/>'s screen
        /// position every LateUpdate. The canvas is read from the indicator's own hierarchy, so an
        /// indicator with no Canvas above it is refused with an error and an invalid handle. The
        /// last two parameters are filled in by the compiler; they name the caller in that error.
        /// </summary>
        WorldPointerHandle Register(Transform target, IWorldPointerIndicator indicator,
            WorldPointerOptionsCVO options = null,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0);

        /// <summary>
        /// Stops following and tells the indicator Hidden - not registered is Hidden, whichever way
        /// it ends. A handle already unregistered, or never valid, is ignored.
        /// </summary>
        void Unregister(WorldPointerHandle handle);

        /// <summary>Stops every pointer. Each indicator is told Hidden first.</summary>
        void UnregisterAll();

        /// <summary>
        /// One-shot: the world point on <paramref name="parent"/>'s plane under a world position,
        /// for something placed once rather than followed - a damage number spawned at the hit.
        /// False when there is no camera or the position is behind it; the out value is then unusable.
        /// </summary>
        bool TryProject(Vector3 worldPosition, RectTransform parent, out Vector3 pointOnCanvas);
    }
}
