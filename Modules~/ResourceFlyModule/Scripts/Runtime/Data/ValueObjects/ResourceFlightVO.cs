using System;
using Modules.ResourceFlyModule.Services;

namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>
    /// One flight on its way: the icons still out, the counter it began on - the one its end is
    /// owed to - and who is waiting for the last icon.
    /// </summary>
    internal class ResourceFlightVO
    {
        public ResourceVO Resource;
        public IResourceFlyCounter Counter;
        public int IconsOut;
        public Action Finished;

        /// <summary>The look the flight resolved at its start; every icon and landing of it reads this.</summary>
        public ResourceFlyLookVO Look;

        /// <summary>A launch fault is reported once per flight, not once per icon.</summary>
        public bool LaunchFaultReported;

        /// <summary>A sprite with no Image to go on is reported once per flight, not once per icon.</summary>
        public bool SpriteFaultReported;
    }
}