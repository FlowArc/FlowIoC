using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// One channel the console knows: the framework's own, or one a module declared on
    /// <see cref="FlowModule"/>. A channel is identified by its name and nothing else - the number
    /// a framework channel carries is its <see cref="SystemLogType"/>, and a project channel has
    /// none.
    /// </summary>
    public class FlowLogChannel
    {
        public FlowLogChannel(string name, SystemLogType? systemType, Color color, bool isVisibleByDefault,
            FlowLogProfile profile)
        {
            Name = name;
            SystemType = systemType;
            Color = color;
            IsVisibleByDefault = isVisibleByDefault;
            Profile = profile;
        }

        public string Name { get; }

        /// <summary>The enum value behind a framework channel, and null for a project channel.</summary>
        public SystemLogType? SystemType { get; }

        /// <summary>The colour of the row's left stripe and of the swatch in the Filters panel.</summary>
        public Color Color { get; }

        /// <summary>
        /// Whether the channel is on for a developer who has not touched it. What one developer
        /// actually has switched on is theirs, and lives in <see cref="FlowConsoleChannelVisibility"/>.
        /// </summary>
        public bool IsVisibleByDefault { get; }

        /// <summary>What decorates a line on this channel - the tag, its colour - or null for nothing.</summary>
        public FlowLogProfile Profile { get; }

        public bool IsFrameworkOwned => SystemType.HasValue;

        /// <summary>
        /// The three channels Unity writes rather than the framework: its own lines, the compiler's
        /// and a shader's. They are the Unity group in the console's Filters panel, and the rows
        /// that carry an icon instead of a text tag.
        /// </summary>
        public bool IsWrittenByUnity =>
            SystemType == SystemLogType.Unity
            || SystemType == SystemLogType.Compiler
            || SystemType == SystemLogType.Shader;
    }
}
