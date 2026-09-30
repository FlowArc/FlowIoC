using Modules.CameraModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.CameraModule.Data.ValueObjects
{
    /// <summary>
    /// One camera name, held whole by CameraModel: the config it was registered with, and the last
    /// position its follow target was at. The position outlives an unregister - the record stays
    /// unregistered - so a camera registered again finds it. It holds a config side and a runtime
    /// side together, which is why it is a plain VO.
    /// </summary>
    internal class CameraVO
    {
        /// <summary>The config the camera was registered with; default while none is.</summary>
        public CameraCVO Config;

        /// <summary>False before the first register and after an unregister.</summary>
        public bool IsRegistered;

        public Vector3 LastPosition;
        public bool HasLastPosition;
    }
}
