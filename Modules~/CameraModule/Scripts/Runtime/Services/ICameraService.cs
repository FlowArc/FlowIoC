using System;
using Modules.CameraModule.Shared.Enums;
using Unity.Cinemachine;
using UnityEngine;

namespace Modules.CameraModule.Services
{
    /// <summary>
    /// The module's one counterpart, and the only way in: the module has no public signals. A
    /// game's Command injects it where it decided which camera goes live or what it follows; the
    /// steps a game binds instead of writing that Command sit under <see cref="Commands"/>, one
    /// file each beside this one. The cameras themselves are the game's: it names them in
    /// <see cref="CameraName"/>, which the module's card lets the game extend.
    /// </summary>
    public partial interface ICameraService
    {
        /// <summary>Makes the named camera the live one. A camera nobody registered is reported, not switched to.</summary>
        void Switch(CameraName camera);

        /// <summary>Points the live camera at a target to follow and look at.</summary>
        void Follow(Transform target);

        /// <summary>
        /// Moves the live camera to a point by moving what it follows, over the seconds given;
        /// arrived is called when it gets there, or at once when the camera follows nothing.
        /// </summary>
        void MoveCamera(Vector3 position, float seconds, Action arrived = null);

        /// <summary>
        /// Zooms the live camera: eases its distance from what it follows to the one given, over
        /// the seconds given; arrived is called when it gets there, or at once when the camera
        /// has no position composer.
        /// </summary>
        void SetDistance(float distance, float seconds, Action arrived = null);

        /// <summary>Remembers where the live camera's follow target is now, under the camera's name.</summary>
        void RememberPosition(CameraName camera);

        /// <summary>The position last remembered for the camera, if one was.</summary>
        bool TryGetRememberedPosition(CameraName camera, out Vector3 position);

        /// <summary>The live camera, or null before any camera is registered as active.</summary>
        CinemachineCamera ActiveCamera { get; }

        /// <summary>Whether a camera of that name is registered in the scene now.</summary>
        bool IsRegistered(CameraName camera);

        /// <summary>
        /// The steps a game binds in a sequence of its own, so the flow reads from the Context:
        /// which camera, after which step. Each step is a file of its own,
        /// <c>ICameraService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}