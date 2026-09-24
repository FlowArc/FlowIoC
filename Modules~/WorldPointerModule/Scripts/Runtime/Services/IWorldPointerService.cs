using System.Runtime.CompilerServices;
using UnityEngine;

namespace Modules.WorldPointerModule.Services
{
    /// <summary>
    /// Joins what stands in the world to the screen that draws it. Both sides meet on a channel -
    /// a name such as "Emote" that says what kind of pointer this is. The side that owns a world
    /// object registers its Transform on a channel and sends requests - content, show, hide -
    /// without ever reaching a canvas. A screen registers itself as the channel's display. While
    /// both are registered the service takes an indicator from the display for every target, moves
    /// it to the target's screen position every LateUpdate, and forwards the requests; with no
    /// display the targets wait, and their last requests are kept for the display that comes.
    /// Nothing is returned and nothing is announced: a refused call is an error naming the line
    /// that made it.
    /// </summary>
    public partial interface IWorldPointerService
    {
        /// <summary>
        /// The camera pointers are projected through. Camera.main until a game sets it, and read
        /// again whenever it comes back null - a scene load that replaced the main camera is picked
        /// up without anybody telling the service.
        /// </summary>
        Camera Camera { get; set; }

        /// <summary>
        /// Starts pointing at <paramref name="target"/> for the display of <paramref name="channel"/>,
        /// now or whenever one arrives. The channel and the Transform together are the target's key,
        /// found in one step: the same pair twice is an error and the second call is ignored, and
        /// two pointers on one object use two channels.
        /// </summary>
        void RegisterTarget(string channel, Transform target,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0);

        /// <summary>
        /// Stops pointing at the pair. Its indicator, if a display had given one, goes back to the
        /// display's Release. A pair that is not registered is ignored. A target that goes back to a
        /// pool is unregistered before it does - a pooled Transform is somebody else's next target.
        /// </summary>
        void UnregisterTarget(string channel, Transform target);

        /// <summary>
        /// What the pair's indicator shows. Forwarded to the indicator when there is one and kept
        /// either way, so a display that registers later starts from the last content. A type the
        /// channel's display does not take is refused with an error.
        /// </summary>
        void SetContent<TContent>(string channel, Transform target, TContent content,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0);

        /// <summary>Lets the projection decide again after a Hide. A target starts shown.</summary>
        void Show(string channel, Transform target,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0);

        /// <summary>Tells the indicator Hidden and stops placing it until Show.</summary>
        void Hide(string channel, Transform target,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0);

        /// <summary>
        /// Unregisters every target, and every indicator goes back to its display's Release. The
        /// displays stay registered: they belong to their screens and go when the screens close.
        /// </summary>
        void UnregisterAll();

        /// <summary>
        /// Makes <paramref name="display"/> the one that draws <paramref name="channel"/>. Every
        /// target on the channel is given an indicator at once, with its last content and its last
        /// Show or Hide. A second display for a channel that has one is an error and is ignored, and
        /// so is a display whose content type differs from content a target already holds.
        /// WorldPointerPoolDisplay is the ready one.
        /// </summary>
        void RegisterDisplay<TContent>(string channel, IWorldPointerDisplay<TContent> display,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0);

        /// <summary>
        /// Takes the channel's display away. Its indicators go back to its Release, and its targets
        /// wait with their last requests for the next display. A channel with no display is ignored.
        /// </summary>
        void UnregisterDisplay(string channel);

        /// <summary>
        /// One-shot, for a screen's own placement: the world point on <paramref name="parent"/>'s
        /// plane under a world position, for something placed once rather than followed - a
        /// damage number spawned at the hit. False when there is no camera or the position is
        /// behind it; the out value is then unusable.
        /// </summary>
        bool TryProject(Vector3 worldPosition, RectTransform parent, out Vector3 pointOnCanvas);

        /// <summary>
        /// The steps a game binds in a sequence of its own. They sit inside the interface so that
        /// the one name a game knows - the Service it injects - is also where its steps are found.
        /// Each step is a file of its own, <c>IWorldPointerService.Commands.&lt;Step&gt;.cs</c>.
        /// Registering is not a step: it takes a Transform and a display that exist at runtime, so
        /// it is a call from the game's own Command.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}