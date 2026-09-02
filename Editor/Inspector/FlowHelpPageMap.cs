#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Which help topic a role sends the reader to. A role with no topic answers null and the
    /// header bar draws no link at all - an icon that opens the wrong page is worse than no icon.
    /// Service and System are the two without a page today; writing one is all it takes for their
    /// icon to appear.
    /// </summary>
    internal class FlowHelpPageMap
    {
        private readonly Dictionary<FlowRole, string> _pages = new Dictionary<FlowRole, string>
        {
            {FlowRole.Root, "Root & Context"},
            {FlowRole.View, "View & Mediator"},
            {FlowRole.Mediator, "View & Mediator"},
            {FlowRole.Screen, "Screens"},
            {FlowRole.Connector, "Connectors"}
        };

        public string PageFor(FlowRole role)
        {
            return _pages.TryGetValue(role, out string title) ? title : null;
        }
    }
}

#endif
