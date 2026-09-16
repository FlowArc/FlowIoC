using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// Every channel the console knows, and the one place a channel's colour and tag are looked up.
    /// The framework's own come from <see cref="SystemLogChannelTable"/>; the project's are read
    /// off <see cref="FlowModule"/>, where each module's generated part declares its channel as a
    /// <c>const string</c> and, beside it, a <c>&lt;Name&gt;Color</c> and an optional
    /// <c>&lt;Name&gt;Profile</c>. So the list of a project's channels is what the compiler sees,
    /// and nothing keeps a second copy of it: a module that exists has a channel, a module that is
    /// gone has none, and there is no list to fall out of step with the modules.
    ///
    /// Read once and kept, because the table is asked on every log written. A domain reload
    /// rebuilds it, which is when the parts can have changed.
    /// </summary>
    public class FlowLogChannels
    {
        private const string DefaultChannelName = "Default";
        private const string ColorSuffix = "Color";
        private const string ProfileSuffix = "Profile";

        private readonly List<FlowLogChannel> _all = new List<FlowLogChannel>();

        private readonly Dictionary<string, FlowLogChannel> _byName =
            new Dictionary<string, FlowLogChannel>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<SystemLogType, FlowLogChannel> _bySystemType =
            new Dictionary<SystemLogType, FlowLogChannel>();

        public FlowLogChannels() : this(typeof(FlowModule))
        {
        }

        /// <summary>
        /// The type the project's channels are read from, so a test can hand in a class of its own
        /// rather than depend on what this project happens to have generated.
        /// </summary>
        internal FlowLogChannels(Type declaredChannels)
        {
            foreach (FlowLogChannel channel in new SystemLogChannelTable().All())
                Add(channel);

            foreach (FlowLogChannel channel in ReadDeclared(declaredChannels))
                Add(channel);

#if UNITY_EDITOR
            // Keyed by the project, because a module's channel is named for the module and two
            // projects on one machine may each have a PlayerModule.
            Visibility = new FlowConsoleChannelVisibility(
                "FlowIoC.Console.Channels." + UnityEditor.PlayerSettings.productGUID);
#endif
        }

        /// <summary>
        /// The framework's channels in enum order, then the project's: Default first, the modules
        /// after it by name.
        /// </summary>
        public IReadOnlyList<FlowLogChannel> All => _all;

#if UNITY_EDITOR
        /// <summary>The channels this developer has switched on and off, over the defaults.</summary>
        public FlowConsoleChannelVisibility Visibility { get; }
#endif

        /// <summary>
        /// The lookup a log goes through. A channel is addressed by name, so this answers for the
        /// framework's channels and the project's alike - the framework's are named for their
        /// <see cref="SystemLogType"/>. Case-insensitive, and the channel found carries the
        /// spelling everything else uses.
        /// </summary>
        public bool TryGet(string name, out FlowLogChannel channel)
        {
            if (name == null)
            {
                channel = null;
                return false;
            }

            return _byName.TryGetValue(name, out channel);
        }

        public bool TryGet(SystemLogType systemType, out FlowLogChannel channel)
        {
            return _bySystemType.TryGetValue(systemType, out channel);
        }

        /// <summary>What decorates a line on the channel, or null when nothing does or the channel is unknown.</summary>
        public FlowLogProfile ProfileOf(string name)
        {
            return TryGet(name, out FlowLogChannel channel) ? channel.Profile : null;
        }

        /// <summary>
        /// In the Editor this is the developer's own answer, switches and all; in a build there is
        /// nobody at the machine, so the shipped default is the only answer there is.
        /// </summary>
        public bool IsShown(FlowLogChannel channel)
        {
#if UNITY_EDITOR
            return Visibility.IsShown(channel);
#else
            return channel.IsVisibleByDefault;
#endif
        }

        /// <summary>A channel nobody declared is shown: there is no switch anywhere that could have hidden it.</summary>
        public bool IsShown(string name)
        {
            return !TryGet(name, out FlowLogChannel channel) || IsShown(channel);
        }

        private void Add(FlowLogChannel channel)
        {
            if (string.IsNullOrEmpty(channel.Name) || _byName.ContainsKey(channel.Name)) return;

            _all.Add(channel);
            _byName[channel.Name] = channel;

            if (channel.SystemType.HasValue)
                _bySystemType[channel.SystemType.Value] = channel;
        }

        /// <summary>
        /// Every public <c>const string</c> on the class is a channel, named by its value. The
        /// identifier the generator wrote for it is what the colour and the profile hang off:
        /// <c>PlayerModule</c>, <c>PlayerModuleColor</c>, <c>PlayerModuleProfile</c>. A channel with
        /// no colour field is white - a part written before colours were, until it is regenerated.
        /// </summary>
        private static IEnumerable<FlowLogChannel> ReadDeclared(Type declaredChannels)
        {
            var declared = new List<FlowLogChannel>();

            if (declaredChannels == null) return declared;

            FieldInfo[] fields = declaredChannels.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                if (!field.IsLiteral || field.FieldType != typeof(string)) continue;

                var name = field.GetRawConstantValue() as string;
                if (string.IsNullOrEmpty(name)) continue;

                Color color = Color.white;
                FieldInfo colorField = declaredChannels.GetField(field.Name + ColorSuffix,
                    BindingFlags.Public | BindingFlags.Static);

                if (colorField != null && colorField.FieldType == typeof(Color))
                    color = (Color) colorField.GetValue(null);

                FlowLogProfile profile = null;
                FieldInfo profileField = declaredChannels.GetField(field.Name + ProfileSuffix,
                    BindingFlags.Public | BindingFlags.Static);

                if (profileField != null && profileField.FieldType == typeof(FlowLogProfile))
                    profile = profileField.GetValue(null) as FlowLogProfile;

                // A part with no profile field is a card with no Profile line, and that means the
                // default tag. A card that says "Profile: none" generates a field holding an
                // undecorated profile, which is the one way a module's lines carry no tag at all.
                if (profile == null)
                    profile = DefaultProfileFor(name, color);

                declared.Add(new FlowLogChannel(name, null, color, true, profile));
            }

            declared.Sort(CompareDeclared);

            return declared;
        }

        /// <summary>
        /// What a module's lines carry when its card says nothing: the module's name as a tag,
        /// "[Player]" for PlayerModule, in the module's colour, the way a framework channel's
        /// lines carry "[Signal]". Null for Default, which is no module and carries no tag.
        /// </summary>
        internal static FlowLogProfile DefaultProfileFor(string channelName, Color color)
        {
            if (string.Equals(channelName, DefaultChannelName, StringComparison.OrdinalIgnoreCase)) return null;

            return new FlowLogProfile().SetPrefix("[" + TagNameOf(channelName) + "]", FlowTextStyle.None, color);
        }

        /// <summary>
        /// The module's name without its Module suffix: the tag reads "[Player]", the way the
        /// framework's read "[Signal]", and the suffix says nothing the Modules group does not.
        /// </summary>
        private static string TagNameOf(string moduleName)
        {
            const string suffix = "Module";

            return moduleName.Length > suffix.Length && moduleName.EndsWith(suffix, StringComparison.Ordinal)
                ? moduleName.Substring(0, moduleName.Length - suffix.Length)
                : moduleName;
        }

        /// <summary>Default leads, because it belongs to no module; the modules follow by name.</summary>
        private static int CompareDeclared(FlowLogChannel left, FlowLogChannel right)
        {
            bool leftDefault = string.Equals(left.Name, DefaultChannelName, StringComparison.OrdinalIgnoreCase);
            bool rightDefault = string.Equals(right.Name, DefaultChannelName, StringComparison.OrdinalIgnoreCase);

            if (leftDefault != rightDefault) return leftDefault ? -1 : 1;

            return string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
